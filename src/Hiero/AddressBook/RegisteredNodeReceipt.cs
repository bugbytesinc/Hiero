// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;
using Proto;

namespace Hiero;

/// <summary>
/// The receipt returned from a successful Create Registered Node
/// transaction, containing the newly assigned registered-node
/// identifier.
/// </summary>
/// <remarks>
/// <para>
/// <b>Best-effort when inferred.</b> Methods that derive a receipt's type from
/// the network's response — <see cref="TransactionReceiptExtensions.GetReceiptAsync"/>
/// and <see cref="TransactionReceiptExtensions.GetAllReceiptsAsync"/> — cannot
/// recognise a registered node whose identifier is <c>0</c>. The network
/// reports <c>registered_node_id</c> as a presence-less <c>uint64</c> and
/// protobuf omits zero from the wire, so such a receipt is indistinguishable
/// from a transaction that registered no node and arrives as a plain
/// <see cref="TransactionReceipt"/> instead.
/// </para>
/// <para>
/// No value is lost to this: the substitution happens only when the identifier
/// is zero. Test rather than cast:
/// </para>
/// <code>
/// ulong id = receipt is RegisteredNodeReceipt node ? node.RegisteredNodeId : 0;
/// </code>
/// <para>
/// Receipts returned directly by
/// <see cref="CreateRegisteredNodeExtensions.CreateRegisteredNodeAsync"/> are
/// typed explicitly and are always correct.
/// </para>
/// </remarks>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed record RegisteredNodeReceipt : TransactionReceipt
{
    /// <summary>
    /// The identifier assigned to the newly registered node. This
    /// value is unique within the network and is used by discovery
    /// (<c>GetRegisteredNodesAsync</c>) and by later update and delete
    /// transactions.
    /// </summary>
    public ulong RegisteredNodeId { get; internal init; }
    /// <summary>
    /// Internal Constructor
    /// </summary>
    internal RegisteredNodeReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt) : base(transactionId, receipt)
    {
        RegisteredNodeId = receipt.RegisteredNodeId;
    }
}
