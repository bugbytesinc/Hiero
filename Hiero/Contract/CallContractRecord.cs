namespace Hiero;
/// <summary>
/// Record produced from creating a calling a contract.
/// </summary>
public sealed record CallContractRecord : TransactionRecord
{
    /// <summary>
    /// The results returned from the contract call.
    /// </summary>
    public ContractCallResult? Result { get; internal init; }
    /// <summary>
    /// Internal Constructor of the record.
    /// </summary>
    internal CallContractRecord(Proto.TransactionRecord record) : base(record)
    {
        Result = record?.ContractCallResult == null ? null : new ContractCallResult(record.ContractCallResult);
    }
}