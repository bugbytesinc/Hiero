// SPDX-License-Identifier: Apache-2.0
using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Hiero.Implementation.Parsing;

internal static class NftParser
{
    internal static bool TryParse(string? value, [NotNullWhen(true)] out Nft? nft)
    {
        nft = null;
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
            return TryParse(buffer, out nft);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    internal static bool TryParse(ReadOnlySpan<byte> value, [NotNullWhen(true)] out Nft? nft)
    {
        nft = null;
        int separator = value.IndexOf((byte)'#');
        if (separator <= 0 || separator >= value.Length - 1)
        {
            return false;
        }
        var serialText = value[(separator + 1)..];
        if (!ShardRealmNumParser.TryParse(value[..separator], out var token) ||
            !Utf8Parser.TryParse(serialText, out uint serial, out var serialConsumed) ||
            serialConsumed != serialText.Length)
        {
            return false;
        }
        if (serial == 0)
        {
            if (token == EntityId.None)
            {
                nft = Nft.None;
                return true;
            }
            return false;
        }
        nft = new Nft(token, serial);
        return true;
    }
}
