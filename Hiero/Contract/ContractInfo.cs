using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Numerics;

namespace Hiero;
/// <summary>
/// The information returned from the GetContractInfoAsync ConsensusClient method call.  
/// It represents the details concerning a Hedera Network Contract instance, including 
/// the public key value to use in smart contract interaction.
/// </summary>
public sealed record ContractInfo
{
    /// <summary>
    /// ID of the contract instance.
    /// </summary>
    public EntityId Contract { get; private init; }
    /// <summary>
    /// The ID of the Crypto Currency Address 
    /// paired with this contract instance.
    /// </summary>
    public EntityId Account { get; private init; }
    /// <summary>
    /// The identity of both the contract ID and the associated
    /// crypto currency Hedera Address in a form to be
    /// used with smart contracts.  
    /// </summary>
    public EvmAddress EvmAddress { get; private init; }
    /// <summary>
    /// An optional endorsement that can be used to modify the contract details.  
    /// If null, the contract is immutable.
    /// </summary>
    public Endorsement? Administrator { get; private init; }
    /// <summary>
    /// The consensus time at which this instance of the contract is
    /// (and associated account) is set to expire.
    /// </summary>
    public ConsensusTimeStamp Expiration { get; private init; }
    /// <summary>
    /// Incremental period for auto-renewal of the contract and account. If
    /// account does not have sufficient funds to renew at the
    /// expiration time, it will be renewed for a period of time
    /// the remaining funds can support.  If no funds remain, the
    /// contract instance and associated account will be deleted.
    /// </summary>
    public TimeSpan RenewPeriod { get; private init; }
    /// <summary>
    /// Optional address of the account supporting the auto renewal of 
    /// the contract at expiration time.  The contract lifetime will be
    /// extended by the RenewPeriod at expiration time if this account
    /// contains sufficient funds.  The private key associated with
    /// this account must sign the transaction if RenewAccount is
    /// specified.
    /// </summary>
    public EntityId? RenewAccount { get; private init; }
    /// <summary>
    /// The number of bytes of required to store this contract instance.
    /// This value impacts the cost of extending the expiration time.
    /// </summary>
    public long Size { get; private init; }
    /// <summary>
    /// The memo associated with the contract instance.
    /// </summary>
    public string Memo { get; private init; }
    /// <summary>
    /// Contract's Address's Crypto Balance in Tinybars
    /// </summary>
    public ulong Balance { get; private init; }
    /// <summary>
    /// [DPRICATED] Balances of tokens associated with this account.
    /// </summary>
    [Obsolete("This field is deprecated by HIP-367")]
    public IReadOnlyList<TokenBalance> Tokens { get; private init; }
    /// <summary>
    /// <code>True</code> if this contract has been deleted.
    /// </summary>
    public bool Deleted { get; private init; }
    /// <summary>
    /// The maximum number of token or NFTs that this contract may
    /// be implicitly assoicated with (by means of being made a treasury
    /// or other related actions).
    /// </summary>
    public int AutoAssociationLimit { get; private init; }
    /// <summary>
    /// Identification of the Ledger (Network) this 
    /// contract information was retrieved from.
    /// </summary>
    public BigInteger Ledger { get; private init; }
    /// <summary>
    /// Staking Metadata Information for the account.
    /// </summary>
    public StakingInfo StakingInfo { get; private init; }
    /// <summary>
    /// Internal Constructor from Raw Results
    /// </summary>
    internal ContractInfo(Response response)
    {
        var info = response.ContractGetInfo.ContractInfo;
        Contract = info.ContractID.AsAddress();
        Account = info.AccountID.AsAddress();
        EvmAddress = EvmAddress.TryParse(info.ContractAccountID, out var evmAddress) ? evmAddress : EvmAddress.None;
        Administrator = info.AdminKey?.ToEndorsement();
        Expiration = info.ExpirationTime.ToConsensusTimeStamp();
        RenewPeriod = info.AutoRenewPeriod.ToTimeSpan();
        RenewAccount = info.AutoRenewAccountId?.AsAddress();
        Size = info.Storage;
        Memo = info.Memo;
        Balance = info.Balance;
#pragma warning disable CS0612 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
        Tokens = info.TokenRelationships.ToBalances();
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning restore CS0612 // Type or member is obsolete
        Deleted = info.Deleted;
        AutoAssociationLimit = info.MaxAutomaticTokenAssociations;
        Ledger = new BigInteger(info.LedgerId.Span, true, true);
        StakingInfo = new StakingInfo(info.StakingInfo);
    }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ContractInfoExtensions
{
    /// <summary>
    /// Retrieves the bytecode for the specified contract.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="contract">
    /// The Hedera Network Payer of the Contract.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// The bytecode for the specified contract instance.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    public static async Task<ReadOnlyMemory<byte>> GetContractBytecodeAsync(this ConsensusClient client, EntityId contract, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        return (await Engine.QueryAsync(client, new ContractGetBytecodeQuery { ContractID = new ContractID(contract) }, cancellationToken, configure).ConfigureAwait(false)).ContractGetBytecodeResponse.Bytecode.Memory;
    }
    /// <summary>
    /// Retrieves detailed information regarding a Smart Contract Instance.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="contract">
    /// The Hedera Network Payer of the Contract instance to retrieve.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A detailed description of the contract instance.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    public static async Task<ContractInfo> GetContractInfoAsync(this ConsensusClient client, EntityId contract, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        return new ContractInfo(await Engine.QueryAsync(client, new ContractGetInfoQuery { ContractID = new ContractID(contract) }, cancellationToken, configure).ConfigureAwait(false));
    }
}