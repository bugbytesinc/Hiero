﻿namespace Hiero;
/// <summary>
/// Represents the balance of a crypto token held by an account.
/// </summary>
public record CryptoBalance
{
    /// <summary>
    /// The balance of crypto tokens held by the associated account
    /// in the smallest denomination.
    /// </summary>
    public ulong Balance { get; internal init; }
    /// <summary>
    /// The number of decimal places which each token may be subdivided.
    /// </summary>
    public uint Decimals { get; internal init; }
    /// <summary>
    /// Convenience operator returning the value of the 
    /// balance in the smallest denomination.
    /// </summary>
    /// <param name="cryptoBalance">The balance to retrieve the balance
    /// value from.</param>
    public static implicit operator ulong(CryptoBalance cryptoBalance)
    {
        return cryptoBalance.Balance;
    }
}