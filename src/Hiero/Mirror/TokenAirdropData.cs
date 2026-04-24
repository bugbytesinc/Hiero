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
/// A single token-airdrop record returned by the outstanding
/// and pending airdrop endpoints
/// (<c>/api/v1/accounts/{id}/airdrops/outstanding</c> and
/// <c>/api/v1/accounts/{id}/airdrops/pending</c>).
/// </summary>
/// <remarks>
/// The schema is symmetric between the two endpoints — the
/// caller's perspective is what changes. From the outstanding
/// endpoint the caller is the <see cref="Sender"/>; from the
/// pending endpoint the caller is the <see cref="Receiver"/>.
/// </remarks>
public class TokenAirdropData
{
    /// <summary>
    /// The amount of the token-airdrop in the token's smallest
    /// denomination. For NFT airdrops this reflects the count of
    /// serials associated with this record.
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Amount { get; set; }
    /// <summary>
    /// The intended recipient of the airdrop.
    /// </summary>
    [JsonPropertyName("receiver_id")]
    public EntityId Receiver { get; set; } = default!;
    /// <summary>
    /// The account that sent the airdrop.
    /// </summary>
    [JsonPropertyName("sender_id")]
    public EntityId Sender { get; set; } = default!;
    /// <summary>
    /// The NFT serial number carried by this airdrop record, or
    /// <c>null</c> for fungible-token airdrops.
    /// </summary>
    [JsonPropertyName("serial_number")]
    [JsonConverter(typeof(NullableLongMirrorConverter))]
    public long? SerialNumber { get; set; }
    /// <summary>
    /// The consensus-timestamp range during which this airdrop
    /// record is valid.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public TimestampRangeData? Timestamp { get; set; }
    /// <summary>
    /// The token class involved in the airdrop.
    /// </summary>
    [JsonPropertyName("token_id")]
    public EntityId Token { get; set; } = default!;
}
/// <summary>
/// Extension methods for querying the token-airdrop endpoints
/// on the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TokenAirdropDataExtensions
{
    /// <summary>
    /// Enumerates airdrops this account <b>has sent</b> that the
    /// intended receivers have not yet claimed. From the caller's
    /// perspective the given <paramref name="account"/> is the
    /// sender of each returned record.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="account">
    /// The account whose outstanding (sent-but-unclaimed) airdrops
    /// are requested.
    /// </param>
    /// <param name="filters">
    /// Additional query filters. The endpoint supports
    /// <see cref="ReceiverFilter"/>, <see cref="TokenFilter"/>,
    /// <see cref="SerialNumberFilter"/>, <see cref="PageLimit"/>,
    /// and <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of outstanding airdrop records.
    /// </returns>
    public static IAsyncEnumerable<TokenAirdropData> GetAccountOutstandingAirdropsAsync(this MirrorRestClient client, EntityId account, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"accounts/{MirrorFormat(account)}/airdrops/outstanding", [new PageLimit(100), .. filters]);
        return client.GetPagedItemsAsync<TokenAirdropDataPage, TokenAirdropData>(path, MirrorJsonContext.Default.TokenAirdropDataPage);
    }
    /// <summary>
    /// Enumerates airdrops this account <b>has received</b> but
    /// not yet claimed (token not yet associated, or recipient
    /// has not accepted). From the caller's perspective the
    /// given <paramref name="account"/> is the receiver of each
    /// returned record.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="account">
    /// The account whose pending (received-but-unclaimed)
    /// airdrops are requested.
    /// </param>
    /// <param name="filters">
    /// Additional query filters. The endpoint supports
    /// <see cref="SenderFilter"/>, <see cref="TokenFilter"/>,
    /// <see cref="SerialNumberFilter"/>, <see cref="PageLimit"/>,
    /// and <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of pending airdrop records.
    /// </returns>
    public static IAsyncEnumerable<TokenAirdropData> GetAccountPendingAirdropsAsync(this MirrorRestClient client, EntityId account, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"accounts/{MirrorFormat(account)}/airdrops/pending", [new PageLimit(100), .. filters]);
        return client.GetPagedItemsAsync<TokenAirdropDataPage, TokenAirdropData>(path, MirrorJsonContext.Default.TokenAirdropDataPage);
    }
}
