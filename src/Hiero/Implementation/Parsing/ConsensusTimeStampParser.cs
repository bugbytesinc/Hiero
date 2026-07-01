// SPDX-License-Identifier: Apache-2.0
using System.Buffers;
using System.Buffers.Text;
using System.Text;

namespace Hiero.Implementation.Parsing;

internal static class ConsensusTimeStampParser
{
    internal static bool TryParse(string? value, out ConsensusTimeStamp timeStamp)
    {
        timeStamp = default;
        if (value is null)
        {
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
            return TryParse(buffer, out timeStamp);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    internal static bool TryParse(ReadOnlySpan<byte> value, out ConsensusTimeStamp timeStamp)
    {
        if (Utf8Parser.TryParse(value, out decimal seconds, out var bytesConsumed) &&
            bytesConsumed == value.Length)
        {
            timeStamp = new ConsensusTimeStamp(seconds);
            return true;
        }
        timeStamp = default;
        return false;
    }
}
