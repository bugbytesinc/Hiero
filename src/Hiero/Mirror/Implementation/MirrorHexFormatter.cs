// SPDX-License-Identifier: Apache-2.0
using System.Buffers;
using System.Numerics;

namespace Hiero.Mirror.Implementation;

internal static class MirrorHexFormatter
{
    internal static string FormatPrefixed(ReadOnlySpan<byte> bytes)
    {
        return string.Create(2 + bytes.Length * 2, bytes, static (destination, source) =>
        {
            destination[0] = '0';
            destination[1] = 'x';
            Convert.TryToHexStringLower(source, destination[2..], out _);
        });
    }

    internal static string FormatPrefixedPadded32(BigInteger value)
    {
        var byteCount = value.GetByteCount(isUnsigned: true);
        var encodedByteCount = Math.Max(byteCount, 32);
        byte[]? rented = null;
        Span<byte> bytes = encodedByteCount <= 64
            ? stackalloc byte[encodedByteCount]
            : (rented = ArrayPool<byte>.Shared.Rent(encodedByteCount)).AsSpan(0, encodedByteCount);
        try
        {
            bytes.Clear();
            value.TryWriteBytes(bytes[(encodedByteCount - byteCount)..], out _, isUnsigned: true, isBigEndian: true);
            return FormatPrefixed(bytes);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }
}
