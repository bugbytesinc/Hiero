// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for adding signatures to an
/// existing scheduled transaction.
/// </summary>
/// <example>
/// Add a co-signer's signature to a pending schedule when that key is not
/// already in the client's context:
/// <code source="../../../samples/DocSnippets/ScheduleSnippets.cs" region="SignScheduleCoSigner" language="csharp"/>
/// </example>
public sealed class SignScheduleParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The address of the schedule entity to sign.
    /// </summary>
    public EntityId Schedule { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method
    /// required to add signatures to the scheduled transaction.
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
        return new ScheduleSignTransactionBody
        {
            ScheduleID = new ScheduleID(Schedule)
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Schedule Sign Transaction";
}
/// <summary>
/// Extension methods for adding signatures to scheduled transactions.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SignScheduleExtensions
{
    /// <summary>
    /// Adds signatures to an existing scheduled transaction.
    /// If the additional signatures satisfy all remaining
    /// signature requirements, the scheduled transaction
    /// may execute immediately (unless delayed).
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the request.
    /// </param>
    /// <param name="schedule">
    /// The address of the schedule entity to sign.
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
    /// <code source="../../../samples/DocSnippets/ScheduleSnippets.cs" region="SignSchedule" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> SignScheduleAsync(this ConsensusClient client, EntityId schedule, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new SignScheduleParams { Schedule = schedule }, configure);
    }
    /// <summary>
    /// Adds signatures to an existing scheduled transaction.
    /// If the additional signatures satisfy all remaining
    /// signature requirements, the scheduled transaction
    /// may execute immediately (unless delayed).
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the request.
    /// </param>
    /// <param name="signParams">
    /// The details of the schedule signing request.
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
    /// <code source="../../../samples/DocSnippets/ScheduleSnippets.cs" region="SignScheduleCoSigner" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> SignScheduleAsync(this ConsensusClient client, SignScheduleParams signParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(signParams, configure);
    }
}
