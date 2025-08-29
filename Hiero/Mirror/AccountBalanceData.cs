using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// Represents a token balance entry for an account and token.
/// </summary>
public class AccountBalanceData
{
    /// <summary>
    /// The account holding the token.
    /// </summary>
    [JsonPropertyName("account")]
    [JsonConverter(typeof(EntityIdConverter))]
    public EntityId Account { get; set; } = default!;
    /// <summary>
    /// The balance of account’s holdings of token in tinytokens.
    /// </summary>

    [JsonPropertyName("balance")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Balance { get; set; }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AccountBalanceDataExtensions
{
    /// <summary>
    /// Retreives the balances for a given token and filtering criteria.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="token">
    /// The Token ID to retrieve.
    /// </param>
    /// <param name="filters">
    /// Additional query filters if desired.
    /// </param>
    /// <returns>
    /// An enumerable of balance and amount pairs, including possibly zero
    /// balance values indicating a token association without a balance.
    /// </returns>
    public static IAsyncEnumerable<AccountBalanceData> GetTokenBalancesAsync(this MirrorRestClient client, EntityId token, params IMirrorQueryFilter[] filters)
    {
        var path = GenerateInitialPath($"tokens/{token}/balances", [new LimitFilter(100), .. filters]);
        return client.GetPagedItemsAsync<AccountBalancePage, AccountBalanceData>(path);
    }
}