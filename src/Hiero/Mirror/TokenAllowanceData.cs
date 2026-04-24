// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Paging;
using Hiero.Mirror.Implementation;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// Allowance information retrieved from a mirror node.
/// </summary>
public class TokenAllowanceData
{
    /// <summary>
    /// ID of the token.
    /// </summary>
    [JsonPropertyName("token_id")]
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// ID of the token owner.
    /// </summary>
    [JsonPropertyName("owner")]
    public EntityId Owner { get; set; } = default!;
    /// <summary>
    /// ID of the account allowed to spend the token.
    /// </summary>
    [JsonPropertyName("spender")]
    public EntityId Spender { get; set; } = default!;
    /// <summary>
    /// The amount of token that the allowed spender
    /// was originally granted (denominated
    /// in smallest denomination)
    /// </summary>
    [JsonPropertyName("amount_granted")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long AmountGranted { get; set; }
    /// <summary>
    /// The remaining amount of token the allowed 
    /// spender may spend from the owner account 
    /// (denominated in smallest denomination)
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Amount { get; set; }
}
/// <summary>
/// Extension methods for querying token allowance data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TokenAllowanceDataExtensions
{
    /// <summary>
    /// Enumerates fungible-token allowances granted by a specific
    /// account from <c>/api/v1/accounts/{id}/allowances/tokens</c>.
    /// Use <see cref="SpenderFilter"/> to narrow to a specific
    /// allowance recipient, or <see cref="TokenFilter"/> to narrow to
    /// a specific token. Default order is ascending (spender id,
    /// then token id).
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="account">
    /// The account whose token-allowance grants are requested.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters. The endpoint supports
    /// <see cref="SpenderFilter"/>, <see cref="TokenFilter"/>,
    /// <see cref="PageLimit"/>, and <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of token-allowance records granted by the
    /// given account.
    /// </returns>
    /// <remarks>
    /// The server imposes cross-filter constraints on this endpoint:
    /// <see cref="TokenFilter"/> requires an accompanying
    /// <see cref="SpenderFilter"/> with compatible operator semantics
    /// (<c>lt(e):spender.id + lt(e):token.id</c>, or equivalent
    /// <c>gt(e)</c>/<c>eq</c> pairings). Violating the rule yields a
    /// server-side 400; the SDK does not enforce client-side.
    /// </remarks>
    public static IAsyncEnumerable<TokenAllowanceData> GetAccountTokenAllowancesAsync(this MirrorRestClient client, EntityId account, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"accounts/{MirrorFormat(account)}/allowances/tokens", [new PageLimit(100), .. filters]);
        return client.GetPagedItemsAsync<TokenAllowanceDataPage, TokenAllowanceData>(path, MirrorJsonContext.Default.TokenAllowanceDataPage);
    }
}