using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Smart Contract creation properties.
/// </summary>
public sealed class CreateContractParams : TransactionParams, INetworkParams
{
    /// <summary>
    /// The address of the file containing the bytecode for the contract. 
    /// The bytecode in the file must be encoded as a hexadecimal string 
    /// representation of the bytecode in the file (not directly as the 
    /// bytes of the bytescode).
    /// 
    /// Typically this field is used for contracts that are so large
    /// that they can not be represented in the transaction size
    /// limit, otherwise the <code>ByteCode</code> property can be set
    /// instead, avoiding the extra fees of uploading a file.
    /// 
    /// This field must be set to <code>None</code> or <code>null</code>
    /// if the <code>ByteCode</code> property is set.
    /// </summary>
    public EntityId File { get; set; } = default!;
    /// <summary>
    /// The binary byte code representing the contract.  This field must
    /// be left <code>Empty</code> if the <code>File</code> property
    /// is specified.  The byte code must fit within the size of
    /// a hapi transaction, or the <code>File</code> option of uploading
    /// a larger contract to a file first must be utilized.
    /// </summary>
    public ReadOnlyMemory<byte> ByteCode { get; set; }
    /// <summary>
    /// An optional endorsement that can be used to modify the contract details.  
    /// If left null, the contract is immutable once created.
    /// </summary>
    public Endorsement? Administrator { get; set; }
    /// <summary>
    /// Maximum gas to pay for executing the constructor method.
    /// </summary>
    public long Gas { get; set; }
    /// <summary>
    /// The renewal period for maintaining the contract bytecode and state.  
    /// The contract instance will be charged at this interval as appropriate.
    /// </summary>
    public TimeSpan RenewPeriod { get; set; }
    /// <summary>
    /// Optional address of the account supporting the auto renewal of 
    /// the contract at expiration time.  The contract lifetime will be
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
    /// The initial value in tinybars to send to this contract instance.  
    /// If the contract is not payable, providing a non-zero value will result 
    /// in a contract create failure.
    /// </summary>
    public long InitialBalance { get; set; }
    /// <summary>
    /// The arguments to pass to the smart contract constructor method.
    /// </summary>
    public object[] ConstructorArgs { get; set; } = default!;
    /// <summary>
    /// The maximum number of token or NFTs that this contract may
    /// be implicitly assoicated with (by means of being made a treasury
    /// or other related actions).
    /// </summary>
    /// <remarks>
    /// Defaults to zero.
    /// </remarks>
    public int AutoAssociationLimit { get; set; } = 0;
    /// <summary>
    /// The funds of this contract will be staked to
    /// the node that this account is staked to and the
    /// specified account will receive the earned reward.
    /// </summary>
    /// <remarks>
    /// This value must be set to <code>null</code> or
    /// <code>None</code> if the <code>StakedNode</code>
    /// property is set.
    /// </remarks>
    public EntityId? StakingProxy { get; set; } = null;
    /// <summary>
    /// The funds of this contract will be staked to
    /// the gossip node with the given ID.
    /// </summary>
    /// <remarks>
    /// Can not be set if the 
    /// <code>StakingProxy</code> property is set.
    /// </remarks>
    public long? StakedNode { get; set; }
    /// <summary>
    /// Indicate to the network that this contract
    /// does not wish to receive any earned staking
    /// rewards.
    /// </summary>
    public bool DeclineStakeReward { get; set; } = false;
    /// <summary>
    /// Short description of the contract, limit to 100 bytes.
    /// </summary>
    public string? Memo { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to create this contract.  Typically matches the
    /// Administrator endorsement assigned to this new contract.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token that interrupt the contract creation.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams.CreateNetworkTransaction()
    {
        var result = new ContractCreateTransactionBody();
        if (AutoAssociationLimit < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(AutoAssociationLimit), "The maximum number of auto-associaitons must greater than or equal to -1");
        }
        if (File.IsNullOrNone())
        {
            if (ByteCode.IsEmpty)
            {
                throw new ArgumentNullException(nameof(File), "Both the File address and ByteCode properties missing, one must be specified.");
            }
            result.Initcode = ByteString.CopyFrom(ByteCode.Span);
        }
        else if (!ByteCode.IsEmpty)
        {
            throw new ArgumentException("Both the File address and ByteCode properties are specified, only one can be set.", nameof(File));
        }
        else
        {
            result.FileID = new FileID(File);
        }
        if (StakingProxy.IsNullOrNone())
        {
            if (StakedNode.HasValue)
            {
                result.StakedNodeId = StakedNode.Value;
            }
        }
        else if (StakedNode.HasValue)
        {
            throw new ArgumentNullException(nameof(StakingProxy), "Both the ProxyAccount and StakedNode properties are specified, only one can be set.");
        }
        else
        {
            result.StakedAccountId = new AccountID(StakingProxy);
        }
        result.AdminKey = Administrator is null ? null : new Key(Administrator);
        result.Gas = Gas;
        result.InitialBalance = InitialBalance;
        result.MaxAutomaticTokenAssociations = AutoAssociationLimit;
        result.AutoRenewPeriod = new Duration(RenewPeriod);
        result.AutoRenewAccountId = RenewAccount.IsNullOrNone() ? null : new AccountID(RenewAccount);
        result.ConstructorParameters = ByteString.CopyFrom(Abi.EncodeArguments(ConstructorArgs).ToArray());
        result.DeclineReward = DeclineStakeReward;
        result.Memo = Memo ?? "";
        return result;
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new CreateContractReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "Create Contract";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CreateContractExtensions
{
    /// <summary>
    /// Creates a new contract instance with the given create parameters.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client executing the contract create.
    /// </param>
    /// <param name="createParameters">
    /// Details regarding the contract to instantiate.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt with a description of the newly created contract.
    /// and receipt information.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<CreateContractReceipt> CreateContractAsync(this ConsensusClient client, CreateContractParams createParameters, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<CreateContractReceipt>(createParameters, configure);
    }
}