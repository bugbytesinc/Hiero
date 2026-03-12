using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for adding signatures to an
/// existing scheduled transaction.
/// </summary>
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
    /// Optional cancellation token to interrupt the transaction
    /// submission process.
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
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
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
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> SignScheduleAsync(this ConsensusClient client, SignScheduleParams signParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(signParams, configure);
    }
}
