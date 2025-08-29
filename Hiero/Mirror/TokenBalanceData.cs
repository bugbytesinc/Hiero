using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;

/// <summary>
/// Represents a token balance entry for a given account
/// </summary>
public class TokenBalanceData
{
    /// <summary>
    /// The address of the token.
    /// </summary>
    [JsonPropertyName("token_id")]
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// The balance of account’s holdings of token in tinytokens.
    /// </summary>

    [JsonPropertyName("balance")]
    public long Balance { get; set; }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TokenBalanceDataExtensions
{
    /// <summary>
    /// Retrieves the token balance for an contract and given token.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="account">
    /// The contract ID.
    /// </param>
    /// <param name="token">
    /// The token ID
    /// </param>
    /// <param name="filters">
    /// Additional query filters if desired.
    /// </param>
    /// <returns>
    /// The amount of token held by the 
    /// target contract, or null if the
    /// token has not been associated.
    /// </returns>
    public static async Task<long?> GetAccountTokenBalanceAsync(this MirrorRestClient client, EntityId account, EntityId token, params IMirrorQueryFilter[] filters)
    {
        var path = GenerateInitialPath($"accounts/{MirrorFormat(account)}/tokens", [new TokenIsFilter(token), .. filters]);
        var payload = await client.GetSingleItemAsync<TokenHoldingDataPage>(path).ConfigureAwait(false);
        return payload?.TokenHoldings?.FirstOrDefault(r => r.Token == token)?.Balance;
    }
}