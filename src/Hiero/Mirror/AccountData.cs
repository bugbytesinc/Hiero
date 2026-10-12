// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using Hiero.Mirror.Paging;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// Account information retrieved from a mirror node.
/// </summary>
public class AccountData
{
    /// <summary>
    /// The ID of the account
    /// </summary>
    [JsonPropertyName("account")]
    public EntityId Account { get; set; } = default!;
    /// <summary>
    /// RFC4648 no-padding base32 encoded account alias,
    /// this appears to be inconsistent in how the mirror
    /// node reports this value, sometimes it is 33 bytes long,
    /// sometimes it is 20 bytes long, user, beware.
    /// </summary>
    [JsonPropertyName("alias")]
    public string Alias { get; set; } = default!;
    /// <summary>
    /// Address Auto-Renew Period in seconds.
    /// </summary>
    [JsonPropertyName("auto_renew_period")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long AutoRenewPeriod { get; set; }
    /// <summary>
    /// Structure enumerating the account balance and a
    /// capped list of token balances for the account (see
    /// <see cref="AccountBalancesData"/>).
    /// </summary>
    [JsonPropertyName("balance")]
    public AccountBalancesData Balances { get; set; } = default!;
    /// <summary>
    /// Consensus Timestamp when this account was created
    /// </summary>
    [JsonPropertyName("created_timestamp")]
    public ConsensusTimeStamp Created { get; set; }
    /// <summary>
    /// Flag indicating that the staking reward is
    /// explicitly declined.
    /// </summary>
    [JsonPropertyName("decline_reward")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool DeclineReward { get; set; }
    /// <summary>
    /// Flag indicating that the account has been deleted.
    /// </summary>
    [JsonPropertyName("deleted")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool Deleted { get; set; }
    /// <summary>
    /// The address an EIP-7702 delegation points the account's code
    /// at; null when the account has no delegation (or the mirror node
    /// does not report one).
    /// </summary>
    [JsonPropertyName("delegation_address")]
    [JsonConverter(typeof(NullableEvmAddressConverter))]
    public EvmAddress? DelegationAddress { get; set; }
    /// <summary>
    /// The account's associated EVM nonce.
    /// </summary>
    [JsonPropertyName("ethereum_nonce")]
    [JsonConverter(typeof(LongMirrorConverter))]

    public long EvmNonce { get; set; }
    /// <summary>
    /// The account's public address encoded
    /// for use with the contract EVM.
    /// </summary>
    [JsonPropertyName("evm_address")]
    public EvmAddress EvmAddress { get; set; } = default!;
    /// <summary>
    /// Timestamp at which the network will try to 
    /// renew the account rent or delete the account
    /// if there are no funds to extend its lifetime.
    /// </summary>
    [JsonPropertyName("expiry_timestamp")]
    public ConsensusTimeStamp Expiration { get; set; }
    /// <summary>
    /// The public endorsements required by this account.
    /// </summary>
    [JsonPropertyName("key")]
    public Endorsement Endorsement { get; set; } = default!;
    /// <summary>
    /// The maximum number of automatic token associations
    /// allowed for this account (-1 means unlimited). This is
    /// the configured limit, not a count of current associations.
    /// </summary>
    [JsonPropertyName("max_automatic_token_associations")]
    [JsonConverter(typeof(IntMirrorConverter))]
    public int Associations { get; set; }
    /// <summary>
    /// The account's memo.
    /// </summary>
    [JsonPropertyName("memo")]
    public string Memo { get; set; } = string.Empty;
    /// <summary>
    /// The pending reward in tinybars the account will receive in 
    /// the next reward payout. Note the value is updated at the 
    /// end of each staking period and there may be delay to 
    /// reflect the changes in the past staking period.
    /// </summary>
    [JsonPropertyName("pending_reward")]
    [JsonConverter(typeof(LongMirrorConverter))]

    public long PendingReward { get; set; }
    /// <summary>
    /// Flag indicating that this account must sign transactions
    /// where this account receives crypto or tokens.
    /// </summary>
    [JsonPropertyName("receiver_sig_required")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool ReceiverSignatureRequired { get; set; }
    /// <summary>
    /// The account to which this account is staking
    /// </summary>
    [JsonPropertyName("staked_account_id")]
    [JsonConverter(typeof(EntityIdConverter))]
    public EntityId StakedAccount { get; set; } = default!;
    /// <summary>
    /// The id of the node to which this account is staking
    /// </summary>
    [JsonPropertyName("staked_node_id")]
    public long? StakedNode { get; set; }
    /// <summary>
    /// The staking period during which either the staking settings 
    /// for this account changed (such as starting staking or 
    /// changing stakedNode) or the most recent reward was earned, 
    /// whichever is later. If this account is not currently staked
    /// to a node, then the value is <see cref="ConsensusTimeStamp.MinValue"/>.
    /// </summary>
    [JsonPropertyName("stake_period_start")]
    public ConsensusTimeStamp StakePeriodStart { get; set; }
}
/// <summary>
/// Extension methods for querying account data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AccountDataExtensions
{
    private sealed class SuppressTransactionsProjection : IMirrorProjection
    {
        public string Name => "transactions";
        public string Value => "false";
    }

    private static readonly SuppressTransactionsProjection SuppressTransactions = new();

    /// <summary>
    /// Retrieves information about a single account from
    /// <c>/api/v1/accounts/{id}</c>. Use <see cref="TimestampFilter"/>
    /// to retrieve the account's state at a historical consensus
    /// instant; otherwise returns the current state. The server's inline
    /// transaction list is suppressed on the wire (bandwidth
    /// optimization, since <see cref="AccountData"/> does not carry it).
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="account">
    /// The id of the account to retrieve.
    /// </param>
    /// <param name="cancellationToken">
    /// Token that cancels the request.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters. The endpoint meaningfully supports
    /// <see cref="TimestampFilter"/> for historical state lookup; other
    /// parameters the server accepts on this endpoint
    /// (<c>limit</c>/<c>order</c>/<c>transactiontype</c>) only affect
    /// the inline transaction list, which this SDK suppresses.
    /// </param>
    /// <returns>
    /// An account information object, or null if not found.
    /// </returns>
    /// <remarks>
    /// For the account's transaction listing, call
    /// <c>GetTransactionsAsync(AccountFilter.Is(account))</c>.
    /// </remarks>
    public static Task<AccountData?> GetAccountAsync(this MirrorRestClient client, EntityId account, CancellationToken cancellationToken, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"accounts/{account.ToMirrorString()}", SuppressTransactions, filters);
        return client.GetSingleItemAsync(path, MirrorJsonContext.Default.AccountData, cancellationToken);
    }
    /// <inheritdoc cref="GetAccountAsync(MirrorRestClient, EntityId, CancellationToken, IMirrorQueryParameter[])"/>
    public static Task<AccountData?> GetAccountAsync(this MirrorRestClient client, EntityId account, params IMirrorQueryParameter[] filters)
    {
        return client.GetAccountAsync(account, CancellationToken.None, filters);
    }
    /// <summary>
    /// Retrieves the hbar balance, in tinybars, of an account or a
    /// contract (contracts are accounts on the mirror node) from
    /// <c>/api/v1/accounts/{id}</c>. Use <see cref="TimestampFilter"/>
    /// to retrieve the balance at a historical consensus instant;
    /// otherwise returns the current balance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This replaces the consensus node balance query, which public
    /// Hedera networks removed in July 2026; consensus nodes now answer
    /// it with <c>BUSY</c>.
    /// </para>
    /// <para>
    /// The mirror node trails consensus by a few seconds. To observe the
    /// effect of a transaction just submitted, wait until the mirror
    /// node's latest consensus timestamp
    /// (<see cref="TransactionTimestampDataExtensions.GetLatestConsensusTimestampAsync"/>)
    /// reaches the transaction's consensus timestamp before reading.
    /// </para>
    /// <para>
    /// For token balances, use
    /// <see cref="TokenBalanceDataExtensions.GetAccountTokenBalanceAsync(MirrorRestClient, EntityId, EntityId, CancellationToken, IMirrorQueryParameter[])"/>
    /// for a single token or
    /// <see cref="TokenHoldingDataExtensions.GetAccountTokenHoldingsAsync(MirrorRestClient, EntityId, CancellationToken, IMirrorQueryParameter[])"/>
    /// for all of an account's holdings.
    /// </para>
    /// </remarks>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="account">
    /// The id of the account or contract.
    /// </param>
    /// <param name="cancellationToken">
    /// Token that cancels the request.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters; <see cref="TimestampFilter"/> selects
    /// a historical balance.
    /// </param>
    /// <returns>
    /// The balance in tinybars, or null if the mirror node does not
    /// know the account or reports no balance for it.
    /// </returns>
    public static async Task<long?> GetAccountBalanceAsync(this MirrorRestClient client, EntityId account, CancellationToken cancellationToken, params IMirrorQueryParameter[] filters)
    {
        var data = await client.GetAccountAsync(account, cancellationToken, filters).ConfigureAwait(false);
        return data?.Balances?.Balance;
    }
    /// <inheritdoc cref="GetAccountBalanceAsync(MirrorRestClient, EntityId, CancellationToken, IMirrorQueryParameter[])"/>
    public static Task<long?> GetAccountBalanceAsync(this MirrorRestClient client, EntityId account, params IMirrorQueryParameter[] filters)
    {
        return client.GetAccountBalanceAsync(account, CancellationToken.None, filters);
    }
    /// <summary>
    /// Enumerates accounts across the network. Use
    /// <see cref="AccountFilter"/> to narrow by id,
    /// <see cref="AccountPublicKeyFilter"/> to narrow by root
    /// public key, <see cref="AccountBalanceFilter"/> to narrow by
    /// tinybar balance threshold, or
    /// <see cref="BalanceProjectionFilter"/> to control whether the
    /// balance subtree is included in each record.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="cancellationToken">
    /// Token that cancels the enumeration, including a page request in
    /// flight. <c>WithCancellation</c> on the result works as well.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters. The endpoint supports
    /// <see cref="AccountFilter"/>, <see cref="AccountPublicKeyFilter"/>,
    /// <see cref="AccountBalanceFilter"/>,
    /// <see cref="BalanceProjectionFilter"/>, <see cref="PageLimit"/>,
    /// and <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of account records.
    /// </returns>
    public static IAsyncEnumerable<AccountData> GetAccountsAsync(this MirrorRestClient client, CancellationToken cancellationToken, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath("accounts", new PageLimit(100), filters);
        return client.GetPagedItemsAsync<AccountDataPage, AccountData>(path, MirrorJsonContext.Default.AccountDataPage, cancellationToken);
    }
    /// <inheritdoc cref="GetAccountsAsync(MirrorRestClient, CancellationToken, IMirrorQueryParameter[])"/>
    public static IAsyncEnumerable<AccountData> GetAccountsAsync(this MirrorRestClient client, params IMirrorQueryParameter[] filters)
    {
        return client.GetAccountsAsync(CancellationToken.None, filters);
    }
}
