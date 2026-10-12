// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;
namespace Hiero;

/// <summary>
/// The consensus record from a successful Create Registered Node
/// transaction, containing the newly assigned registered-node
/// identifier.
/// </summary>
/// <remarks>
/// <b>Best-effort when inferred</b>, exactly as for
/// <see cref="RegisteredNodeReceipt"/>: a registered node whose identifier is 0 cannot be recognised as a registration. Records obtained from
/// <see cref="TransactionRecordExtensions.GetTransactionRecordAsync"/>,
/// <see cref="TransactionRecordExtensions.GetAllTransactionRecordsAsync"/> or
/// <see cref="TransactionRecordExtensions.GetAccountRecordsAsync"/> therefore
/// arrive as a plain <see cref="TransactionRecord"/> in that case. No value is
/// lost — the substitution happens only when the value is zero. Test rather
/// than cast:
/// <code>
/// ulong id = record is RegisteredNodeRecord node ? node.RegisteredNodeId : 0;
/// </code>
/// </remarks>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed record RegisteredNodeRecord : TransactionRecord
{
    /// <summary>
    /// The identifier assigned to the newly registered node. This
    /// value is unique within the network and is used by discovery
    /// (<see cref="Mirror.RegisteredNodeDataExtensions.GetRegisteredBlockNodesAsync(MirrorRestClient, CancellationToken, Mirror.Filters.IMirrorQueryParameter[])"/>)
    /// and by later update and delete transactions.
    /// </summary>
    public ulong RegisteredNodeId { get; internal init; }
    /// <summary>
    /// Internal Constructor of the record.
    /// </summary>
    internal RegisteredNodeRecord(Proto.TransactionRecord record) : base(record)
    {
        RegisteredNodeId = record.Receipt.RegisteredNodeId;
    }
}
