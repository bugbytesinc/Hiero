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
/// Lightweight token summary returned by the
/// <c>/api/v1/tokens</c> list endpoint. Intentionally a
/// reduced-field type — the mirror node omits the full HAPI
/// <c>TokenInfo</c> configuration when listing, and modeling it
/// as a smaller class avoids the common trap of callers
/// expecting full-info fields (supply/pause/wipe keys,
/// auto-renew settings, fees, etc.) to be present.
/// </summary>
/// <remarks>
/// When full configuration is required for a single token, use
/// <see cref="TokenData"/> via the
/// <see cref="TokenDataExtensions.GetTokenAsync(MirrorRestClient, EntityId, IMirrorQueryParameter[])"/>
/// method instead.
/// </remarks>
public class TokenSummaryData
{
    /// <summary>
    /// The admin key granting operational authority over the
    /// token. Null when the token has no admin key (immutable
    /// token).
    /// </summary>
    [JsonPropertyName("admin_key")]
    public Endorsement? Administrator { get; set; }
    /// <summary>
    /// Number of decimal places for fungible tokens; zero for
    /// non-fungible tokens.
    /// </summary>
    [JsonPropertyName("decimals")]
    [JsonConverter(typeof(IntMirrorConverter))]
    public int Decimals { get; set; }
    /// <summary>
    /// Arbitrary binary metadata attached to the token class
    /// (per HIP-657). Empty when the token carries no metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonConverter(typeof(Base64StringToBytesConverter))]
    public ReadOnlyMemory<byte> Metadata { get; set; }
    /// <summary>
    /// Human-readable token name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;
    /// <summary>
    /// Short token symbol.
    /// </summary>
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = default!;
    /// <summary>
    /// The Hiero token entity id.
    /// </summary>
    [JsonPropertyName("token_id")]
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// Token type classification: fungible or non-fungible.
    /// </summary>
    [JsonPropertyName("type")]
    public TokenType Type { get; set; }
}
/// <summary>
/// Extension methods for listing token summaries from the
/// mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TokenSummaryDataExtensions
{
    /// <summary>
    /// Enumerates token summaries across the network. This is a
    /// broad list/search surface — pair with
    /// <see cref="TokenNameFilter"/>,
    /// <see cref="PublicKeyFilter"/>,
    /// <see cref="TokenTypeFilter"/>,
    /// <see cref="AccountFilter"/>, or
    /// <see cref="TokenFilter"/> to narrow the result set.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="filters">
    /// Additional query filters. The endpoint supports
    /// <see cref="AccountFilter"/>, <see cref="TokenFilter"/>,
    /// <see cref="TokenNameFilter"/>,
    /// <see cref="PublicKeyFilter"/>,
    /// <see cref="TokenTypeFilter"/>, <see cref="PageLimit"/>,
    /// and <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of token-summary records.
    /// </returns>
    public static IAsyncEnumerable<TokenSummaryData> GetTokensAsync(this MirrorRestClient client, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath("tokens", [new PageLimit(100), .. filters]);
        return client.GetPagedItemsAsync<TokenSummaryDataPage, TokenSummaryData>(path, MirrorJsonContext.Default.TokenSummaryDataPage);
    }
}
