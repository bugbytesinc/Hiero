using Com.Hedera.Hapi.Node.Addressbook;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for removing a consensus node from the network address book.
/// </summary>
/// <remarks>
/// This is a privileged transaction requiring Hedera governing council authorization.
/// The node enters a "pending delete" state immediately, but is fully removed from the
/// network at the next upgrade (freeze with PREPARE_UPGRADE). Node identifiers are
/// never reused after deletion.
/// </remarks>
public sealed class RemoveConsensusNodeParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The identifier of the node to delete. This field is REQUIRED.
    /// The identified node must exist and must not already be deleted.
    /// </summary>
    public ulong NodeId { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method required to
    /// authorize this transaction. Must include the node's admin key or one
    /// of the governing council keys.
    /// </summary>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional cancellation token to interrupt the submission process.
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
/// Extension methods for removing consensus nodes from the network address book.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RemoveConsensusNodeExtensions
{
    /// <summary>
    /// Removes a consensus node from the network address book.
    /// </summary>
    /// <remarks>
    /// This is a privileged transaction requiring Hedera governing council authorization.
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
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> RemoveConsensusNodeAsync(this ConsensusClient client, ulong nodeId, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new RemoveConsensusNodeParams { NodeId = nodeId }, configure);
    }
    /// <summary>
    /// Removes a consensus node from the network address book.
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
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> RemoveConsensusNodeAsync(this ConsensusClient client, RemoveConsensusNodeParams deleteParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(deleteParams, configure);
    }
}
