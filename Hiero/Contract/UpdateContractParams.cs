using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Represents the properties on a contract that can be changed.
/// Any property set to <code>null</code> on this object when submitted to the 
/// <see cref="ConsensusClient.UpdateContractAsync(UpdateContractParams, Action{IConsensusContext})"/>
/// method will be left unchanged by the system.  The transaction must be
/// appropriately signed as described by the original
/// <see cref="CreateContractParams.Administrator"/> endorsement in order
/// to make changes.  If there is no administrator endorsement specified,
/// the contract is imutable and cannot be changed.
/// </summary>
public sealed class UpdateContractParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The network address of the contract to update.
    /// </summary>
    public EntityId Contract { get; set; } = default!;
    /// <summary>
    /// The new expiration date for this contract instance, it will be 
    /// ignored if it is equal to or before the current expiration date 
    /// value for this contract.
    /// </summary>
    /// <remarks>
    /// NOTE: Presently this functionality is not correctly implemented
    /// by the network.  Therefore this property is makred internal so
    /// it can not be mistakenly used.  When properly implemented by
    /// the network, this property will be made public again.
    /// </remarks>
    internal ConsensusTimeStamp? Expiration { get; set; }
    /// <summary>
    /// Replace this Contract's current administrative key signing rquirements 
    /// with new signing requirements.</summary>
    /// <remarks>
    /// For this request to be accepted by the network, both the current private
    /// key(s) for this account and the new private key(s) must sign the transaction.  
    /// The existing key must sign for security and the new key must sign as a 
    /// safeguard to avoid accidentally changing the key to an invalid value.  
    /// The <see cref="IConsensusContext.Payer"/> account must carry the old and new 
    /// private keys for signing to meet this requirement.
    /// </remarks>
    public Endorsement? Administrator { get; set; }
    /// <summary>
    /// Incremental period for auto-renewal of the contract account. If
    /// account does not have sufficient funds to renew at the
    /// expiration time, it will be renewed for a period of time
    /// the remaining funds can support.  If no funds remain, the
    /// account will be deleted.
    /// </summary>
    public TimeSpan? RenewPeriod { get; set; }
    /// <summary>
    /// If specified updates the address of the account supporting the auto 
    /// renewal of the contract at expiration time.  The topic lifetime will be
    /// extended by the RenewPeriod at expiration time if this account
    /// contains sufficient funds.  The private key associated with
    /// this account must sign the transaction if RenewAccount is
    /// specified.  Setting the value to <code>Payer.None</code> clears the
    /// renewal account.
    /// </summary>
    public EntityId? RenewAccount { get; set; }
    /// <summary>
    /// The memo to be associated with the contract.  Maximum
    /// of 100 bytes.
    /// </summary>
    public string? Memo { get; set; }
    /// <summary>
    /// If set, updates the maximum number of token or NFTs that this contract may
    /// be implicitly assoicated with (by means of being made a treasury
    /// or other related actions).
    /// </summary>
    public int? AutoAssociationLimit { get; set; }
    /// <summary>
    /// If set, updates this contract's staking proxy
    /// account.  If set The funds of this contract will 
    /// be staked to the node that this account is staked 
    /// to and the specified account will receive 
    /// the earned reward.
    /// </summary>
    public EntityId? ProxyAccount { get; set; }
    /// <summary>
    /// If set, updates this contract's staked node.
    /// The funds of this contract will be staked to
    /// the gossip node with the given ID.
    /// </summary>
    public long? StakedNode { get; set; }
    /// <summary>
    /// If set, updates the flag indicating to the network 
    /// that this contract does not wish to receive any 
    /// earned staking rewards.
    /// </summary>
    public bool? DeclineStakeReward { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to update this contract.  Typically matches the
    /// Administrator endorsement associated with this contract.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token that interrupt the contract update.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        if (Contract.IsNullOrNone())
        {
            throw new ArgumentNullException(nameof(Contract), "Contract address is missing. Please check that it is not null.");
        }
        if (Expiration is null &&
            Administrator is null &&
            RenewPeriod is null &&
            RenewAccount is null &&
            Memo is null &&
            ProxyAccount is null &&
            StakedNode is null &&
            DeclineStakeReward is null &&
            AutoAssociationLimit is null)
        {
            throw new ArgumentException("The Contract Updates contains no update properties, it is blank.", nameof(UpdateContractParams));
        }
        var result = new ContractUpdateTransactionBody()
        {
            ContractID = new ContractID(Contract)
        };
        if (Expiration.HasValue)
        {
            result.ExpirationTime = new Timestamp(Expiration.Value);
        }
        if (Administrator is not null)
        {
            result.AdminKey = new Key(Administrator);
        }
        if (RenewPeriod.HasValue)
        {
            result.AutoRenewPeriod = new Duration(RenewPeriod.Value);
        }
        if (RenewAccount is not null)
        {
            result.AutoRenewAccountId = new AccountID(RenewAccount);
        }
        if (!string.IsNullOrWhiteSpace(Memo))
        {
            result.MemoWrapper = Memo;
        }
        if (AutoAssociationLimit is not null)
        {
            var limit = AutoAssociationLimit.Value;
            if (limit < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(AutoAssociationLimit), "The maximum number of auto-associations must greater than or equal to -1");
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
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Contract Update";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class UpdateContractExtensions
{
    /// <summary>
    /// Updates the changeable properties of a hedera network contract.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client executing the contract update.
    /// </param>
    /// <param name="updateParameters">
    /// The contract update parameters, includes a required 
    /// <see cref="EntityId"/> reference to the Contract to update plus
    /// a number of changeable properties of the Contract.
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
    public static Task<TransactionReceipt> UpdateContractAsync(this ConsensusClient client, UpdateContractParams updateParameters, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(updateParameters, configure);
    }
}