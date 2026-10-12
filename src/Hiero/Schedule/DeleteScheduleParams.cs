// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for deleting an existing
/// scheduled transaction from the network.
/// </summary>
/// <example>
/// Cancel a pending schedule. Only schedules that were created with an
/// Administrator key can be deleted, and only by that admin:
/// <code source="../../../samples/DocSnippets/ScheduleSnippets.cs" region="DeleteSchedule" language="csharp"/>
/// </example>
public sealed class DeleteScheduleParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The address of the schedule entity to delete.
    /// </summary>
    public EntityId Schedule { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method
    /// required to authorize the deletion of the schedule.
    /// Typically matches the administrator endorsement assigned
    /// to the schedule when it was created.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this
    /// transaction.
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
        return new ScheduleDeleteTransactionBody
        {
            ScheduleID = new ScheduleID(Schedule)
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Schedule Delete Transaction";
}
/// <summary>
/// Extension methods for deleting scheduled transactions from the network.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DeleteScheduleExtensions
{
    /// <summary>
    /// Deletes an existing scheduled transaction from the network.
    /// Must be signed by the schedule's administrator key.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the request.
    /// </param>
    /// <param name="schedule">
    /// The address of the schedule entity to delete.
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
    /// <code source="../../../samples/DocSnippets/ScheduleSnippets.cs" region="DeleteSchedule" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> DeleteScheduleAsync(this ConsensusClient client, EntityId schedule, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new DeleteScheduleParams { Schedule = schedule }, configure);
    }
    /// <summary>
    /// Deletes an existing scheduled transaction from the network.
    /// Must be signed by the schedule's administrator key.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the request.
    /// </param>
    /// <param name="deleteParams">
    /// The details of the schedule deletion request.
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
    /// <code source="../../../samples/DocSnippets/ScheduleSnippets.cs" region="DeleteSchedule" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> DeleteScheduleAsync(this ConsensusClient client, DeleteScheduleParams deleteParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(deleteParams, configure);
    }
}
