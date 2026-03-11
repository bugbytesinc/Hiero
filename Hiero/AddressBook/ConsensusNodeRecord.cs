namespace Hiero;

/// <summary>
/// The consensus record from a successful Create Node transaction,
/// containing the newly assigned node identifier.
/// </summary>
public sealed record ConsensusNodeRecord : TransactionRecord
{
    /// <summary>
    /// The identifier assigned to the newly created consensus node.
    /// This value is unique within the network and will not be reused
    /// even if the node is later deleted.
    /// </summary>
    public ulong NodeId { get; internal init; }
    /// <summary>
    /// Internal Constructor of the record.
    /// </summary>
    internal ConsensusNodeRecord(Proto.TransactionRecord record) : base(record)
    {
        NodeId = record.Receipt.NodeId;
    }
}