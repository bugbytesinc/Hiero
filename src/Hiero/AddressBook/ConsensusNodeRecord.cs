// SPDX-License-Identifier: Apache-2.0
namespace Hiero;

/// <summary>
/// The consensus record from a successful Create Node transaction,
/// containing the newly assigned node identifier.
/// </summary>
/// <remarks>
/// <b>Best-effort when inferred</b>, exactly as for
/// <see cref="ConsensusNodeReceipt"/>: the node whose identifier is 0 cannot be recognised as a node creation. Records obtained from
/// <see cref="TransactionRecordExtensions.GetTransactionRecordAsync"/>,
/// <see cref="TransactionRecordExtensions.GetAllTransactionRecordsAsync"/> or
/// <see cref="TransactionRecordExtensions.GetAccountRecordsAsync"/> therefore
/// arrive as a plain <see cref="TransactionRecord"/> in that case. No value is
/// lost — the substitution happens only when the value is zero. Test rather
/// than cast:
/// <code>
/// ulong nodeId = record is ConsensusNodeRecord node ? node.NodeId : 0;
/// </code>
/// </remarks>
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