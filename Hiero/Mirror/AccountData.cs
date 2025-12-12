using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// Address information retrieved from a mirror node.
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
    /// Structure enumerating the account balance and the
    /// first 100 token balances for the account.
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
    /// if there are no funds to extends its lifetime.
    /// </summary>
    [JsonPropertyName("expiry_timestamp")]
    public ConsensusTimeStamp Expiration { get; set; }
    /// <summary>
    /// The public endorsements required by this account.
    /// </summary>
    [JsonPropertyName("key")]
    public Endorsement Endorsement { get; set; } = default!;
    /// <summary>
    /// The number of auto-associations for this account.
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
    /// to a node, then the value is null
    /// </summary>
    [JsonPropertyName("stake_period_start")]
    public ConsensusTimeStamp StakePeriodStart { get; set; }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AccountDataExtensions
{
    /// <summary>
    /// Retrieves information about an contract.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="account">
    /// The id of the contract to retrieve.
    /// </param>
    /// <returns>
    /// An Address information object or throws an exception if not found.
    /// </returns>
    public static Task<AccountData?> GetAccountAsync(this MirrorRestClient client, EntityId account, params IMirrorQueryFilter[] filters)
    {
        var path = GenerateInitialPath($"accounts/{MirrorFormat(account)}", filters);
        return client.GetSingleItemAsync<AccountData>(path);
    }
    /// <summary>
    /// Returns a list of accounts matching the given public key endorsement value.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="endorsement">
    /// The endorsement to match against.
    /// </param>
    /// <returns>
    /// Array of contract information objects with public keys matching the endorsement,
    /// or empty if no matches are found.
    /// </returns>
    public static IAsyncEnumerable<AccountData> GetAccountsFromEndorsementAsync(this MirrorRestClient client, Endorsement endorsement)
    {
        var searchKey = Hex.FromBytes(endorsement.ToBytes(KeyFormat.Mirror));
        return client.GetPagedItemsAsync<AccountDataPage, AccountData>($"accounts?account.publickey={searchKey}&balance=true&limit=20&order=asc");
    }
}