using Hiero;
using System;

namespace Proto;

public sealed partial class NftID
{
    internal NftID(Nft nft) : this()
    {
        if (nft is null || nft == Nft.None)
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
            return new Nft(new EntityId(id.TokenID.ShardNum, id.TokenID.RealmNum, id.TokenID.TokenNum), id.SerialNumber);
        }
        return Nft.None;
    }
}