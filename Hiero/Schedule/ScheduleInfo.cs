using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Numerics;

namespace Hiero;
/// <summary>
/// Information regarding a scheduled transaction held
/// by the network that is either awaiting additional
/// signatures or is awaiting its appointed execution
/// consensus time.
/// </summary>
public sealed record ScheduleInfo
{
    /// <summary>
    /// The identifier of scheduled transaction
    /// record held by the network.
    /// </summary>
    public EntityId Schedule { get; internal init; } = default!;
    /// <summary>
    /// The ID of the scheduled transaction, 
    /// should it be executed.
    /// </summary>
    public TransactionId TransactionId { get; internal init; } = default!;
    /// <summary>
    /// The Address that paid for the scheduling 
    /// of the transaction.
    /// </summary>
    public EntityId Creator { get; private init; }
    /// <summary>
    /// The account paying for the execution of the
    /// transaction should it be executed.
    /// </summary>
    public EntityId Payer { get; private init; }
    /// <summary>
    /// A list of keys having signed the schedule transaction, when
    /// all necessary keyholders have signed, the network will attempt
    /// to execute the transaction immediately if it was configured for
    /// immediate execution, or wait until the configured consensus 
    /// timestamp if delayed execution flag was set.
    /// </summary>
    public Endorsement[] Endorsements { get; private init; }
    /// <summary>
    /// The endorsement key that can cancel this schedule transaction.
    /// It may be null, in which case it can not be canceled and will
    /// exist until sufficiently signed and executed or expired by the 
    /// network.
    /// </summary>
    public Endorsement? Administrator { get; private init; }
    /// <summary>
    /// Optional memo attached to the scheduling 
    /// of the transaction.
    /// </summary>
    public string? Memo { get; private init; }
    /// <summary>
    /// The time at which the schedule transaction will expire
    /// and be removed from the network if not signed by 
    /// all necessary parties and executed.
    /// </summary>
    public ConsensusTimeStamp Expiration { get; private init; }
    /// <summary>
    /// If not null, the consensus time at which this schedule
    /// transaction was completed and executed by the network.
    /// </summary>
    public ConsensusTimeStamp? Executed { get; private init; }
    /// <summary>
    /// If not null, the consensus time when this schedule
    /// transaction was canceled using the administrative key.
    /// </summary>
    public ConsensusTimeStamp? Deleted { get; private init; }
    /// <summary>
    /// The body bytes of the schedule transaction, serialized
    /// into the binary protobuf message format 
    /// of the SchedulableTransactionBody message.
    /// </summary>
    public ReadOnlyMemory<byte> ScheduledTransactionBodyBytes { get; private init; }
    /// <summary>
    /// Identification of the Ledger (Network) this 
    /// schedule transaction information was retrieved from.
    /// </summary>
    public ulong Ledger { get; private init; }
    /// <summary>
    /// If set to <code>true</code>, the network will delay the
    /// attempt to execute the schedule transaction until the
    /// expiration time, even if it receives sufficient signatures
    /// satisfying the signing requirements prior to the deadline.
    /// </summary>
    public bool DelayExecution { get; init; }
    /// <summary>
    /// Internal Constructor from Raw Results
    /// </summary>
    internal ScheduleInfo(Response response)
    {
        var info = response.ScheduleGetInfo.ScheduleInfo;
        Schedule = info.ScheduleID.ToAddress();
        TransactionId = info.ScheduledTransactionID.AsTxId();
        Creator = info.CreatorAccountID.AsAddress();
        Payer = info.PayerAccountID.AsAddress();
        Endorsements = info.Signers.ToEndorsements();
        Administrator = info.AdminKey?.ToEndorsement();
        Memo = info.Memo;
        Expiration = info.ExpirationTime.ToConsensusTimeStamp();
        Executed = info.ExecutionTime?.ToConsensusTimeStamp();
        Deleted = info.DeletionTime?.ToConsensusTimeStamp();
        ScheduledTransactionBodyBytes = info.ScheduledTransactionBody.ToByteArray();
        Ledger = (ulong)new BigInteger(info.LedgerId.Span, true, true);
        DelayExecution = info.WaitForExpiry;
    }
}
/// <summary>
/// Extension methods for querying scheduled transaction information from the network.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ScheduleInfoExtensions
{
    /// <summary>
    /// Retrieves detailed information regarding a scheduled transaction by ID.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="schedule">
    /// The address of the schedule entity to retrieve.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A detailed description of the schedule transaction,
    /// including the serialized schedule transaction body itself.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission.</exception>
    public static async Task<ScheduleInfo> GetScheduleInfoAsync(this ConsensusClient client, EntityId schedule, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        return new ScheduleInfo(await Engine.QueryAsync(client, new ScheduleGetInfoQuery { ScheduleID = new ScheduleID(schedule) }, cancellationToken, configure).ConfigureAwait(false));
    }
}
