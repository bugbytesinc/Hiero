using Proto;

namespace Hiero;
/// <summary>
/// Receipt produced from creating a new token.
/// </summary>
public sealed record CreateTokenReceipt : TransactionReceipt
{
    /// <summary>
    /// The newly created token address.
    /// </summary>
    /// <remarks>
    /// The value will be <code>None</code> if the create token
    /// method was scheduled as a pending transaction.
    /// </remarks>
    public EntityId Token { get; internal init; }
    /// <summary>
    /// Internal Constructor of the receipt.
    /// </summary>
    internal CreateTokenReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt) : base(transactionId, receipt)
    {
        Token = receipt.TokenID.AsAddress();
    }
}