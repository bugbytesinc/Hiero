using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Parameters for creating a scheduled transaction on the network.
/// The inner transaction will be held by the network and executed
/// when all required signatures are collected, or optionally
/// delayed until the specified expiration time.
/// </summary>
public sealed class ScheduleParams
{
    /// <summary>
    /// The transaction to schedule for future execution.
    /// This can be any supported transaction type that implements
    /// the schedulable transaction interface.
    /// </summary>
    public TransactionParams Transaction { get; set; } = default!;
    /// <summary>
    /// Optional administrator endorsement (key) for the schedule.
    /// When set, this key must sign any request to delete the
    /// schedule before it executes or expires.
    /// </summary>
    public Endorsement? Administrator { get; set; }
    /// <summary>
    /// Optional memo describing the scheduled transaction.
    /// Limited to 100 bytes by the network.
    /// </summary>
    public string? Memo { get; set; }
    /// <summary>
    /// Optional account that will pay for the execution of
    /// the scheduled transaction when it triggers. If not set,
    /// the payer of the schedule create transaction will be used.
    /// </summary>
    public EntityId? Payer { get; set; }
    /// <summary>
    /// Optional expiration time for the schedule. If the scheduled
    /// transaction has not been executed by this time, it will be
    /// removed from the network.
    /// </summary>
    public ConsensusTimeStamp? Expiration { get; set; }
    /// <summary>
    /// When set to <code>true</code>, the scheduled transaction will
    /// not execute until the expiration time, even if all required
    /// signatures are collected before then.
    /// </summary>
    public bool DelayExecution { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method
    /// required to authorize the schedule creation.
    /// </summary>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional cancellation token for this schedule creation request.
    /// If set, this token takes precedence over any cancellation token
    /// defined by the inner scheduled transaction parameters.
    /// </summary>    
    public CancellationToken? CancellationToken { get; set; }
}
internal sealed class ScheduleParamsOrchestrator : TransactionParams<ScheduleReceipt>, INetworkParams<ScheduleReceipt>
{
    private readonly Signatory? _signatory;
    private readonly CancellationToken? _cancellationToken;
    private readonly INetworkParams<TransactionReceipt> _innerNetworkParams;
    private readonly INetworkTransaction _networkTransaction;

