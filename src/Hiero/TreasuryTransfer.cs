// SPDX-License-Identifier: Apache-2.0
namespace Hiero;

/// <summary>
/// Represents a change of Treasury for an NFT token.  The change
/// applies to the token as a whole and carries no serial number.
/// </summary>
public sealed record TreasuryTransfer
{
    /// <summary>
    /// The Token ID of the class of NFT types that were transferred.
    /// </summary>
    public EntityId NftToken { get; private init; }
    /// <summary>
    /// The token's treasury account before the change.
    /// </summary>
    public EntityId PreviousTreasury { get; private init; }
    /// <summary>
    /// The token's treasury account after the change.
    /// </summary>
    public EntityId NewTreasury { get; private init; }
    /// <summary>
    /// Internal Constructor, a <code>TreasuryTransfer</code> is immutable after creation.
    /// </summary>
    /// <param name="token">
    /// The address of the token identifying the class of NFTs.
    /// </param>
    /// <param name="previousTreasury">
    /// The token's treasury account before the change.
    /// </param>
    /// <param name="newTreasury">
    /// The token's treasury account after the change.
    /// </param>
    internal TreasuryTransfer(EntityId token, EntityId previousTreasury, EntityId newTreasury)
    {
        NftToken = token;
        PreviousTreasury = previousTreasury;
        NewTreasury = newTreasury;
    }
}
