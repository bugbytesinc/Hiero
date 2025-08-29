using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Consensus Topics Creation Parameters.
/// </summary>
public sealed class CreateTopicParams : TransactionParams, INetworkParams
{
    /// <summary>
    /// Short description of the topic, not checked for uniqueness.
    /// </summary>
    public string? Memo { get; set; }
    /// <summary>
    /// An optional endorsement, when specified, can be used to 
    /// authorize a modification or deletion of this topic, including
    /// control of topic lifetime extensions. Additionally, if null, 
    /// any account can extend the topic lifetime.
    /// </summary>
    public Endorsement? Administrator { get; set; }
    /// <summary>
    /// Identify any key requirements for submitting messages to this topic.
    /// If left blank, any account may submit messages to this topic, 
    /// otherwise they must meet the specified signing requirements.
    /// </summary>
    public Endorsement? Submitter { get; set; }
    /// <summary>
    /// Initial lifetime of the topic and auto-renewal period. If
    /// the associated account does not have sufficient funds to 
    /// renew at the expiration time, it will be renewed for a period 
    /// of time the remaining funds can support.  If no funds remain, the
    /// topic instance will be deleted.
    /// </summary>
    public TimeSpan RenewPeriod { get; set; } = TimeSpan.FromDays(90);
    /// <summary>
    /// Optional address of the account supporting the auto renewal of 
    /// the topic at expiration time.  The topic lifetime will be
    /// extended by the RenewPeriod at expiration time if this account
    /// contains sufficient funds.  The private key associated with
    /// this account must sign the transaction if RenewAccount is
    /// specified.
    /// </summary>
    /// <remarks>
    /// If specified, an Administrator Endorsement must also be specified.
    /// </remarks>
    public EntityId? RenewAccount { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to create to this topic.  Typically matches the
    /// Administrator, Submitter and RenwAccount key(s)
    /// associated with this topic.
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
    INetworkTransaction INetworkParams.CreateNetworkTransaction()
    {
        if (Memo is null)
        {
            throw new ArgumentNullException(nameof(Memo), "Memo can not be null.");
        }
        if (!(RenewAccount is null) && Administrator is null)
        {
            throw new ArgumentNullException(nameof(Administrator), "The Administrator endorssement must not be null if RenewAccount is specified.");
        }
        if (RenewPeriod.Ticks < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(RenewPeriod), "The renew period must be greater than zero, and typically less than or equal to 90 days.");
        }
        return new ConsensusCreateTopicTransactionBody()
        {
            Memo = Memo,
            AdminKey = Administrator is null ? null : new Key(Administrator),
            SubmitKey = Submitter is null ? null : new Key(Submitter),
            AutoRenewPeriod = new Duration(RenewPeriod),
            AutoRenewAccount = RenewAccount is null ? null : new AccountID(RenewAccount)
        };
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new CreateTopicReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "Create Topic";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CreateTopicExtensions
{
    /// <summary>
    /// Creates a new topic instance with the given create parameters.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the create.
    /// </param>
    /// <param name="createParameters">
    /// Details regarding the topic to instantiate.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt with a description of the newly created topic.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<CreateTopicReceipt> CreateTopicAsync(this ConsensusClient client, CreateTopicParams createParameters, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<CreateTopicReceipt>(createParameters, configure);
    }
}