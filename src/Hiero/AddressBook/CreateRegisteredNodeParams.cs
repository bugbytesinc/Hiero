// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;
using Com.Hedera.Hapi.Node.Addressbook;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for registering a new node (block node,
/// etc.) in the HIP-1137 on-chain registered-node registry.
/// </summary>
/// <remarks>
/// On mainnet, creating a registered node is currently restricted to
/// privileged payer accounts. The receipt carries the network-assigned
/// <see cref="RegisteredNodeReceipt.RegisteredNodeId"/> that later
/// update and delete transactions require, and that discovery
/// (<c>GetRegisteredNodesAsync</c>) surfaces.
/// </remarks>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed class CreateRegisteredNodeParams : TransactionParams<RegisteredNodeReceipt>, INetworkParams<RegisteredNodeReceipt>
{
    /// <summary>
    /// The administrative key authorized to update or delete this
    /// registration. This key must sign this transaction. This field
    /// is REQUIRED.
    /// </summary>
    public Endorsement AdminKey { get; set; } = default!;
    /// <summary>
    /// A short description of the node. Must not exceed 100 bytes when
    /// encoded as UTF-8. This field is OPTIONAL.
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// The service endpoints this node publishes. Must contain between
    /// 1 and 50 entries. This field is REQUIRED.
    /// </summary>
    public IReadOnlyList<RegisteredNodeEndpoint> Endpoints { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method required
    /// to authorize this transaction. Must include the key matching
    /// <see cref="AdminKey"/> and, where required, a privileged payer.
    /// </summary>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional cancellation token.  Cancelling stops the client waiting;
    /// it cannot recall a transaction already sent.  Cancelled after the
    /// send, the outcome is unknown: settle it by its transaction id on a
    /// mirror node rather than resubmitting it.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<RegisteredNodeReceipt>.CreateNetworkTransaction()
    {
        if (AdminKey is null)
        {
            throw new ArgumentNullException(nameof(AdminKey), "The registered node admin key is required.");
        }
        if (Endpoints is null || Endpoints.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Endpoints), "At least one service endpoint is required.");
        }
        var body = new RegisteredNodeCreateTransactionBody
        {
            AdminKey = new Key(AdminKey)
        };
        if (!string.IsNullOrEmpty(Description))
        {
            body.Description = Description;
        }
        foreach (var endpoint in Endpoints)
        {
            body.ServiceEndpoint.Add(endpoint.ToProtobuf());
        }
        return body;
    }
    RegisteredNodeReceipt INetworkParams<RegisteredNodeReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new RegisteredNodeReceipt(transactionId, receipt);
    }
    string INetworkParams<RegisteredNodeReceipt>.OperationDescription => "Create Registered Node";
}
/// <summary>
/// Extension methods for registering nodes in the HIP-1137 registry.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public static class CreateRegisteredNodeExtensions
{
    /// <summary>
    /// Registers a new node in the HIP-1137 on-chain registered-node
    /// registry.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the request.
    /// </param>
    /// <param name="createParams">
    /// The registration parameters: admin key, optional description,
    /// and service endpoints.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// </param>
    /// <returns>
    /// A receipt carrying the network-assigned registered-node id.
    /// </returns>
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<RegisteredNodeReceipt> CreateRegisteredNodeAsync(this ConsensusClient client, CreateRegisteredNodeParams createParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(createParams, configure);
    }
}
