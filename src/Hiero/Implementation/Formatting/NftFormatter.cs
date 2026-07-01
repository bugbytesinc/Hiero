// SPDX-License-Identifier: Apache-2.0
using System.Text;

namespace Hiero.Implementation.Formatting;

internal static class NftFormatter
{
    internal static string Format(Nft? nft)
    {
        if (nft is null)
        {
            return string.Empty;
        }
        Span<byte> buffer = stackalloc byte[96];
        if (TryFormat(nft, buffer, out var bytesWritten))
        {
            return Encoding.ASCII.GetString(buffer[..bytesWritten]);
        }
        return $"{nft.Token}#{nft.SerialNumber}";
    }

    internal static bool TryFormat(Nft nft, Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;
        var token = nft.Token;
        if (token is null || !token.IsShardRealmNum)
        {
            return false;
        }
        return Utf8Format.TryAppend(token.ShardNum, destination, ref bytesWritten) &&
            Utf8Format.TryAppend((byte)'.', destination, ref bytesWritten) &&
            Utf8Format.TryAppend(token.RealmNum, destination, ref bytesWritten) &&
            Utf8Format.TryAppend((byte)'.', destination, ref bytesWritten) &&
            Utf8Format.TryAppend(token.AccountNum, destination, ref bytesWritten) &&
            Utf8Format.TryAppend((byte)'#', destination, ref bytesWritten) &&
            Utf8Format.TryAppend(nft.SerialNumber, destination, ref bytesWritten);
    }
}
