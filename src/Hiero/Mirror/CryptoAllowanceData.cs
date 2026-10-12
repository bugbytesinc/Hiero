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
    /// ID of the account allowed to spend hBar from the
    /// owner's account.
    /// </summary>
    [JsonPropertyName("spender")]
    public EntityId Spender { get; set; } = default!;
    /// <summary>
    /// The amount of hBar (in tinybars) the allowed spender
    /// was originally granted.
    /// </summary>
    [JsonPropertyName("amount_granted")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long AmountGranted { get; set; }
    /// <summary>
    /// The remaining amount of hBar (in tinybars) the allowed
    /// spender may spend from the owner account.
    /// </summary>
    /// <remarks>
    /// Before 3.0.0 this property held the amount originally granted,
    /// now <see cref="AmountGranted"/>; it now matches
    /// <see cref="TokenAllowanceData.Amount"/>.
    /// </remarks>
    [JsonPropertyName("amount")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Amount { get; set; }
    /// <summary>
    /// The consensus-timestamp range during which this
    /// allowance record is valid.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public TimestampRangeData? Timestamp { get; set; }
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
    /// <param name="cancellationToken">
    /// Token that cancels the enumeration, including a page request in
    /// flight. <c>WithCancellation</c> on the result works as well.
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
    public static IAsyncEnumerable<CryptoAllowanceData> GetAccountCryptoAllowancesAsync(this MirrorRestClient client, EntityId account, CancellationToken cancellationToken, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"accounts/{account.ToMirrorString()}/allowances/crypto", new PageLimit(100), filters);
        return client.GetPagedItemsAsync<CryptoAllowanceDataPage, CryptoAllowanceData>(path, MirrorJsonContext.Default.CryptoAllowanceDataPage, cancellationToken);
    }
    /// <inheritdoc cref="GetAccountCryptoAllowancesAsync(MirrorRestClient, EntityId, CancellationToken, IMirrorQueryParameter[])"/>
    public static IAsyncEnumerable<CryptoAllowanceData> GetAccountCryptoAllowancesAsync(this MirrorRestClient client, EntityId account, params IMirrorQueryParameter[] filters)
    {
        return client.GetAccountCryptoAllowancesAsync(account, CancellationToken.None, filters);
    }
}
