// SPDX-License-Identifier: Apache-2.0
using System.Numerics;
using System.Text;

namespace Hiero;

/// <summary>
/// Utility class for Recursive Length Prefix (RLP) encoding and decoding,
/// used for serializing data in EVM-compatible transactions.
/// </summary>
public static class Rlp
{
    /// <summary>
    /// RLP-encodes one or more data items into a byte array.
    /// </summary>
    /// <param name="data">The data items to encode. Supports byte arrays, strings, numeric types, and nested arrays.</param>
    /// <returns>The RLP-encoded byte array.</returns>
    /// <exception cref="ArgumentException">If a data item is of an unsupported type.</exception>
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
    /// <summary>
    /// Decodes an RLP-encoded byte array into its constituent data items.
    /// </summary>
    /// <param name="data">The RLP-encoded byte array to decode.</param>
    /// <returns>An array of decoded objects, where each element is either a byte array or a nested array of objects.</returns>
    public static object[] Decode(byte[] data)
    {
        return DecodeItems(data);
    }
    internal static byte[] EncodeEvmTransaction(long nonce, long gasPrice, long gasLimit, ReadOnlySpan<byte> toAddress, BigInteger value, ReadOnlyMemory<byte> data, BigInteger v, byte[]? r, byte[]? s)
    {
        return EncodeList(
            EncodeInteger(new BigInteger(nonce)),
            EncodeInteger(new BigInteger(gasPrice)),
            EncodeInteger(new BigInteger(gasLimit)),
            EncodeByteSpan(toAddress),
            EncodeInteger(value),
            EncodeByteSpan(data.Span),
            EncodeInteger(v),
            EncodeData(r),
            EncodeData(s));
    }
    private static byte[] EncodeData(object? data)
    {
        if (data == null)
        {
            return [0x80];
        }
        if (data is byte[] byteData)
        {
            return EncodeByteArray(byteData);
        }
        if (data is Array array)
        {
            return EncodeRuntimeArray(array);
        }
        if (data is ReadOnlyMemory<byte> readOnlyMemory)
        {
            return EncodeByteSpan(readOnlyMemory.Span);
        }
        if (data is string stringData)
        {
            return EncodeString(stringData);
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
    private static byte[] EncodeRuntimeArray(Array data)
    {
        var items = new byte[data.Length][];
        if (data is object?[] objectData)
        {
            for (var i = 0; i < objectData.Length; i++)
            {
                items[i] = EncodeData(objectData[i]);
            }
        }
        else
        {
            for (var i = 0; i < data.Length; i++)
            {
                items[i] = EncodeData(data.GetValue(i));
            }
        }
        return EncodeList(items);
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
        return EncodeByteArray(integer.ToByteArray(true, true));
    }
    private static byte[] EncodeByteArray(byte[] data)
    {
        if (data.Length == 0)
        {
            return [0x80];
        }
        if (data.Length == 1 && data[0] < 0x80)
        {
            return data;
        }
        return EncodeByteSpan(data.AsSpan());
    }
    private static byte[] EncodeByteSpan(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
        {
            return [0x80];
        }
        if (data.Length == 1 && data[0] < 0x80)
        {
            return [data[0]];
        }
        var (buffer, payloadOffset) = AllocatePrefixedByteStringBuffer(data.Length);
        data.CopyTo(buffer.AsSpan(payloadOffset));
        return buffer;
    }
    private static byte[] EncodeString(string data)
    {
        var byteCount = Encoding.UTF8.GetByteCount(data);
        if (byteCount == 0)
        {
            return [0x80];
        }
        if (byteCount == 1)
        {
            Span<byte> single = stackalloc byte[1];
            Encoding.UTF8.GetBytes(data.AsSpan(), single);
            if (single[0] < 0x80)
            {
                return [single[0]];
            }
            return [0x81, single[0]];
        }

        var (buffer, payloadOffset) = AllocatePrefixedByteStringBuffer(byteCount);
        Encoding.UTF8.GetBytes(data.AsSpan(), buffer.AsSpan(payloadOffset));
        return buffer;
    }
    private static (byte[] buffer, int payloadOffset) AllocatePrefixedByteStringBuffer(int payloadLength)
    {
        if (payloadLength < 56)
        {
            var result = new byte[payloadLength + 1];
            result[0] = (byte)(0x80 + payloadLength);
            return (result, 1);
        }
        var lengthByteCount = GetLengthByteCount(payloadLength);
        var longResult = new byte[payloadLength + lengthByteCount + 1];
        longResult[0] = (byte)(0xb7 + lengthByteCount);
        WriteLength(longResult.AsSpan(1, lengthByteCount), payloadLength);
        return (longResult, lengthByteCount + 1);
    }

    private static byte[] EncodeList(params byte[][] items)
    {
        if (items.Length == 0)
        {
            return [0xc0];
        }
        var size = 0;
        for (var i = 0; i < items.Length; i++)
        {
            size += items[i].Length;
        }
        byte[] data;
        if (size < 56)
        {
            data = new byte[size + 1];
            data[0] = (byte)(0xc0 + size);
        }
        else
        {
            var sizeByteCount = GetLengthByteCount(size);
            data = new byte[size + sizeByteCount + 1];
            data[0] = (byte)(0xf7 + sizeByteCount);
            WriteLength(data.AsSpan(1, sizeByteCount), size);
        }
        var offset = data.Length - size;
        foreach (var item in items)
        {
            Buffer.BlockCopy(item, 0, data, offset, item.Length);
            offset += item.Length;
        }
        return data;
    }
    private static byte[] EncodeList(byte[] item0, byte[] item1, byte[] item2, byte[] item3, byte[] item4, byte[] item5, byte[] item6, byte[] item7, byte[] item8)
    {
        var size = item0.Length +
            item1.Length +
            item2.Length +
            item3.Length +
            item4.Length +
            item5.Length +
            item6.Length +
            item7.Length +
            item8.Length;
        byte[] data;
        if (size < 56)
        {
            data = new byte[size + 1];
            data[0] = (byte)(0xc0 + size);
        }
        else
        {
            var sizeByteCount = GetLengthByteCount(size);
            data = new byte[size + sizeByteCount + 1];
            data[0] = (byte)(0xf7 + sizeByteCount);
            WriteLength(data.AsSpan(1, sizeByteCount), size);
        }
        var offset = data.Length - size;
        CopyListItem(item0, data, ref offset);
        CopyListItem(item1, data, ref offset);
        CopyListItem(item2, data, ref offset);
        CopyListItem(item3, data, ref offset);
        CopyListItem(item4, data, ref offset);
        CopyListItem(item5, data, ref offset);
        CopyListItem(item6, data, ref offset);
        CopyListItem(item7, data, ref offset);
        CopyListItem(item8, data, ref offset);
        return data;
    }
    private static void CopyListItem(byte[] item, byte[] destination, ref int offset)
    {
        Buffer.BlockCopy(item, 0, destination, offset, item.Length);
        offset += item.Length;
    }
    private static int GetLengthByteCount(int value)
    {
        var count = 1;
        while ((value >>= 8) > 0)
        {
            count++;
        }
        return count;
    }
    private static void WriteLength(Span<byte> destination, int value)
    {
        for (var i = destination.Length - 1; i >= 0; i--)
        {
            destination[i] = (byte)value;
            value >>= 8;
        }
    }

    private static object[] DecodeItems(ReadOnlySpan<byte> data)
    {
        var result = new object[CountItems(data)];
        var ptr = 0;
        var index = 0;
        while (ptr < data.Length)
        {
            var code = data[ptr];
            if (code <= 0x7f)
            {
                byte[] item = [code];
                result[index++] = item;
                ptr++;
            }
            else if (code <= 0xb7)
            {
                int length = code - 0x80;
                byte[] item = new byte[length];
                data.Slice(ptr + 1, length).CopyTo(item);
                result[index++] = item;
                ptr += length + 1;
            }
            else if (code <= 0xbf)
            {
                int lengthSize = code - 0xb7;
                int length = ReadLength(data.Slice(ptr + 1, lengthSize));
                byte[] item = new byte[length];
                data.Slice(ptr + lengthSize + 1, length).CopyTo(item);
                result[index++] = item;
                ptr += length + lengthSize + 1;
            }
            else if (code <= 0xf7)
            {
                int length = code - 0xc0;
                result[index++] = DecodeItems(data.Slice(ptr + 1, length));
                ptr += length + 1;
            }
            else
            {
                int lengthSize = code - 0xf7;
                int length = ReadLength(data.Slice(ptr + 1, lengthSize));
                result[index++] = DecodeItems(data.Slice(ptr + lengthSize + 1, length));
                ptr += length + lengthSize + 1;
            }
        }
        return result;
    }

    private static int CountItems(ReadOnlySpan<byte> data)
    {
        var count = 0;
        var ptr = 0;
        while (ptr < data.Length)
        {
            var code = data[ptr];
            if (code <= 0x7f)
            {
                ptr++;
            }
            else if (code <= 0xb7)
            {
                ptr += code - 0x80 + 1;
            }
            else if (code <= 0xbf)
            {
                int lengthSize = code - 0xb7;
                ptr += ReadLength(data.Slice(ptr + 1, lengthSize)) + lengthSize + 1;
            }
            else if (code <= 0xf7)
            {
                ptr += code - 0xc0 + 1;
            }
            else
            {
                int lengthSize = code - 0xf7;
                ptr += ReadLength(data.Slice(ptr + 1, lengthSize)) + lengthSize + 1;
            }
            count++;
        }
        return count;
    }

    private static int ReadLength(ReadOnlySpan<byte> source)
    {
        var length = 0;
        for (var i = 0; i < source.Length; i++)
        {
            length = (length << 8) + source[i];
        }
        return length;
    }
}
