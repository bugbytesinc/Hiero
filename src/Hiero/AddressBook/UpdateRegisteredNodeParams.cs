// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;
using Com.Hedera.Hapi.Node.Addressbook;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for updating a node in the HIP-1137
/// registered-node registry. Only the supplied fields are changed.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed class UpdateRegisteredNodeParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The identifier of the registered node to update. This field is
    /// REQUIRED and the node must exist.
    /// </summary>
    public ulong RegisteredNodeId { get; set; }
    /// <summary>
    /// A replacement administrative key. This field is OPTIONAL; when
    /// omitted the admin key is unchanged. Both the existing and the
    /// new admin key must sign when this is supplied.
    /// </summary>
    public Endorsement? AdminKey { get; set; }
    /// <summary>
    /// A replacement description (≤ 100 bytes UTF-8). This field is
    /// OPTIONAL; when omitted the description is unchanged.
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// A replacement set of service endpoints. This field is OPTIONAL;
    /// when omitted (null) the endpoints are unchanged. A non-empty list
    /// replaces the entire endpoint list. An empty list is encoded the
    /// same as null, so it also leaves the endpoints unchanged; the
    /// endpoint list cannot be cleared.
    /// </summary>
    public IReadOnlyList<RegisteredNodeEndpoint>? Endpoints { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method required
    /// to authorize this transaction. Must include the current admin
    /// key (and the new one if <see cref="AdminKey"/> is supplied).
    /// </summary>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional cancellation token.  Cancelling stops the client waiting;
    /// it cannot recall a transaction already sent.  Cancelled after the
    /// send, the outcome is unknown: settle it by its transaction id on a
    /// mirror node rather than resubmitting it.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        var body = new RegisteredNodeUpdateTransactionBody
        {
            RegisteredNodeId = RegisteredNodeId
        };
        if (AdminKey is not null)
        {
            body.AdminKey = new Key(AdminKey);
        }
        if (Description is not null)
        {
            body.Description = Description;
        }
        if (Endpoints is not null)
        {
            foreach (var endpoint in Endpoints)
            {
                body.ServiceEndpoint.Add(endpoint.ToProtobuf());
            }
        }
        return body;
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Update Registered Node";
}
/// <summary>
/// Extension methods for updating registered nodes.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public static class UpdateRegisteredNodeExtensions
{
    /// <summary>
    /// Updates a node in the HIP-1137 registered-node registry.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the request.
    /// </param>
    /// <param name="updateParams">
    /// The update parameters; only supplied fields are changed.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating a successful operation.
    /// </returns>
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> UpdateRegisteredNodeAsync(this ConsensusClient client, UpdateRegisteredNodeParams updateParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(updateParams, configure);
    }
}
