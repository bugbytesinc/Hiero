using Proto;

namespace Hiero;
/// <summary>
/// Receipt produced from creating a new contract.
/// </summary>
public sealed record CreateContractReceipt : TransactionReceipt
{
    /// <summary>
    /// The newly created or associated contract instance address.
    /// </summary>
    /// <remarks>
    /// The value will be <code>None</code> if the create contract
    /// method was scheduled as a pending transaction.
    /// </remarks>
    public EntityId Contract { get; internal init; }
    /// <summary>
    /// Internal Constructor of the receipt.
    /// </summary>
    internal CreateContractReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt) : base(transactionId, receipt)
    {
        Contract = receipt.ContractID.AsAddress();
    }
}