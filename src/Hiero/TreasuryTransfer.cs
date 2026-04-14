// SPDX-License-Identifier: Apache-2.0
namespace Hiero;

/// <summary>
/// Represents a change of Treasury for an NFT token .
/// </summary>
public sealed record TreasuryTransfer
{
    /// <summary>
    /// The Token ID of the class of NFT types that were transferred.
    /// </summary>
    public EntityId NftToken { get; private init; }
    /// <summary>
    /// The account sending the NFT.
    /// </summary>
    public EntityId PreviousTreasury { get; private init; }
    /// <summary>
    /// The account receiving the NFT.
    /// </summary>
    public EntityId NewTreasury { get; private init; }
    /// <summary>
    /// Public Constructor, an <code>NftTransfer</code> is immutable after creation.
    /// </summary>
    /// <param name="token">
    /// The address of the token identifying the class of NFTs.
    /// </param>
    /// <param name="previousTreasury">
    /// The previous treasury for the NFT.
    /// </param>
    /// <param name="newTreasury">
    /// The new treasury for the NFT.
    /// </param>
    internal TreasuryTransfer(EntityId token, EntityId previousTreasury, EntityId newTreasury)
    {
        NftToken = token;
        PreviousTreasury = previousTreasury;
        NewTreasury = newTreasury;
    }
}
