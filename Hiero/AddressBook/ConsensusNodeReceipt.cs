using Proto;

namespace Hiero;

/// <summary>
/// The receipt returned from a successful Create Node transaction,
/// containing the newly assigned node identifier.
/// </summary>
public sealed record ConsensusNodeReceipt : TransactionReceipt
{
    /// <summary>
    /// The identifier assigned to the newly created consensus node.
    /// This value is unique within the network and will not be reused
    /// even if the node is later deleted.
    /// </summary>
    public ulong NodeId { get; internal init; }
    /// <summary>
    /// Internal Constructor
    /// </summary>
    internal ConsensusNodeReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt) : base(transactionId, receipt)
    {
        NodeId = receipt.NodeId;
    }
}
