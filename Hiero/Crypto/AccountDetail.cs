using Hiero.Implementation;
using Proto;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Hiero;

/// <summary>
/// The information returned from the CreateAccountAsync ConsensusClient method call.  
/// It represents the details concerning a Hedera Network Address, including 
/// the public key value to use in smart contract interaction.
/// </summary>
public sealed record AccountDetail
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
    /// <code>True</code> if this account has been deleted.
    /// Its existence in the network will cease after the expiration
    /// date for the account lapses.  It cannot participate in
    /// transactions except to extend the expiration/removal date.
    /// </summary>
    public bool Deleted { get; private init; }
    /// <summary>
    /// The total number of tinybars that are proxy staked to this account.
    /// </summary>
    public long ProxiedToAccount { get; private init; }
    /// <summary>
    /// Address's Public Key (typically a single Ed25519 key).
    /// </summary>
    public Endorsement Endorsement { get; private init; }
    /// <summary>
    /// Address Balance in Tinybars
    /// </summary>
    public ulong Balance { get; private init; }
    /// <summary>
    /// Balances of tokens and NFTs associated with this account.
    /// </summary>
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
    /// be implicitly assoicated with (by means of being made a treasury
    /// or other related actions).
    /// </summary>
    public int AutoAssociationLimit { get; private init; }
    /// <summary>
    /// The alternate identifier associated with this account that is
    /// in the form of a public key.  If an alternate identifer for this
    /// account does not exist, this value will be <code>None</code>.
    /// </summary>
    public Endorsement KeyAlias { get; private init; }
    /// <summary>
    /// Identification of the Ledger (Network) this 
    /// account information was retrieved from.
    /// </summary>
    public BigInteger Ledger { get; private init; }
    /// <summary>
    /// List of crypto delegate allowances 
    /// allocated  by this account.
    /// </summary>
    public IReadOnlyList<CryptoAllowance> CryptoAllowances { get; private init; }
    /// <summary>
    /// List of token delegate allowances 
    /// allocated  by this account.
    /// </summary>
    public IReadOnlyList<TokenAllowance> TokenAllowances { get; private init; }
    /// <summary>
    /// List of Nft delegate allowances 
    /// allocated  by this account.
    /// </summary>
    public IReadOnlyList<NftAllowance> NftAllowances { get; private init; }
    /// <summary>
    /// Internal Constructor from Raw Response
    /// </summary>
    internal AccountDetail(Response response)
    {
        var info = response.AccountDetails.AccountDetails;
        var address = Address = info.AccountId.AsAddress();
        EvmAddress = EvmAddress.TryParse(info.ContractAccountId, out var evmAddress) ? evmAddress : EvmAddress.None;
        Deleted = info.Deleted;
        ProxiedToAccount = info.ProxyReceived;
        Endorsement = info.Key.ToEndorsement();
        Balance = info.Balance;
        Tokens = info.TokenRelationships.ToBalances();
        ReceiveSignatureRequired = info.ReceiverSigRequired;
        AutoRenewPeriod = info.AutoRenewPeriod.ToTimeSpan();
        Expiration = info.ExpirationTime.ToConsensusTimeStamp();
        Memo = info.Memo;
        NftCount = info.OwnedNfts;
        AutoAssociationLimit = info.MaxAutomaticTokenAssociations;
        KeyAlias = info.Alias is { IsEmpty: false } ? Key.Parser.ParseFrom(info.Alias).ToEndorsement() : Endorsement.None;
        Ledger = new BigInteger(info.LedgerId.Span, true, true);
        CryptoAllowances = info.GrantedCryptoAllowances is { Count: > 0 } ? info.GrantedCryptoAllowances.Select(a => new CryptoAllowance(a, address)).ToArray() : [];
        TokenAllowances = info.GrantedTokenAllowances is { Count: > 0 } ? info.GrantedTokenAllowances.Select(a => new TokenAllowance(a, address)).ToArray() : [];
        NftAllowances = info.GrantedNftAllowances is { Count: > 0 } ? info.GrantedNftAllowances.Select(a => new NftAllowance(a, address)).ToArray() : [];
    }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AccountDetailExtensions
{
    /// <summary>
    /// Retrieves all details regarding a Hedera Network Address.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="address">
    /// The Hedera account to retrieve details of.
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
    public static async Task<AccountDetail> GetAccountDetailAsync(this ConsensusClient client, EntityId address, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        return new AccountDetail(await client.ExecuteQueryAsync(new GetAccountDetailsQuery { AccountId = new AccountID(address) }, cancellationToken, configure).ConfigureAwait(false));
    }
}