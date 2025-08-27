using Proto;

namespace Hiero;
/// <summary>
/// A transaction record containing information concerning the newly created account.
/// </summary>
public sealed record CreateAccountReceipt : TransactionReceipt
{
    /// <summary>
    /// The address of the newly created account.
    /// </summary>
    /// <remarks>
    /// The value will be <code>None</code> if the create acocunt
    /// method was scheduled as a pending transaction.
    /// </remarks>
    public EntityId Address { get; internal init; }
    /// <summary>
    /// Internal Constructor of the receipt.
    /// </summary>
    internal CreateAccountReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt) : base(transactionId, receipt)
    {
        Address = receipt.AccountID.AsAddress();
    }
}