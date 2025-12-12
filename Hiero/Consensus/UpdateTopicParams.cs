using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Represents the properties on a topic that can be changed.
/// Any property set to <code>null</code> on this object when submitted to the 
/// <see cref="ConsensusClient.UpdateTopicAsync(UpdateTopicParams, Action{IConsensusContext})"/>
/// method will be left unchanged by the system.  The transaction must be
/// appropriately signed as described by the original
/// <see cref="CreateTopicParams.Administrator"/> endorsement in order
/// to make changes.  If there is no administrator endorsement specified,
/// the topic is immutable and cannot be changed.
/// </summary>
public sealed class UpdateTopicParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The network address of the topic to update.
    /// </summary>
    public EntityId Topic { get; set; } = default!;
    /// <summary>
    /// The publicly visible memo to be associated with the topic.
    /// </summary>
    public string? Memo { get; set; }
    /// <summary>
    /// Replace this Topic's current administrative key signing requirements 
    /// with new signing requirements.  To completely remove the administrator
    /// key and make the Topic immutable, use the <see cref="Endorsement.None"/>
    /// endorsement value.
    /// </summary>
    /// <remarks>
    /// For this request to be accepted by the network, both the current private
    /// key(s) for this account and the new private key(s) must sign the transaction.  
    /// The existing key must sign for security and the new key must sign as a 
    /// safeguard to avoid accidentally changing the key to an invalid value.  
    /// </remarks>
    public Endorsement? Administrator { get; set; }
    /// <summary>
    /// Identify any key requirements for submitting messages to this topic.
    /// If left blank, no changes will be made. To completely remove the 
    /// key requirements and make the Topic open for all to submit, use
    /// the <see cref="Endorsement.None"/> endorsement value.
    /// </summary>
    public Endorsement? Submitter { get; set; }
    /// <summary>
    /// The new expiration date for this topic, it will be ignored
    /// if it is equal to or before the current expiration date value
    /// for this topic.  This allows non-administrator accounts to
    /// extend the lifetime of this topic when no auto renew 
    /// account has been specified.
    /// </summary>
    public ConsensusTimeStamp? Expiration { get; set; }
    /// <summary>
    /// Incremental period for auto-renewal of the topic account. If
    /// the associated account does not have sufficient funds to 
    /// renew at the expiration time, it will be renewed for a period 
    /// of time the remaining funds can support.  If no funds remain, the
    /// topic instance will be deleted.
    /// </summary>
    public TimeSpan? RenewPeriod { get; set; }
    /// <summary>
    /// Optional address of the account supporting the auto renewal of 
    /// the topic at expiration time.  The topic lifetime will be
    /// extended by the RenewPeriod at expiration time if this account
    /// contains sufficient funds.  The private key associated with
    /// this account must sign the transaction if RenewAccount is
    /// specified.
    /// </summary>
    public EntityId? RenewAccount { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to update this topic.  Typically matches the
    /// Administrator endorsement associated with this contract.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token that interrupt the token
    /// submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        if (Topic is null)
        {
            throw new ArgumentNullException(nameof(Topic), "Topic address is missing. Please check that it is not null.");
        }
        if (Memo is null &&
            Administrator is null &&
            Submitter is null &&
            Expiration is null &&
            RenewPeriod is null &&
            RenewAccount is null)
        {
            throw new ArgumentException("The Topic Updates contain no update properties, it is blank.", nameof(UpdateTopicParams));
        }
        var result = new ConsensusUpdateTopicTransactionBody()
        {
            TopicID = new TopicID(Topic)
        };
        if (Memo != null)
        {
            result.Memo = Memo;
        }
        if (Administrator is not null)
        {
            result.AdminKey = new Key(Administrator);
        }
        if (Submitter is not null)
        {
            result.SubmitKey = new Key(Submitter);
        }
        if (Expiration.HasValue)
        {
            result.ExpirationTime = new Timestamp(Expiration.Value);
        }
        if (RenewPeriod.HasValue)
        {
            result.AutoRenewPeriod = new Duration(RenewPeriod.Value);
        }
        if (RenewAccount is not null)
        {
            result.AutoRenewAccount = new AccountID(RenewAccount);
        }
        return result;
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Topic Update";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class UpdateTopicExtensions
{
    /// <summary>
    /// Updates the changeable properties of a Hedera Network Topic.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client receiving the update transaction.
    /// </param>
    /// <param name="updateParameters">
    /// The Topic update parameters, includes a required 
    /// <see cref="EntityId"/> reference to the Topic to update plus
    /// a number of changeable properties of the Topic.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating success of the operation.
    /// of the request.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> UpdateTopicAsync(this ConsensusClient client, UpdateTopicParams updateParameters, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(updateParameters, configure);
    }
}