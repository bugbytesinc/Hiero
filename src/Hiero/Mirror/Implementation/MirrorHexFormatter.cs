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

    private static readonly BigInteger TwoToThe256 = BigInteger.One << 256;
    private static readonly BigInteger MinSigned256 = -(BigInteger.One << 255);

    /// <summary>
    /// Formats a 32-byte EVM word. A negative value, as a signed parse of a
    /// word with its top bit set produces, is written as its 256-bit two's
    /// complement.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If the value is negative and does not fit in a signed 256-bit word.
    /// </exception>
    internal static string FormatPrefixedPadded32(BigInteger value)
    {
        if (value.Sign < 0)
        {
            if (value < MinSigned256)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "A negative value must fit in a signed 256-bit word.");
            }
            value += TwoToThe256;
        }
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
