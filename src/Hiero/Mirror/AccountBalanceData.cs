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
/// <summary>
/// Extension methods for querying account balance data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AccountBalanceDataExtensions
{
    /// <summary>
    /// Retrieves a snapshot of token holders for a given token at (or just
    /// before) the specified consensus timestamp.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This endpoint (<c>/api/v1/tokens/{id}/balances</c>) reads from the
    /// mirror node's balance-file snapshot, which is produced roughly every
    /// fifteen minutes — <b>not</b> from real-time balance state. Results
    /// may therefore lag the ledger by up to that window. For that reason
    /// the <paramref name="asOf"/> timestamp is required: callers must
    /// consciously pick the snapshot instant rather than silently receiving
    /// "whatever the mirror node had last."
    /// </para>
    /// <para>
    /// If you need a live per-account token balance, call
    /// <see cref="TokenBalanceDataExtensions.GetAccountTokenBalanceAsync"/>
    /// instead — that method hits the fresher
    /// <c>/api/v1/accounts/{id}/tokens</c> surface.
    /// </para>
    /// </remarks>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="token">
    /// The Token ID whose holder snapshot is being fetched.
    /// </param>
    /// <param name="asOf">
    /// The consensus timestamp defining the snapshot instant. Translated
    /// internally to the <c>timestamp=lte:{asOf}</c> query parameter.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters (paging, account/key predicates) to
    /// refine the request. Do not pass another <c>timestamp</c> filter —
    /// <paramref name="asOf"/> already supplies it.
    /// </param>
    /// <returns>
    /// An enumerable of account/balance pairs from the chosen snapshot,
    /// including possibly zero balance values indicating a token
    /// association without a balance.
    /// </returns>
    public static IAsyncEnumerable<AccountBalanceData> GetTokenHoldersSnapshotAsync(this MirrorRestClient client, EntityId token, ConsensusTimeStamp asOf, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"tokens/{token}/balances", [new PageLimit(100), TimestampFilter.OnOrBefore(asOf), .. filters]);
        return client.GetPagedItemsAsync<AccountBalanceDataPage, AccountBalanceData>(path, MirrorJsonContext.Default.AccountBalanceDataPage);
    }
}