    public Signatory? Signatory => _signatory;
    public CancellationToken? CancellationToken => _cancellationToken;
    INetworkTransaction INetworkParams<ScheduleReceipt>.CreateNetworkTransaction() => _networkTransaction;
    private ScheduleParamsOrchestrator(Signatory? signatory, CancellationToken? cancellationToken, INetworkParams<TransactionReceipt> innerNetworkParams, INetworkTransaction networkTransaction)
    {
        _signatory = signatory;
        _cancellationToken = cancellationToken;
        _innerNetworkParams = innerNetworkParams;
        _networkTransaction = networkTransaction;
    }
    internal static async Task<TransactionParams<ScheduleReceipt>> CreateAsync(ScheduleParams scheduleParams, ConsensusClient client)
    {
        if (scheduleParams.Transaction is null)
        {
            throw new ArgumentNullException(nameof(Transaction), "The Transaction to schedule is missing. Please provide a valid transaction.");
        }
        if (scheduleParams.Transaction is not INetworkParams<TransactionReceipt> innerNetworkParams)
        {
            throw new ArgumentException("The provided transaction does not support scheduling.", nameof(Transaction));
        }
        await using var context = client.BuildChildContext(null);
        var innerNetworkTransaction = innerNetworkParams.CreateNetworkTransaction();
        var schedulableBody = innerNetworkTransaction.CreateSchedulableTransactionBody();
        schedulableBody.TransactionFee = (ulong)context.FeeLimit;
        var scheduleCreateTransactionBody = new ScheduleCreateTransactionBody
        {
            ScheduledTransactionBody = schedulableBody,
            AdminKey = scheduleParams.Administrator is null ? null : new Key(scheduleParams.Administrator),
            PayerAccountID = scheduleParams.Payer is null ? null : new AccountID(scheduleParams.Payer),
            ExpirationTime = scheduleParams.Expiration is null ? null : new Proto.Timestamp(scheduleParams.Expiration.Value),
            WaitForExpiry = scheduleParams.DelayExecution,
            Memo = scheduleParams.Memo ?? ""
        };
        var signatory = innerNetworkParams.Signatory is not null ? (scheduleParams.Signatory is not null ? new Signatory(innerNetworkParams.Signatory, scheduleParams.Signatory) : innerNetworkParams.Signatory) : scheduleParams.Signatory;
        var cancellationToken = scheduleParams.CancellationToken ?? innerNetworkParams.CancellationToken;
        return new ScheduleParamsOrchestrator(signatory, cancellationToken, innerNetworkParams, scheduleCreateTransactionBody);
    }
    ScheduleReceipt INetworkParams<ScheduleReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new ScheduleReceipt(transactionId, receipt);
    }
    string INetworkParams<ScheduleReceipt>.OperationDescription => $"Scheduling {_innerNetworkParams.OperationDescription ?? "Transaction"}";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ScheduleExtensions
{
    /// <summary>
    /// Creates a new scheduled transaction on the network.
    /// The inner transaction will be held by the network and
    /// executed when all required signatures are collected.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the request.
    /// </param>
    /// <param name="transactionParams">
    /// The transaction to schedule for future execution.
    /// This can be any supported transaction type that implements
    /// the schedulable transaction interface.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A receipt containing the schedule ID and the scheduled
    /// transaction ID that will be used when executed.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<ScheduleReceipt> ScheduleAsync(this ConsensusClient client, TransactionParams transactionParams, Action<IConsensusContext>? configure = null)
    {
        return client.ScheduleAsync(new ScheduleParams { Transaction = transactionParams }, configure);
    }
    /// <summary>
    /// Creates a new scheduled transaction on the network.
    /// The inner transaction will be held by the network and
    /// executed when all required signatures are collected.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the request.
    /// </param>
    /// <param name="scheduleParams">
    /// The details of the scheduled transaction to create.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A receipt containing the schedule ID and the scheduled
    /// transaction ID that will be used when executed.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async Task<ScheduleReceipt> ScheduleAsync(this ConsensusClient client, ScheduleParams scheduleParams, Action<IConsensusContext>? configure = null)
    {
        await using var configuredClient = client.Clone(configure);
        var transactionParams = await ScheduleParamsOrchestrator.CreateAsync(scheduleParams, configuredClient).ConfigureAwait(false);
        return await configuredClient.ExecuteAsync(transactionParams, null).ConfigureAwait(false);
    }
    /// <summary>
    /// Creates, signs, submits a scheduling transaction and waits for a response from 
    /// the target consensus node.  Returning the precheck response code.
    /// A <see cref="PrecheckException"/> may be thrown under certain invalid
    /// input scenarios.
    /// </summary>
    /// <remarks>
    /// This method will wait for the target consensus node to respond with 
    /// a code other than <see cref="ResponseCode.Busy"/> or 
    /// <see cref="ResponseCode.InvalidTransactionStart"/> if applicable, 
    /// until such time as the retry count is exhausted, in which case it 
    /// is possible to receive a <see cref="ResponseCode.Busy"/> response.
    /// </remarks>
    /// <typeparam name="T">
    /// The type of <see cref="TransactionReceipt"/> returned by the request.
    /// </typeparam>
    /// <param name="scheduleParams">
    /// Scheduling transaction input parameters.
    /// </param>
    /// <param name="configure">
    /// Optional callback to configure the calling context immediately 
    /// before assembling the transaction for submission.
    /// </param>
    /// <returns>
    /// The precheck <see cref="ResponseCode"/> returned from the request
    /// after waiting for submission retries if applicable.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async static Task<ResponseCode> SubmitAsync(this ConsensusClient client, ScheduleParams scheduleParams, Action<IConsensusContext>? configure = null)
    {
        await using var configuredClient = client.Clone(configure);
        var transactionParams = await ScheduleParamsOrchestrator.CreateAsync(scheduleParams, configuredClient).ConfigureAwait(false);
        return await configuredClient.SubmitAsync(transactionParams, null).ConfigureAwait(false);
    }
}
