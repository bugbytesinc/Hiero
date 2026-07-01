// SPDX-License-Identifier: Apache-2.0
using System.Buffers.Text;

namespace Hiero.Implementation.Formatting;

internal static class Utf8Format
{
    internal static bool TryAppend(long value, Span<byte> destination, ref int offset)
    {
        if (!Utf8Formatter.TryFormat(value, destination[offset..], out var bytesWritten))
        {
            return false;
        }
        offset += bytesWritten;
        return true;
    }

    internal static bool TryAppendPaddedNineDigits(int value, Span<byte> destination, ref int offset)
    {
        if (!Utf8Formatter.TryFormat(value, destination[offset..], out var bytesWritten, new('D', 9)))
        {
            return false;
        }
        offset += bytesWritten;
        return true;
    }

    internal static bool TryAppend(byte value, Span<byte> destination, ref int offset)
    {
        if (offset >= destination.Length)
        {
            return false;
        }
        destination[offset++] = value;
        return true;
    }

    internal static bool TryAppend(ReadOnlySpan<byte> value, Span<byte> destination, ref int offset)
    {
        if (value.Length > destination.Length - offset)
        {
            return false;
        }
        value.CopyTo(destination[offset..]);
        offset += value.Length;
        return true;
    }
}
