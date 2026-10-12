// SPDX-License-Identifier: Apache-2.0
using Com.Hedera.Hapi.Node.Addressbook;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for deleting a consensus node from the network address book.
/// </summary>
/// <example>
/// Retire a node by NodeId. The node is removed from the network at the next
/// freeze with PREPARE_UPGRADE:
/// <code source="../../../samples/DocSnippets/GovernanceSnippets.cs" region="DeleteConsensusNode" language="csharp"/>
/// </example>
/// <remarks>
/// This is a privileged transaction: it must be signed by the node's admin key or by
/// the treasury (0.0.2), system admin (0.0.50) or address book admin (0.0.55) key.
/// The node enters a "pending delete" state immediately, but is fully removed from the
/// network at the next upgrade (freeze with PREPARE_UPGRADE). Node identifiers are
/// never reused after deletion.
/// </remarks>
public sealed class DeleteConsensusNodeParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The identifier of the node to delete. This field is REQUIRED.
    /// The identified node must exist and must not already be deleted.
    /// </summary>
    public ulong NodeId { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method required to
    /// authorize this transaction. Must include the node's admin key or the
    /// treasury (0.0.2), system admin (0.0.50) or address book admin (0.0.55) key.
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
        return new NodeDeleteTransactionBody
        {
            NodeId = NodeId
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Delete Node";
}
/// <summary>
/// Extension methods for deleting consensus nodes from the network address book.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DeleteConsensusNodeExtensions
{
    /// <summary>
    /// Deletes a consensus node from the network address book.
    /// </summary>
    /// <remarks>
    /// This is a privileged transaction: it must be signed by the node's admin key or by
    /// the treasury (0.0.2), system admin (0.0.50) or address book admin (0.0.55) key.
    /// The node is fully removed from the network at the next upgrade.
    /// </remarks>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the request.
    /// </param>
    /// <param name="nodeId">
    /// The identifier of the node to delete.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating a successful operation.
    /// </returns>
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/GovernanceSnippets.cs" region="DeleteConsensusNode" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> DeleteConsensusNodeAsync(this ConsensusClient client, ulong nodeId, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new DeleteConsensusNodeParams { NodeId = nodeId }, configure);
    }
    /// <summary>
    /// Deletes a consensus node from the network address book.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the request.
    /// </param>
    /// <param name="deleteParams">
    /// The deletion parameters containing the node identifier to delete.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating a successful operation.
    /// </returns>
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/GovernanceSnippets.cs" region="DeleteConsensusNode" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> DeleteConsensusNodeAsync(this ConsensusClient client, DeleteConsensusNodeParams deleteParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(deleteParams, configure);
    }
}
