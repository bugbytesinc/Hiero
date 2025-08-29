using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Address creation parameters.
/// </summary>
public sealed class CreateAccountParams : TransactionParams, INetworkParams
{
    /// <summary>
    /// The public key structure representing the signature or signatures
    /// required to sign on behalf of this new account.  It can represent
    /// a single Ed25519 key, set of n-of-m keys or any other key structure
    /// supported by the network.
    /// </summary>
    public Endorsement? Endorsement { get; set; }
    /// <summary>
    /// The initial balance that will be transferred from the 
    /// <see cref="IConsensusContext.Payer"/> account to the new account 
    /// upon creation.
    /// </summary>
    public ulong InitialBalance { get; set; }
    /// <summary>
    /// When creating a new account: the newly created account must 
    /// sign any transaction transferring crypto into the newly 
    /// created account.
    /// </summary>
    public bool RequireReceiveSignature { get; set; } = false;
    /// <summary>
    /// The maximum number of token or NFTs that this account may
    /// be implicitly assoicated with (by means of being made a treasury
    /// or other related actions).
    /// </summary>
    /// <remarks>
    /// Defaults to zero.
    /// </remarks>
    public int AutoAssociationLimit { get; set; } = 0;
    /// <summary>
    /// The funds of this account will be staked to
    /// the node that this specified account is staked to 
    /// and the specified account will receive the earned reward.
    /// </summary>
    /// <remarks>
    /// This value must be set to <code>null</code> or
    /// <code>None</code> if the <code>StakedNode</code>
    /// property is set.
    /// </remarks>
    public EntityId? ProxyAccount { get; set; } = null;
    /// <summary>
    /// The funds of this account will be staked to
    /// the gossip node with the given ID.
    /// </summary>
    /// <remarks>
    /// Can not be greater than zero if the 
    /// <code>StakingProxy</code> property is set.
    /// </remarks>
    public long? StakedNode { get; set; } = null;
    /// <summary>
    /// Indicate to the network that this account
    /// does not wish to receive any earned 
    /// staking rewards.
    /// </summary>
    public bool DeclineStakeReward { get; set; } = false;
    /// <summary>
    /// The auto-renew period for the newly created account, it will continue 
    /// to be renewed at the given interval for as long as the account contains 
    /// hbars sufficient to cover the renewal charge.
    /// </summary>
    public TimeSpan AutoRenewPeriod { get; set; } = TimeSpan.FromSeconds(7890000);
    /// <summary>
    /// Optional Alias value to use to identify this newly created account.
    /// </summary>
    public Endorsement? KeyAlias { get; set; } = null;
    /// <summary>
    /// Short description of the account.
    /// </summary>
    public string? Memo { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to create this account.  Typically matches the
    /// Endorsement assigned to this new account.
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
        if (Endorsement.IsNullOrNone())
        {
            throw new ArgumentOutOfRangeException(nameof(Endorsement), "The Endorsement for the account is missing, it is required.");
        }
        if (AutoAssociationLimit < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(AutoAssociationLimit), "The maximum number of auto-associations must equal to or greater than -1.");
        }
        var result = new CryptoCreateTransactionBody();
        if (ProxyAccount.IsNullOrNone())
        {
            if (StakedNode.HasValue)
            {
                result.StakedNodeId = StakedNode.Value;
            }
        }
        else if (StakedNode.HasValue)
        {
            throw new ArgumentNullException(nameof(ProxyAccount), "Both the ProxyAccount and StakedNode properties are specified, only one can be set.");
        }
        else
        {
            result.StakedAccountId = new AccountID(ProxyAccount);
        }
        result.Key = new Key(Endorsement);
        result.InitialBalance = InitialBalance;
        result.ReceiverSigRequired = RequireReceiveSignature;
        result.AutoRenewPeriod = new Duration(AutoRenewPeriod);
        if (!KeyAlias.IsNullOrNone())
        {
            result.Alias = new Key(KeyAlias).ToByteString();
        }
        result.DeclineReward = DeclineStakeReward;
        result.Memo = Memo ?? string.Empty;
        result.MaxAutomaticTokenAssociations = AutoAssociationLimit;
        return result;
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new CreateAccountReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "Create Account";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CreateAccountExtensions
{
    /// <summary>
    /// Creates a new network account with a given initial balance
    /// and other values as indicated in the create parameters.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the create.
    /// </param>
    /// <param name="createParameters">
    /// The account creation parameters, includes the initial balance,
    /// public key and values associated with the new account.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction recipt with a description of the newly created account.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<CreateAccountReceipt> CreateAccountAsync(this ConsensusClient client, CreateAccountParams createParameters, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<CreateAccountReceipt>(createParameters, configure);
    }
}