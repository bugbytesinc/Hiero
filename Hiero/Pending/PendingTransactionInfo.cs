using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Numerics;

namespace Hiero;
/// <summary>
/// The information returned from the GetPendingTransactionInfo
/// ConsensusClient  method call.  It represents the details concerning a 
/// pending (scheduled, not yet executed) transaction held by the
/// network awaiting signatures.
/// </summary>
public sealed record PendingTransactionInfo : PendingTransaction
{
    /// <summary>
    /// The Address that paid for the scheduling of the 
    /// pending transaction.
    /// </summary>
    public EntityId Creator { get; private init; }
    /// <summary>
    /// The account paying for the execution of the
    /// pending transaction.
    /// </summary>
    public EntityId Payer { get; private init; }
    /// <summary>
    /// A list of keys having signed the pending transaction, when
    /// all necessary keyholders have signed, the network will attempt
    /// to execute the transaction.
    /// </summary>
    public Endorsement[] Endorsements { get; private init; }
    /// <summary>
    /// The endorsement key that can cancel this pending transaction.
    /// It may be null, in which case it can not be canceled and will
    /// exist until signed or expired by the network.
    /// </summary>
    public Endorsement? Administrator { get; private init; }
    /// <summary>
    /// Optional memo attached to the scheduling of 
    /// the pending transaction.
    /// </summary>
    public string? Memo { get; private init; }
    /// <summary>
    /// The time at which the pending transaction will expire
    /// and be removed from the network if not signed by 
    /// all necessary parties and executed.
    /// </summary>
    public ConsensusTimeStamp Expiration { get; private init; }
    /// <summary>
    /// If not null, the consensus time at which this pending
    /// transaction was completed and executed by the network.
    /// </summary>
    public ConsensusTimeStamp? Executed { get; private init; }
    /// <summary>
    /// If not null, the consensus time when this pending
    /// transaction was canceled using the administrative key.
    /// </summary>
    public ConsensusTimeStamp? Deleted { get; private init; }
    /// <summary>
    /// The body bytes of the pending transaction, serialized
    /// into the binary protobuf message format 
    /// of the SchedulableTransactionBody message.
    /// </summary>
    public ReadOnlyMemory<byte> PendingTransactionBody { get; private init; }
    /// <summary>
    /// Identification of the Ledger (Network) this 
    /// pending transaction information was retrieved from.
    /// </summary>
    public BigInteger Ledger { get; private init; }
    /// <summary>
    /// If set to <code>true</code>, the network will delay the
    /// attempt to execute the pending transaction until the
    /// expiration time, even if it receives sufficient signatures
    /// satisfying the signing requirements prior to the deadline.
    /// </summary>
    public bool DelayExecution { get; init; }
    /// <summary>
    /// Internal Constructor from Raw Results
    /// </summary>
    internal PendingTransactionInfo(Response response)
    {
        var info = response.ScheduleGetInfo.ScheduleInfo;
        Id = info.ScheduleID.ToAddress();
        TxId = info.ScheduledTransactionID.AsTxId();
        Creator = info.CreatorAccountID.AsAddress();
        Payer = info.PayerAccountID.AsAddress();
        Endorsements = info.Signers.ToEndorsements();
        Administrator = info.AdminKey?.ToEndorsement();
        Memo = info.Memo;
        Expiration = info.ExpirationTime.ToConsensusTimeStamp();
        Executed = info.ExecutionTime?.ToConsensusTimeStamp();
        Deleted = info.DeletionTime?.ToConsensusTimeStamp();
        PendingTransactionBody = info.ScheduledTransactionBody.ToByteArray();
        Ledger = new BigInteger(info.LedgerId.Span, true, true);
        DelayExecution = info.WaitForExpiry;
    }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class PendingTransactionInfoExtensions
{
    /// <summary>
    /// Retrieves detailed information regarding a pending transaction by ID.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="pending">
    /// The identifier (Payer/Schedule ID) of the pending transaction to retrieve.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A detailed description of the pending transaction, 
    /// including the serialized pending transaction body itself.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    public static async Task<PendingTransactionInfo> GetPendingTransactionInfoAsync(this ConsensusClient client, EntityId pending, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        return new PendingTransactionInfo(await Engine.QueryAsync(client, new ScheduleGetInfoQuery { ScheduleID = new ScheduleID(pending) }, cancellationToken, configure).ConfigureAwait(false));
    }
}