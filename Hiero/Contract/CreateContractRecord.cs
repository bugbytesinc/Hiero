using Proto;

namespace Hiero;
/// <summary>
/// Record produced from creating a new contract.
/// </summary>
public sealed record CreateContractRecord : TransactionRecord
{
    /// <summary>
    /// The newly created contract instance address.
    /// </summary>
    /// <remarks>
    /// The value will be <code>None</code> if the create contract
    /// request was scheduled as a pending transaction.
    /// </remarks>
    public EntityId Contract { get; internal init; }
    /// <summary>
    /// The results returned from the contract create call.
    /// </summary>
    public ContractCallResult? Result { get; internal init; }
    /// <summary>
    /// Internal Constructor of the record.
    /// </summary>
    internal CreateContractRecord(Proto.TransactionRecord record) : base(record)
    {
        Contract = record.Receipt.ContractID.AsAddress();
        Result = record.ContractCreateResult is null ? null : new ContractCallResult(record.ContractCreateResult);
    }
}