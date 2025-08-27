using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using System.Collections.Generic;
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
[EditorBrowsable(EditorBrowsableState.Never)]
public static class Extensions
{

    /// <summary>
    /// Retrieves the list of token holdings for this contract, which includes
    /// both fungible tokens and NFTs.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="account">
    /// The contract to retrieve the token holdings.
    /// </param>
    /// <param name="filters">
    /// Additional query filters if desired.
    /// </param>
    /// <returns>
    /// An async enumerable of the native token holdings given the constraints.
    /// </returns>
    public static IAsyncEnumerable<TokenHoldingData> GetAccountTokenHoldingsAsync(this MirrorRestClient client, EntityId account, params IMirrorQueryFilter[] filters)
    {
        var path = GenerateInitialPath($"accounts/{MirrorFormat(account)}/tokens", [new LimitFilter(100), .. filters]);
        return client.GetPagedItemsAsync<TokenHoldingDataPage, TokenHoldingData>(path);
    }
}