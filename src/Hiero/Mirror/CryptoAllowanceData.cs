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
public class CryptoAllowanceData
{
    /// <summary>
    /// ID of the allowance owner.
    /// </summary>
    [JsonPropertyName("owner")]
    public EntityId Owner { get; set; } = default!;
    /// <summary>
    /// ID of the account allowed to spend the token.
    /// </summary>
    [JsonPropertyName("spender")]
    public EntityId Spender { get; set; } = default!;
    /// <summary>
    /// The amount of hBar (in tinybars) the allowed spender may
    /// spend from the owner account (denominated
    /// in smallest denomination)
    /// </summary>
    [JsonPropertyName("amount_granted")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Amount { get; set; }
}
/// <summary>
/// Extension methods for querying crypto allowance data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CryptoAllowanceDataExtensions
{
    /// <summary>
    /// Enumerates HBAR (crypto) allowances granted by a specific
    /// account from <c>/api/v1/accounts/{id}/allowances/crypto</c>.
    /// Use <see cref="SpenderFilter"/> to narrow to a specific
    /// allowance recipient. Newest-first by default; pass
    /// <see cref="OrderBy.Ascending"/> to reverse.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="account">
    /// The account whose crypto-allowance grants are requested.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters. The endpoint supports
    /// <see cref="SpenderFilter"/>, <see cref="PageLimit"/>, and
    /// <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of crypto-allowance records granted by the
    /// given account.
    /// </returns>
    public static IAsyncEnumerable<CryptoAllowanceData> GetAccountCryptoAllowancesAsync(this MirrorRestClient client, EntityId account, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"accounts/{MirrorFormat(account)}/allowances/crypto", [new PageLimit(100), .. filters]);
        return client.GetPagedItemsAsync<CryptoAllowanceDataPage, CryptoAllowanceData>(path, MirrorJsonContext.Default.CryptoAllowanceDataPage);
    }
}