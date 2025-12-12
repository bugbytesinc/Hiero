namespace Hiero;
/// <summary>
/// Record produced from an Ethereum transaction call.
/// </summary>
public sealed record EvmTransactionRecord : TransactionRecord
{
    /// <summary>
    /// The keccak256 hash of the ethereumData.
    /// </summary>
    public ReadOnlyMemory<byte> EthereumHash { get; internal init; }
    /// <summary>
    /// Internal Constructor of the record.
    /// </summary>
    internal EvmTransactionRecord(Proto.TransactionRecord record) : base(record)
    {
        EthereumHash = record.EthereumHash.Memory;
    }
}