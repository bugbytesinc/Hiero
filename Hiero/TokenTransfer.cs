namespace Hiero;

/// <summary>
/// Represents a token transfer (Token, Address, Amount)
/// </summary>
public sealed record TokenTransfer
{
    /// <summary>
    /// The identifier of the Token type that was transferred.
    /// </summary>
    public EntityId Token { get; private init; }
    /// <summary>
    /// The Payer receiving or sending the token's coins.
    /// </summary>
    public EntityId Account { get; private init; }
    /// <summary>
    /// The (divisible) amount of coins transferred.  Negative values
    /// indicate an outflow of coins to the <code>Address</code> positive
    /// values indicate an inflow of coins from the associated <code>Address</code>.
    /// </summary>
    public long Amount { get; init; }
    /// <summary>
    /// Indicates the parties involved in the transaction
    /// are acting as delegates thru a granted allowance.
    /// </summary>
    public bool Delegated { get; private init; }
    /// <summary>
    /// Public Constructor, an <code>TokenTransfer</code> is immutable after creation.
    /// </summary>
    /// <param name="token">
    /// The Payer of the Token who's coins have transferred.
    /// </param>
    /// <param name="address">
    /// The Payer receiving or sending the token's coins.
    /// </param>
    /// <param name="amount">
    /// The (divisible) amount of coins transferred.  Negative values
    /// indicate an outflow of coins to the <code>Address</code> positive
    /// values indicate an inflow of coins from the associated <code>Address</code>.
    /// </param>
    /// <param name="delegated">
    /// Indicates the parties involved in the transaction
    /// are acting as delegates thru a granted allowance.
    /// </param>
    public TokenTransfer(EntityId token, EntityId address, long amount, bool delegated = false)
    {
        Token = token;
        Account = address;
        Amount = amount;
        Delegated = delegated;
    }
}