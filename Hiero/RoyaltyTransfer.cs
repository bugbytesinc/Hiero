using Proto;
using System.Collections.Generic;
using System.Linq;

namespace Hiero;

/// <summary>
/// Represents a token or hBar transfer 
/// (Token, Payer, Amount, Address) fufilling a royalty 
/// payment for the transfer of a token or asset.
/// </summary>
public sealed record RoyaltyTransfer
{
    /// <summary>
    /// The Payer of the token who's coins (or crypto)
    /// have been transferred to pay the royalty.
    /// </summary>
    public EntityId Token { get; private init; }
    /// <summary>
    /// The Payer(s) that were charged the assessed fee.
    /// </summary>
    public IReadOnlyList<EntityId> Payers { get; private init; }
    /// <summary>
    /// The Payer receiving the transferred token or crypto.
    /// </summary>
    public EntityId Receiver { get; private init; }
    /// <summary>
    /// The (divisible) amount of tokens or crypto transferred.
    /// </summary>
    public long Amount { get; init; }
    /// <summary>
    /// Internal Constructor representing the "None" 
    /// version of an royalty transfer.
    /// </summary>
    private RoyaltyTransfer()
    {
        Token = EntityId.None;
        Payers = new List<EntityId>().AsReadOnly();
        Receiver = EntityId.None;
        Amount = 0;
    }
    /// <summary>
    /// Internal Helper Class to Create Royalty Transfer
    /// from raw protobuf.
    /// </summary>        
    internal RoyaltyTransfer(AssessedCustomFee fee)
    {
        Token = fee.TokenId.AsAddress();
        Receiver = fee.FeeCollectorAccountId.AsAddress();
        Amount = fee.Amount;
        Payers = fee.EffectivePayerAccountId.Select(payerID => payerID.AsAddress()).ToList().AsReadOnly();
    }
}