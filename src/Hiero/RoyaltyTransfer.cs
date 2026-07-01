// SPDX-License-Identifier: Apache-2.0
using Proto;

namespace Hiero;

/// <summary>
/// Represents a token or hBar transfer 
/// (Token, Payer, Amount, Address) fulfilling a royalty 
/// payment for the transfer of a token or asset.
/// </summary>
public sealed record RoyaltyTransfer
{
    private static readonly IReadOnlyList<EntityId> EmptyPayers = Array.AsReadOnly(Array.Empty<EntityId>());

    /// <summary>
    /// The token whose coins (or crypto)
    /// have been transferred to pay the royalty.
    /// </summary>
    public EntityId Token { get; private init; }
    /// <summary>
    /// The Payer(s) that were charged the assessed fee.
    /// </summary>
    public IReadOnlyList<EntityId> Payers { get; private init; }
    /// <summary>
    /// The account receiving the transferred token or crypto.
    /// </summary>
    public EntityId Receiver { get; private init; }
    /// <summary>
    /// The (divisible) amount of tokens or crypto transferred.
    /// </summary>
    public long Amount { get; init; }
    /// <summary>
    /// Internal Constructor representing the "None" 
    /// version of a royalty transfer.
    /// </summary>
    private RoyaltyTransfer()
    {
        Token = EntityId.None;
        Payers = EmptyPayers;
        Receiver = EntityId.None;
        Amount = 0;
    }
    /// <summary>
    /// Internal constructor to create a Royalty Transfer
    /// from raw protobuf.
    /// </summary>        
    internal RoyaltyTransfer(AssessedCustomFee fee)
    {
        Token = fee.TokenId.AsAddress();
        Receiver = fee.FeeCollectorAccountId.AsAddress();
        Amount = fee.Amount;
        var payerCount = fee.EffectivePayerAccountId.Count;
        if (payerCount == 0)
        {
            Payers = EmptyPayers;
        }
        else
        {
            var payers = new EntityId[payerCount];
            for (var i = 0; i < payerCount; i++)
            {
                payers[i] = fee.EffectivePayerAccountId[i].AsAddress();
            }
            Payers = Array.AsReadOnly(payers);
        }
    }
}
