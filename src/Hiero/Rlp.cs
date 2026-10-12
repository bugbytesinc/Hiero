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
    /// RLP-encodes one or more data items into a byte array.  A single
    /// argument is encoded as that item alone; two or more arguments are
    /// encoded as an RLP list.  Calling with no arguments produces an
    /// empty list (<c>0xC0</c>), and a <c>null</c> array produces
    /// <c>0x80</c>.
    /// </summary>
    /// <param name="data">
    /// The data items to encode: <c>byte[]</c>,
    /// <c>ReadOnlyMemory&lt;byte&gt;</c>, <c>string</c>, non-negative
    /// <c>int</c>, <c>long</c>, <c>ulong</c>, <c>short</c>, <c>ushort</c>,
    /// <c>uint</c> and <c>BigInteger</c> values, <c>decimal</c> values
    /// (the fractional part is truncated), <c>null</c> (encoded as an
    /// empty string), and nested arrays of these (encoded as lists).
    /// Other types, such as <c>byte</c>, <c>sbyte</c>, <c>float</c> and
    /// <c>double</c>, are not supported.
    /// </param>
    /// <returns>The RLP-encoded byte array.</returns>
    /// <exception cref="ArgumentException">If a data item is of an unsupported type.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If a numeric data item is negative.</exception>
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
    /// <exception cref="FormatException">If the data is not valid RLP: an item's declared length runs past the end of the data, or a length prefix is too long to describe data held in memory.</exception>
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
            var (start, length, isList) = ReadItemPrefix(data, ptr);
            var payload = data.Slice(start, length);
            result[index++] = isList ? DecodeItems(payload) : payload.ToArray();
            ptr = start + length;
        }
        return result;
    }

    private static int CountItems(ReadOnlySpan<byte> data)
    {
        var count = 0;
        var ptr = 0;
        while (ptr < data.Length)
        {
            var (start, length, _) = ReadItemPrefix(data, ptr);
            // Every item occupies at least one byte, so this always advances.
            ptr = start + length;
            count++;
        }
        return count;
    }

    /// <summary>
    /// Reads the prefix of the item at the given offset: where its payload
    /// starts, how long it is, and whether it is a list. Checks that the
    /// payload lies within the data, so a crafted length can neither run
    /// past the end nor stall the decoder.
    /// </summary>
    private static (int Start, int Length, bool IsList) ReadItemPrefix(ReadOnlySpan<byte> data, int ptr)
    {
        var code = data[ptr];
        int start;
        int length;
        bool isList;
        if (code <= 0x7f)
        {
            return (ptr, 1, false);
        }
        else if (code <= 0xb7)
        {
            (start, length, isList) = (ptr + 1, code - 0x80, false);
        }
        else if (code <= 0xbf)
        {
            (start, length) = ReadLongLength(data, ptr, code - 0xb7);
            isList = false;
        }
        else if (code <= 0xf7)
        {
            (start, length, isList) = (ptr + 1, code - 0xc0, true);
        }
        else
        {
            (start, length) = ReadLongLength(data, ptr, code - 0xf7);
            isList = true;
        }
        if (length > data.Length - start)
        {
            throw new FormatException($"The RLP item at offset {ptr} declares {length} bytes of content, but only {data.Length - start} remain.");
        }
        return (start, length, isList);
    }

    private static (int Start, int Length) ReadLongLength(ReadOnlySpan<byte> data, int ptr, int lengthSize)
    {
        if (lengthSize > 4)
        {
            throw new FormatException($"The RLP item at offset {ptr} has a {lengthSize}-byte length prefix, describing more data than can be held in memory.");
        }
        if (lengthSize > data.Length - ptr - 1)
        {
            throw new FormatException($"The RLP item at offset {ptr} is truncated within its length prefix.");
        }
        long length = 0;
        foreach (var b in data.Slice(ptr + 1, lengthSize))
        {
            length = (length << 8) | b;
        }
        if (length > int.MaxValue)
        {
            throw new FormatException($"The RLP item at offset {ptr} declares {length} bytes of content, more than can be held in memory.");
        }
        return (ptr + 1 + lengthSize, (int)length);
    }
}
