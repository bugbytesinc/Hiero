// SPDX-License-Identifier: Apache-2.0
using Proto;

namespace Hiero;

/// <summary>
/// The receipt returned from a successful Create Node transaction,
/// containing the newly assigned node identifier.
/// </summary>
/// <remarks>
/// <para>
/// <b>Best-effort when inferred.</b> Methods that derive a receipt's type from
/// the network's response — <see cref="TransactionReceiptExtensions.GetReceiptAsync"/>
/// and <see cref="TransactionReceiptExtensions.GetAllReceiptsAsync"/> — cannot
/// recognise the node whose identifier is <c>0</c>. The network reports
/// <c>node_id</c> as a presence-less <c>uint64</c> and protobuf omits zero from
/// the wire, so creating node 0 — the first node of a new network — is
/// indistinguishable from a transaction that created no node. Such a receipt
/// arrives as a plain <see cref="TransactionReceipt"/> instead. Established
/// networks are unaffected, since node 0 has long existed and new nodes are
/// assigned higher identifiers.
/// </para>
/// <para>
/// No value is lost to this: the substitution happens only when the identifier
/// is zero. Test rather than cast:
/// </para>
/// <code>
/// ulong nodeId = receipt is ConsensusNodeReceipt node ? node.NodeId : 0;
/// </code>
/// <para>
/// Receipts returned directly by the operation that produced them — such as
/// <c>CreateNodeAsync</c> — are typed explicitly and are always correct.
/// </para>
/// </remarks>
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
