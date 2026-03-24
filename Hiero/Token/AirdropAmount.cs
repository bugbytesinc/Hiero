// SPDX-License-Identifier: Apache-2.0
using Proto;

namespace Hiero;

/// <summary>
/// The identifier of an airdrop token, sender and receiver 
/// paired with the amount of token(s) to be transferred.
/// </summary>
public sealed record AirdropAmount
{
    /// <summary>
    /// Identifier of the airdrop.
    /// </summary>
    public Airdrop Airdrop { get; private init; }
    /// <summary>
    /// The amount of fungible tokens to be transferred
    /// or one if the airdrop is for an NFT.
    /// </summary>
    public ulong Amount { get; private init; }
    /// <summary>
    /// Internal Constructor
    /// </summary>
    internal AirdropAmount(PendingAirdropRecord record)
    {
        var airdrop = record?.PendingAirdropId;
        if (airdrop != null)
        {
            if (airdrop.TokenReferenceCase == PendingAirdropId.TokenReferenceOneofCase.FungibleTokenType)
            {
                Airdrop = new(airdrop.SenderId.AsAddress(), airdrop.ReceiverId.AsAddress(), airdrop.FungibleTokenType.AsAddress());
                Amount = record!.PendingAirdropValue?.Amount ?? 0;
                return;
            }
            else if (airdrop.TokenReferenceCase == PendingAirdropId.TokenReferenceOneofCase.NonFungibleToken)
            {
                Airdrop = new(airdrop.SenderId.AsAddress(), airdrop.ReceiverId.AsAddress(), airdrop.NonFungibleToken.AsNft());
                Amount = 1;
                return;
            }
        }
        Airdrop = new(EntityId.None, EntityId.None, EntityId.None);
        Amount = 0;
    }
}
