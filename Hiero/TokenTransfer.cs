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
    /// The account receiving or sending the token's coins.
    /// </summary>
    public EntityId Account { get; private init; }
    /// <summary>
    /// The (divisible) amount of coins transferred.  Negative values
    /// indicate an outflow of coins from the account. Positive
    /// values indicate an inflow of coins to the account.
    /// </summary>
    public long Amount { get; init; }
    /// <summary>
    /// Indicates the parties involved in the transaction
    /// are acting as delegates through a granted allowance.
    /// </summary>
    public bool Delegated { get; private init; }
    /// <summary>
    /// Optional allowance hook call for this token transfer.
    /// The hook's <see cref="HookCall.CallMode"/> determines
    /// whether it is invoked before the transfer only, or both
    /// before and after.
    /// </summary>
    public HookCall? AllowanceHook { get; private init; }
    /// <summary>
    /// Public Constructor, an <code>TokenTransfer</code> is immutable after creation.
    /// </summary>
    /// <param name="token">
    /// The identifier of the Token whose coins were transferred.
    /// </param>
    /// <param name="address">
    /// The account receiving or sending the token's coins.
    /// </param>
    /// <param name="amount">
    /// The (divisible) amount of coins transferred.  Negative values
    /// indicate an outflow of coins from the account. Positive
    /// values indicate an inflow of coins to the account.
    /// </param>
    /// <param name="delegated">
    /// Indicates the parties involved in the transaction
    /// are acting as delegates through a granted allowance.
    /// </param>
    /// <param name="allowanceHook">
    /// Optional allowance hook call for this token transfer.
    /// </param>
    public TokenTransfer(EntityId token, EntityId address, long amount, bool delegated = false, HookCall? allowanceHook = null)
    {
        Token = token;
        Account = address;
        Amount = amount;
        Delegated = delegated;
        AllowanceHook = allowanceHook;
    }
}
