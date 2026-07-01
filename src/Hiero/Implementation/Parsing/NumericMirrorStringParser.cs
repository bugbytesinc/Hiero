// SPDX-License-Identifier: Apache-2.0
using System.Buffers;
using System.Buffers.Text;
using System.Text;
using System.Text.Json;

namespace Hiero.Implementation.Parsing;

internal static class NumericMirrorStringParser
{
    private delegate bool SpanParser<T>(ReadOnlySpan<byte> span, out T value);

    public static bool TryGetInt32(ref Utf8JsonReader reader, out int value)
    {
        if (reader.HasValueSequence || reader.ValueIsEscaped)
        {
            return TryParseString(reader.GetString(), TryParseInt32, out value);
        }

        return TryParseInt32(reader.ValueSpan, out value);
    }

    public static bool TryGetInt64(ref Utf8JsonReader reader, out long value)
    {
        if (reader.HasValueSequence || reader.ValueIsEscaped)
        {
            return TryParseString(reader.GetString(), TryParseInt64, out value);
        }

        return TryParseInt64(reader.ValueSpan, out value);
    }

    public static bool TryGetUInt64(ref Utf8JsonReader reader, out ulong value)
    {
        if (reader.HasValueSequence || reader.ValueIsEscaped)
        {
            return TryParseString(reader.GetString(), TryParseUInt64, out value);
        }

        return TryParseUInt64(reader.ValueSpan, out value);
    }

    public static bool TryGetDouble(ref Utf8JsonReader reader, out double value)
    {
        if (reader.HasValueSequence || reader.ValueIsEscaped)
        {
            return TryParseString(reader.GetString(), TryParseDouble, out value);
        }

        return TryParseDouble(reader.ValueSpan, out value);
    }

    private static bool TryParseInt32(ReadOnlySpan<byte> source, out int value)
    {
        var span = TrimAsciiWhiteSpace(source);
        return Utf8Parser.TryParse(span, out value, out var consumed) && consumed == span.Length;
    }

    private static bool TryParseInt64(ReadOnlySpan<byte> source, out long value)
    {
        var span = TrimAsciiWhiteSpace(source);
        return Utf8Parser.TryParse(span, out value, out var consumed) && consumed == span.Length;
    }

    private static bool TryParseUInt64(ReadOnlySpan<byte> source, out ulong value)
    {
        var span = TrimAsciiWhiteSpace(source);
        return Utf8Parser.TryParse(span, out value, out var consumed) && consumed == span.Length;
    }

    private static bool TryParseDouble(ReadOnlySpan<byte> source, out double value)
    {
        var span = TrimAsciiWhiteSpace(source);
        if (Utf8Parser.TryParse(span, out value, out var consumed) && consumed == span.Length)
        {
            return true;
        }
        value = default;
        return false;
    }

    private static bool TryParseString<T>(string? value, SpanParser<T> parser, out T result)
    {
        if (value is null)
        {
            result = default!;
            return false;
        }
        var byteCount = Encoding.UTF8.GetByteCount(value);
        byte[]? rented = null;
        Span<byte> buffer = byteCount <= 128
            ? stackalloc byte[byteCount]
            : (rented = ArrayPool<byte>.Shared.Rent(byteCount)).AsSpan(0, byteCount);
        try
        {
            Encoding.UTF8.GetBytes(value, buffer);
            return parser(buffer, out result);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static ReadOnlySpan<byte> TrimAsciiWhiteSpace(ReadOnlySpan<byte> span)
    {
        var start = 0;
        var end = span.Length - 1;
        while (start <= end && IsAsciiWhiteSpace(span[start]))
        {
            start++;
        }
        while (end >= start && IsAsciiWhiteSpace(span[end]))
        {
            end--;
        }
        return span[start..(end + 1)];
    }

    private static bool IsAsciiWhiteSpace(byte value)
    {
        return value is (byte)' ' or (byte)'\t' or (byte)'\n' or (byte)'\r';
    }
}
