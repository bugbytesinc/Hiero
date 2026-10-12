// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for Deleting a Consensus Topic.
/// </summary>
/// <example>
/// Permanently delete a topic. Requires the Administrator key; immutable
/// topics cannot be deleted:
/// <code source="../../../samples/DocSnippets/ConsensusSnippets.cs" region="DeleteTopic" language="csharp"/>
/// </example>
public sealed class DeleteTopicParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The Id of the topic.
    /// </summary>
    public EntityId Topic { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to authorize the deletion.  Typically matches the
    /// Endorsement assigned to the admin for the topic if it is not already
    /// set as the payer for the transaction.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to delete this topic.
    /// </remarks>
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
        return new ConsensusDeleteTopicTransactionBody()
        {
            TopicID = new TopicID(Topic)
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Delete Topic";
}
/// <summary>
/// Extension methods for deleting consensus topics.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DeleteTopicExtensions
{
    /// <summary>
    /// Deletes a topic instance from the network. Must be signed 
    /// by the admin key.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the delete.
    /// </param>
    /// <param name="topic">
    /// The Topic instance that will be deleted.
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
    /// <exception cref="TransactionException">If the network rejected the delete request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/ConsensusSnippets.cs" region="DeleteTopic" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> DeleteTopicAsync(this ConsensusClient client, EntityId topic, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new DeleteTopicParams { Topic = topic }, configure);
    }
    /// <summary>
    /// Deletes a topic instance from the network. Must be signed 
    /// by the admin key.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the delete.
    /// </param>
    /// <param name="deleteParams">
    /// The parameters for deleting the topic.
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
    /// <exception cref="TransactionException">If the network rejected the delete request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/ConsensusSnippets.cs" region="DeleteTopic" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> DeleteTopicAsync(this ConsensusClient client, DeleteTopicParams deleteParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(deleteParams, configure);
    }
}