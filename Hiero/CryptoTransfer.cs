namespace Hiero;
/// <summary>
/// Represents a token transfer (Token, Address, Amount)
/// </summary>
public sealed record CryptoTransfer
{
    /// <summary>
    /// The account receiving or sending the crypto.
    /// </summary>
    public EntityId Address { get; private init; }
    /// <summary>
    /// The amount of crypto transferred in tinybars.  Negative values
    /// indicate an outflow of tinybars to the <code>Address</code> positive
    /// values indicate an inflow of tinybars from the associated <code>Address</code>.
    /// </summary>
    public long Amount { get; private init; }
    /// <summary>
    /// Indicates the parties involved in the transaction
    /// are acting as delegates through a granted allowance.
    /// </summary>
    public bool Delegated { get; private init; }
    /// <summary>
    /// Internal Constructor representing the "None" 
    /// version of an transfer.
    /// </summary>
    private CryptoTransfer()
    {
        Address = Hiero.EntityId.None;
        Amount = 0;
    }
    /// <summary>
    /// Public Constructor, a <code>CryptoTransfer</code> is immutable after creation.
    /// </summary>
    /// <param name="token">
    /// The account of the crypto whose tinybars have transferred.
    /// </param>
    /// <param name="address">
    /// The account receiving or sending the crypto.
    /// </param>
    /// <param name="amount">
    /// The amount of crypto transferred in tinybars.  Negative values
    /// indicate an outflow of tinybars to the <code>Address</code> positive
    /// values indicate an inflow of tinybars from the associated <code>Address</code>.
    /// </param>
    /// <param name="delegated">
    /// Indicates the parties involved in the transaction
    /// are acting as delegates through a granted allowance.
    /// </param>
    public CryptoTransfer(EntityId address, long amount, bool delegated = false)
    {
        Address = address;
        Amount = amount;
        Delegated = delegated;
    }
}