namespace Hiero;
/// <summary>
/// Represents a crypto transfer (Address, Amount)
/// </summary>
public sealed record CryptoTransfer
{
    /// <summary>
    /// The account receiving or sending the crypto.
    /// </summary>
    public EntityId Address { get; private init; }
    /// <summary>
    /// The amount of crypto transferred in tinybars.  Negative values
    /// indicate an outflow of tinybars from the <code>Address</code>.  Positive
    /// values indicate an inflow of tinybars to the associated <code>Address</code>.
    /// </summary>
    public long Amount { get; private init; }
    /// <summary>
    /// Indicates the parties involved in the transaction
    /// are acting as delegates through a granted allowance.
    /// </summary>
    public bool Delegated { get; private init; }
    /// <summary>
    /// Optional allowance hook call for this transfer. The hook's
    /// <see cref="HookCall.CallMode"/> determines whether it is
    /// invoked before the transfer only, or both before and after.
    /// </summary>
    public HookCall? AllowanceHook { get; private init; }
    /// <summary>
    /// Internal Constructor representing the "None"
    /// version of a transfer.
    /// </summary>
    private CryptoTransfer()
    {
        Address = Hiero.EntityId.None;
        Amount = 0;
    }
    /// <summary>
    /// Public Constructor, a <code>CryptoTransfer</code> is immutable after creation.
    /// </summary>
    /// <param name="address">
    /// The account receiving or sending the crypto.
    /// </param>
    /// <param name="amount">
    /// The amount of crypto transferred in tinybars.  Negative values
    /// indicate an outflow of tinybars from the <code>Address</code>.  Positive
    /// values indicate an inflow of tinybars to the associated <code>Address</code>.
    /// </param>
    /// <param name="delegated">
    /// Indicates the parties involved in the transaction
    /// are acting as delegates through a granted allowance.
    /// </param>
    /// <param name="allowanceHook">
    /// Optional allowance hook call for this transfer.
    /// </param>
    public CryptoTransfer(EntityId address, long amount, bool delegated = false, HookCall? allowanceHook = null)
    {
        Address = address;
        Amount = amount;
        Delegated = delegated;
        AllowanceHook = allowanceHook;
    }
}
