// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;
using Com.Hedera.Hapi.Node.Addressbook;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for removing a node from the HIP-1137
/// registered-node registry.
/// </summary>
/// <remarks>
/// A registered node cannot be deleted while it is still associated
/// with a consensus node; the network rejects such a request with
/// <see cref="ResponseCode.RegisteredNodeStillAssociated"/>.
/// </remarks>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed class DeleteRegisteredNodeParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The identifier of the registered node to delete. This field is
    /// REQUIRED and the node must exist.
    /// </summary>
    public ulong RegisteredNodeId { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method required
    /// to authorize this transaction. Must include the node's admin key.
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
        return new RegisteredNodeDeleteTransactionBody
        {
            RegisteredNodeId = RegisteredNodeId
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Delete Registered Node";
}
/// <summary>
/// Extension methods for deleting registered nodes.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public static class DeleteRegisteredNodeExtensions
{
    /// <summary>
    /// Deletes a node from the HIP-1137 registered-node registry.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the request.
    /// </param>
    /// <param name="registeredNodeId">
    /// The identifier of the registered node to delete.
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
    public static Task<TransactionReceipt> DeleteRegisteredNodeAsync(this ConsensusClient client, ulong registeredNodeId, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new DeleteRegisteredNodeParams { RegisteredNodeId = registeredNodeId }, configure);
    }
}
