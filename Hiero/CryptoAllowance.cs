using Proto;
using System;

namespace Hiero;

/// <summary>
/// Represents an allowance allocation permitting a
/// spender account privleges of spending the specified
/// amount of hBars from the owning account.
/// </summary>
public sealed record CryptoAllowance
{
    /// <summary>
    /// The account holding the hBars that
    /// may be spent by the delegate spender.
    /// </summary>
    public EntityId Owner { get; private init; }
    /// <summary>
    /// The account that may spend the allocated
    /// allowance of hBars
    /// </summary>
    public EntityId Spender { get; private init; }
    /// <summary>
    /// The specific amount of hBars that the 
    /// spender may spend from the owner's account.
    /// </summary>
    public long Amount { get; private init; }
    /// <summary>
    /// Represents an allowance allocation permitting a
    /// delegate account privleges of spending the specified
    /// amount of hBars from the owning account.
    /// </summary>
    /// <param name="owner">
    /// The account holding the hBars that
    /// may be spent by the delegate spender.
    /// </param>
    /// <param name="spender">
    /// The account that may spend the allocated
    /// allowance of hBars
    /// </param>
    /// <param name="amount">
    /// The specific amount of hBars that the 
    /// spender may spend from the owner's account.
    /// </param>
    /// <exception cref="ArgumentException">
    /// If any of the addresses are null or empty.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If the amount of allowance is less than zero.
    /// </exception>
    public CryptoAllowance(EntityId owner, EntityId spender, long amount)
    {
        if (owner.IsNullOrNone())
        {
            throw new ArgumentException(nameof(owner), "The allowance owner account cannot be null or empty.");
        }
        if (spender.IsNullOrNone())
        {
            throw new ArgumentException(nameof(spender), "The allowance spender account cannot be null or empty.");
        }
        else if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "The allowance amount must be greater than or equal to zero.");
        }
        Owner = owner;
        Spender = spender;
        Amount = amount;
    }
    /// <summary>
    /// Helper constructor creating a crypto 
    /// allowance grant from the protobuf message.
    /// </summary>
    internal CryptoAllowance(GrantedCryptoAllowance allowance, EntityId owner)
    {
        if (allowance is not null)
        {
            Owner = owner;
            Spender = allowance.Spender.AsAddress();
            Amount = allowance.Amount;
        }
        else
        {
            Owner = owner;
            Spender = EntityId.None;
            Amount = 0;
        }
    }
}