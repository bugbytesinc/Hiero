﻿using Proto;

namespace Hiero;

/// <summary>
/// Represents an allowance allocation permitting a
/// delegate account privleges of spending the specified
/// amount of tokens from the owning account.
/// </summary>
public sealed record TokenAllowance
{
    /// <summary>
    /// The address of the token that having
    /// the allocated allowance.
    /// </summary>
    public EntityId Token { get; private init; }
    /// <summary>
    /// The Address owner holding the tokens that
    /// may be spent by the delegate.
    /// </summary>
    public EntityId Owner { get; private init; }
    /// <summary>
    /// The account that may spend the allocated
    /// allowance of tokens.
    /// </summary>
    public EntityId Agent { get; private init; }
    /// <summary>
    /// The increase or decrease of the amount of
    /// tokens that the delegate may spend.
    /// </summary>
    public long Amount { get; private init; }
    /// <summary>
    /// Represents an allowance allocation permitting a
    /// agent account privleges of spending the specified
    /// amount of tokens from the owning account.
    /// </summary>
    /// <param name="token">
    /// The address of the token that having
    /// the allocated allowance.
    /// </param>
    /// <param name="owner">
    /// The Address owner holding the tokens that
    /// may be spent by the delegate.
    /// </param>
    /// <param name="agent">
    /// The account that may spend the allocated
    /// allowance of tokens.
    /// </param>
    /// <param name="amount">
    /// The increase or decrease of the amount of
    /// tokens that the delegate may spend.
    /// </param>
    /// <exception cref="ArgumentException">
    /// If any of the address are null or None.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If the amount is zero.
    /// </exception>
    public TokenAllowance(EntityId token, EntityId owner, EntityId agent, long amount)
    {
        if (token.IsNullOrNone())
        {
            throw new ArgumentException(nameof(token), "The allowance token cannot be null or empty.");
        }
        if (owner.IsNullOrNone())
        {
            throw new ArgumentException(nameof(owner), "The allowance owner account cannot be null or empty.");
        }
        if (agent.IsNullOrNone())
        {
            throw new ArgumentException(nameof(agent), "The allowance spender account cannot be null or empty.");
        }
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "The allowance amount must be greater than or equal to zero.");
        }
        Token = token;
        Owner = owner;
        Agent = agent;
        Amount = amount;
    }
    /// <summary>
    /// Internal helper function creating an allowance
    /// representation from protobuf object.
    /// </summary>
    internal TokenAllowance(GrantedTokenAllowance allowance, EntityId owner)
    {
        if (allowance is not null)
        {
            Token = allowance.TokenId.AsAddress();
            Owner = owner;
            Agent = allowance.Spender.AsAddress();
            Amount = allowance.Amount;
        }
        else
        {
            Token = EntityId.None;
            Owner = owner;
            Agent = EntityId.None;
            Amount = 0;
        }
    }
}