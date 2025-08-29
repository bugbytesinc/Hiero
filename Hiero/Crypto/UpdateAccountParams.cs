using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Represents the properties on an account that can be changed.
/// Any property set to <code>null</code> on this object when submitted to the 
/// <see cref="ConsensusClient.UpdateAccountAsync(UpdateAccountParams, Action{IConsensusContext})"/>
/// method will be left unchanged by the system.  Certain additional condidions
/// apply to certain propertites such as the signing key are described below.
/// </summary>
public sealed class UpdateAccountParams : TransactionParams, INetworkParams
{
    /// <summary>
    /// The network address of account to update.
    /// </summary>
    public EntityId Address { get; set; } = default!;
    /// <summary>
    /// Replace this Address's current key signing rquirements with new signing
    /// requirements.</summary>
    /// <remarks>
    /// For this request to be accepted by the network, both the current private
    /// key(s) for this account and the new private key(s) must sign the transaction.  
    /// The existing key must sign for security and the new key must sign as a 
    /// safeguard to avoid accidentally changing the key to an invalid value.  
    /// Either the <see cref="IConsensusContext.Payer"/> account or 
    /// <see cref="UpdateAccountParams.Address"/> may carry the new private key 
    /// for signing to meet this requirement.
    /// </remarks>
    public Endorsement? Endorsement { get; set; }
    /// <summary>
    /// If set to True, the account must sign any transaction 
    /// transferring crypto into account.
    /// </summary>
    public bool? RequireReceiveSignature { get; set; }
    /// <summary>
    /// The new expiration date for this account, it will be ignored
    /// if it is equal to or before the current expiration date value
    /// for this account.
    /// </summary>
    public ConsensusTimeStamp? Expiration { get; set; }
    /// <summary>
    /// Incremental period for auto-renewal of the account. If
    /// account does not have sufficient funds to renew at the
    /// expiration time, it will be renewed for a period of time
    /// the remaining funds can support.  If no funds remain, the
    /// account will be deleted.
    /// </summary>
    public TimeSpan? AutoRenewPeriod { get; set; }
    /// <summary>
    /// If not null, a new description of the account.
    /// </summary>
    public string? Memo { get; set; }
    /// <summary>
    /// If set, updates the maximum number of token or NFTs that this account may
    /// be implicitly assoicated with (by means of being made a treasury
    /// or other related actions).
    /// </summary>
    public int? AutoAssociationLimit { get; set; }
    /// <summary>
    /// If set, updates this account's staking proxy
    /// account.  If set The funds of this account will 
    /// be staked to the node that this account is staked 
    /// to and the specified proxy account will receive 
    /// the earned reward.
    /// </summary>
    public EntityId? ProxyAccount { get; set; }
    /// <summary>
    /// If set, updates this accounts's staked node.
    /// The funds of this account will be staked to
    /// the gossip node with the given ID.
    /// </summary>
    /// <remarks>
    /// Node IDs are used instead of node account
    /// IDs because a node has the ability to change
    /// its wallet account ID.
    /// </remarks>
    public long? StakedNode { get; set; }
    /// <summary>
    /// If set, updates the flag indicating to the network 
    /// that this account does not wish to receive any 
    /// earned staking rewards.
    /// </summary>
    public bool? DeclineStakeReward { get; set; }
    /// <summary>
    /// Any additional signing keys required to validate the transaction
    /// that are not already specified in the client object's context.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token that interrupt the creation proces.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams.CreateNetworkTransaction()
    {
        if (Address is null)
        {
            throw new ArgumentNullException(nameof(Address), "Account is missing. Please check that it is not null.");
        }
        if (Endorsement.None.Equals(Endorsement))
        {
            throw new ArgumentOutOfRangeException(nameof(Endorsement), "Endorsement can not be 'None', it must contain at least one key requirement.");
        }
        if (Endorsement is null &&
            RequireReceiveSignature is null &&
            Expiration is null &&
            AutoRenewPeriod is null &&
            Memo is null &&
            AutoAssociationLimit is null &&
            ProxyAccount is null &&
            StakedNode is null &&
            DeclineStakeReward is null)
        {
            throw new ArgumentException("The Account Updates contains no update properties, it is blank.", nameof(UpdateAccountParams));
        }
        var result = new CryptoUpdateTransactionBody();
        result.AccountIDToUpdate = new AccountID(Address);
        if (Endorsement is not null)
        {
            result.Key = new Key(Endorsement);
        }
        if (RequireReceiveSignature.HasValue)
        {
            result.ReceiverSigRequiredWrapper = RequireReceiveSignature.Value;
        }
        if (AutoRenewPeriod.HasValue)
        {
            result.AutoRenewPeriod = new Duration(AutoRenewPeriod.Value);
        }
        if (Expiration.HasValue)
        {
            result.ExpirationTime = new Timestamp(Expiration.Value);
        }
        if (Memo is not null)
        {
            result.Memo = Memo;
        }
        if (AutoAssociationLimit is not null)
        {
            var limit = AutoAssociationLimit.Value;
            if (limit < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(AutoAssociationLimit), "The number of auto-associaitons must be greater than or equal to -1.");
            }
            result.MaxAutomaticTokenAssociations = limit;
        }
        if (ProxyAccount is not null)
        {
            if (StakedNode is not null)
            {
                throw new ArgumentOutOfRangeException(nameof(ProxyAccount), "Can not set ProxyAccount and StakedNode at the same time.");
            }
            result.StakedAccountId = new AccountID(ProxyAccount);
        }
        if (StakedNode is not null)
        {
            result.StakedNodeId = StakedNode.Value;
        }
        if (DeclineStakeReward is not null)
        {
            result.DeclineReward = DeclineStakeReward.Value;
        }
        return result;
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "Account Update";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class UpdateAccountExtensions
{
    /// <summary>
    /// Updates the changeable properties of a hedera network account.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client occhestrating the update.
    /// </param>
    /// <param name="updateParameters">
    /// The account update parameters, includes a required 
    /// <see cref="EntityId"/> reference to the account to update plus
    /// a number of changeable properties of the account.
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
    public static Task<TransactionReceipt> UpdateAccountAsync(this ConsensusClient client, UpdateAccountParams updateParameters, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<TransactionReceipt>(updateParameters, configure);
    }
}