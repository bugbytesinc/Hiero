// SPDX-License-Identifier: Apache-2.0
using Hiero;

namespace Proto;

public sealed partial class NftID
{
    internal NftID(Hiero.Nft nft) : this()
    {
        if (nft is null || nft == Hiero.Nft.None)
        {
            throw new ArgumentNullException(nameof(nft), "NFT is missing. Please check that it is not null or empty.");
        }
        TokenID = new TokenID(nft);
        SerialNumber = nft.SerialNumber;
    }
}
internal static class NftIDExtensions
{
    internal static Hiero.Nft AsNft(this NftID? id)
    {
        if (id is not null && id.TokenID is not null)
        {
            return new Hiero.Nft(new EntityId(id.TokenID.ShardNum, id.TokenID.RealmNum, id.TokenID.TokenNum), id.SerialNumber);
        }
        return Hiero.Nft.None;
    }
}