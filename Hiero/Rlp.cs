using System.Collections;
using System.Numerics;
using System.Text;

namespace Hiero;

public static class Rlp
{
    public static byte[] Encode(params object?[] data)
    {
        if (data == null)
        {
            return [0x80];
        }
        if (data.Length == 1)
        {
            return EncodeData(data[0]);
        }
        return EncodeData(data);
    }
    private static byte[] EncodeData(object? data)
    {
        if (data == null)
        {
            return [0x80];
        }
        if (data is byte[] byteData)
        {
            return EncodeBytes(byteData);
        }
        if (data.GetType().IsArray)
        {
            return EncodeList(((IEnumerable)data).Cast<object>().Select(EncodeData).ToArray());
        }
        if (data is ReadOnlyMemory<byte> readOnlyMemory)
        {
            return EncodeBytes(readOnlyMemory.ToArray());
        }
        if (data is string stringData)
        {
            return EncodeBytes(Encoding.UTF8.GetBytes(stringData));
        }
        if (data is BigInteger integer)
        {
            return EncodeInteger(integer);
        }
        if (data is int intData)
        {
            return EncodeInteger(new BigInteger(intData));
        }
        if (data is long longData)
        {
            return EncodeInteger(new BigInteger(longData));
        }
        if (data is ulong ulongData)
        {
            return EncodeInteger(new BigInteger(ulongData));
        }
        if (data is decimal decimalData)
        {
            return EncodeInteger(new BigInteger(decimalData));
        }
        if (data is short shortData)
        {
            return EncodeInteger(new BigInteger(shortData));
        }
        if (data is ushort ushortData)
        {
            return EncodeInteger(new BigInteger(ushortData));
        }
        if (data is uint uintData)
        {
            return EncodeInteger(new BigInteger(uintData));
        }
        throw new ArgumentException($"Unable to RLP Encode value of type {data.GetType().FullName}", nameof(data));
    }

    private static byte[] EncodeInteger(BigInteger integer)
    {
        if (BigInteger.IsNegative(integer))
        {
            throw new ArgumentOutOfRangeException("data", "Negative Numbers are not supported.");
        }
        if (integer.IsZero)
        {
            return [0x80];
        }
        return EncodeBytes(integer.ToByteArray(true, true));
    }
    private static byte[] EncodeBytes(byte[] data)
    {
        if (data.Length == 0)
        {
            return [0x80];
        }
        if (data.Length == 1 && data[0] < 0x80)
        {
            return data;
        }
        if (data.Length < 56)
        {
            return [(byte)(0x80 + data.Length), .. data];
        }
        var arrayLengthInBytes = new BigInteger(data.Length).ToByteArray(true, true);
        return [(byte)(0xb7 + arrayLengthInBytes.Length), .. arrayLengthInBytes, .. data];
    }

    private static byte[] EncodeList(params byte[][] items)
    {
        if (items.Length == 0)
        {
            return [0xc0];
        }
        var size = items.Sum(a => a.Length);
        byte[] data;
        if (size < 56)
        {
            data = new byte[size + 1];
            data[0] = (byte)(0xc0 + size);
        }
        else
        {
            var sizeInBytes = new BigInteger(size).ToByteArray(true, true);
            data = new byte[size + sizeInBytes.Length + 1];
            data[0] = (byte)(0xf7 + sizeInBytes.Length);
            Buffer.BlockCopy(sizeInBytes, 0, data, 1, sizeInBytes.Length);
        }
        var offset = data.Length - size;
        foreach (var item in items)
        {
            Buffer.BlockCopy(item, 0, data, offset, item.Length);
            offset += item.Length;
        }
        return data;
    }

    public static object[] Decode(byte[] data)
    {
        var list = new List<object>();
        var ptr = 0;
        while (ptr < data.Length)
        {
            var code = data[ptr];
            if (code <= 0x7f)
            {
                byte[] item = [code];
                list.Add(item);
                ptr++;
            }
            else if (code <= 0xb7)
            {
                int length = code - 0x80;
                byte[] item = new byte[length];
                Array.Copy(data, ptr + 1, item, 0, length);
                list.Add(item);
                ptr += length + 1;
            }
            else if (code <= 0xbf)
            {
                int lengthSize = code - 0xb7;
                int length = 0;
                for (int i = 0; i < lengthSize; i++)
                {
                    length = (length << 8) + data[ptr + 1 + i];
                }
                byte[] item = new byte[length];
                Array.Copy(data, ptr + lengthSize + 1, item, 0, length);
                list.Add(item);
                ptr += length + lengthSize + 1;
            }
            else if (code <= 0xf7)
            {
                int length = code - 0xc0;
                byte[] item = new byte[length];
                Array.Copy(data, ptr + 1, item, 0, length);
                list.Add(Decode(item));
                ptr += length + 1;
            }
            else
            {
                int lengthSize = code - 0xf7;
                int length = 0;
                for (int i = 0; i < lengthSize; i++)
                {
                    length = (length << 8) + data[ptr + 1 + i];
                }
                byte[] item = new byte[length];
                Array.Copy(data, ptr + lengthSize + 1, item, 0, length);
                list.Add(Decode(item));
                ptr += length + lengthSize + 1;
            }
        }
        return list.ToArray();
    }
}
