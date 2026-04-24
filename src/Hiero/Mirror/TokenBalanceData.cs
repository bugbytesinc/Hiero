// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using Hiero.Mirror.Paging;
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
/// <summary>
/// Extension methods for querying token balance data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TokenBalanceDataExtensions
{
    /// <summary>
    /// Retrieves the current balance of a specific fungible token
    /// held by an account, via
    /// <c>/api/v1/accounts/{id}/tokens?token.id={token}</c>. The
    /// <paramref name="token"/> argument is pinned internally as a
    /// <see cref="TokenFilter"/>; callers do not need to supply one.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="account">
    /// The account whose holding is requested.
    /// </param>
    /// <param name="token">
    /// The token whose balance is requested.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters. The underlying endpoint supports
    /// <see cref="PageLimit"/> and <see cref="OrderBy"/>, though a
    /// single-token lookup rarely benefits from either.
    /// </param>
    /// <returns>
    /// The amount of token held by the target account, or null if the
    /// token has not been associated.
    /// </returns>
    public static async Task<long?> GetAccountTokenBalanceAsync(this MirrorRestClient client, EntityId account, EntityId token, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"accounts/{MirrorFormat(account)}/tokens", [TokenFilter.Is(token), .. filters]);
        var payload = await client.GetSingleItemAsync(path, MirrorJsonContext.Default.TokenHoldingDataPage).ConfigureAwait(false);
        return payload?.TokenHoldings?.FirstOrDefault(r => r.Token == token)?.Balance;
    }
}