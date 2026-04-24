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
/// Represents a detailed holding of a token by an account.
/// </summary>
public class TokenHoldingData
{
    /// <summary>
    /// The address of the token.
    /// </summary>
    [JsonPropertyName("token_id")]
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// Was this token holding a result of an 
    /// automatic association.
    /// </summary>
    [JsonPropertyName("automatic_association")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool AutoAssociated { get; set; }
    /// <summary>
    /// The balance of account’s holdings of token in tinytokens.
    /// </summary>
    [JsonPropertyName("balance")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Balance { get; set; }
    /// <summary>
    /// The date when this holding was established.
    /// </summary>
    [JsonPropertyName("created_timestamp")]
    public ConsensusTimeStamp Created { get; set; }
    /// <summary>
    /// Status of the token related to freezing (if applicable)
    /// </summary>
    [JsonPropertyName("freeze_status")]
    [JsonConverter(typeof(FreezeStatusConverter))]
    public TokenTradableStatus FreezeStatus { get; set; } = default!;
    /// <summary>
    /// Status of the KYC status of the holding (if applicable)
    /// </summary>
    [JsonPropertyName("kyc_status")]
    [JsonConverter(typeof(TokenKycStatusConverter))]
    public TokenKycStatus KycStatus { get; set; } = default!;
}
/// <summary>
/// Extension methods for querying token holding data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class Extensions
{

    /// <summary>
    /// Enumerates the token-association records for a specific account
    /// from <c>/api/v1/accounts/{id}/tokens</c>, including both
    /// fungible tokens and NFT classes. Use <see cref="TokenFilter"/>
    /// to narrow to a specific token.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="account">
    /// The account to retrieve the token holdings.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters. The endpoint supports
    /// <see cref="TokenFilter"/>, <see cref="PageLimit"/>, and
    /// <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of the account's token holdings.
    /// </returns>
    public static IAsyncEnumerable<TokenHoldingData> GetAccountTokenHoldingsAsync(this MirrorRestClient client, EntityId account, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"accounts/{MirrorFormat(account)}/tokens", [new PageLimit(100), .. filters]);
        return client.GetPagedItemsAsync<TokenHoldingDataPage, TokenHoldingData>(path, MirrorJsonContext.Default.TokenHoldingDataPage);
    }
}