using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Numerics;

namespace Hiero;
/// <summary>
/// Information about a Hedera Address, including public key, evm address
/// and other account related information.
/// </summary>
public sealed record AccountInfo
{
    /// <summary>
    /// The Hedera address of this account.
    /// </summary>
    public EntityId Address { get; private init; }
    /// <summary>
    /// The identity of the Hedera Address in a form to be
    /// used with smart contracts.  This can also be the
    /// ID of a smart contract instance if this is the account
    /// associated with a smart contract.
    /// </summary>
    public EvmAddress EvmAddress { get; private init; }
    /// <summary>
    /// The smart contract (EVM) transaction counter nonce
    /// associated with this account.
    /// </summary>
    public long EvmNonce { get; private init; }
    /// <summary>
    /// <code>True</code> if this account has been deleted.
    /// Its existence in the network will cease after the expiration
    /// date for the account lapses.  It cannot participate in
    /// transactions except to extend the expiration/removal date.
    /// </summary>
    public bool Deleted { get; private init; }
    /// <summary>
    /// Address's Public Key (typically a single Ed25519 key).
    /// </summary>
    public Endorsement Endorsement { get; private init; }
    /// <summary>
    /// Address Balance in Tinybars
    /// </summary>
    public ulong Balance { get; private init; }
    /// <summary>
    /// [DEPRECATED] Balances of tokens and NFTs associated with this account.
    /// </summary>
    [Obsolete("This field is deprecated by HIP-367")]
    public IReadOnlyList<TokenBalance> Tokens { get; private init; }
    /// <summary>
    /// <code>True</code> if any receipt of funds require
    /// a signature from this account.
    /// </summary>
    public bool ReceiveSignatureRequired { get; private init; }
    /// <summary>
    /// Incremental period for auto-renewal of the account. If
    /// account does not have sufficient funds to renew at the
    /// expiration time, it will be renewed for a period of time
    /// the remaining funds can support.  If no funds remain, the
    /// account will be deleted.
    /// </summary>
    public TimeSpan AutoRenewPeriod { get; private init; }
    /// <summary>
    /// The account expiration time, at which it will attempt
    /// to renew if sufficient funds remain in the account.
    /// </summary>
    public ConsensusTimeStamp Expiration { get; private init; }
    /// <summary>
    /// A short description associated with the account.
    /// </summary>
    public string Memo { get; private init; }
    /// <summary>
    /// The number of NFTs (non fungible tokens) held
    /// by this account.
    /// </summary>
    public long NftCount { get; private init; }
    /// <summary>
    /// The maximum number of token or NFTs that this account may
    /// be implicitly associated with (by means of being made a treasury
    /// or other related actions).
    /// </summary>
    public int AutoAssociationLimit { get; private init; }
    /// <summary>
    /// The alternate identifier associated with this account that is
    /// in the form of a public key.  If an alternate identifier for this
    /// account does not exist, this value will be <code>None</code>.
    /// </summary>
    public Endorsement KeyAlias { get; private init; }
    /// <summary>
    /// Identification of the Ledger (Network) this 
    /// account information was retrieved from.
    /// </summary>
    public BigInteger Ledger { get; private init; }
    /// <summary>
    /// Staking Metadata Information for the account.
    /// </summary>
    public StakingInfo StakingInfo { get; private init; }
    /// <summary>
    /// Internal Constructor from Raw Response
    /// </summary>
    internal AccountInfo(Response response)
    {
        var info = response.CryptoGetInfo.AccountInfo;
        Address = info.AccountID.AsAddress();
        EvmAddress = string.IsNullOrWhiteSpace(info.ContractAccountID) ? EvmAddress.None : new EvmAddress(Hex.ToBytes(info.ContractAccountID));
        EvmNonce = info.EthereumNonce;
        Deleted = info.Deleted;
        Endorsement = info.Key.ToEndorsement();
        Balance = info.Balance;
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning disable CS0612 // Type or member is obsolete
        Tokens = info.TokenRelationships.ToBalances();
#pragma warning restore CS0612 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
        ReceiveSignatureRequired = info.ReceiverSigRequired;
        AutoRenewPeriod = info.AutoRenewPeriod.ToTimeSpan();
        // v0.34.0 Churn
        //AutoRenewAccount = info.AutoRenewAccount.AsAddress();
        Expiration = info.ExpirationTime.ToConsensusTimeStamp();
        Memo = info.Memo;
        NftCount = info.OwnedNfts;
        AutoAssociationLimit = info.MaxAutomaticTokenAssociations;
        KeyAlias = info.Alias is not null && !info.Alias.IsEmpty ? Key.Parser.ParseFrom(info.Alias).ToEndorsement() : Endorsement.None;
        Ledger = new BigInteger(info.LedgerId.Span, true, true);
        StakingInfo = new StakingInfo(info.StakingInfo);
    }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AccountInfoExtensions
{
    /// <summary>
    /// Retrieves detailed information regarding a Hedera Network Address.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="address">
    /// The Hedera Network Address to retrieve detailed information of.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A detailed description of the account.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    public static async Task<AccountInfo> GetAccountInfoAsync(this ConsensusClient client, EntityId address, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        return new AccountInfo(await Engine.QueryAsync(client, new CryptoGetInfoQuery { AccountID = new AccountID(address) }, cancellationToken, configure).ConfigureAwait(false));
    }
}