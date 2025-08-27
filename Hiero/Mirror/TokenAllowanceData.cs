using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// Allowance information retreived from a mirror node.
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
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TokenAllowanceDataExtensions
{
    /// <summary>
    /// Retrieves the token allowances associated with this contract.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="account">
    /// The contract ID
    /// </param>
    /// <param name="filters">
    /// Additional query filters if desired.
    /// </param>
    /// <returns>
    /// A list of token allowances granted by this contract.
    /// </returns>
    public static IAsyncEnumerable<TokenAllowanceData> GetAccountTokenAllowancesAsync(this MirrorRestClient client, EntityId account, params IMirrorQueryFilter[] filters)
    {
        var path = GenerateInitialPath($"accounts/{MirrorFormat(account)}/allowances/tokens", [new LimitFilter(100), .. filters]);
        return client.GetPagedItemsAsync<TokenAllowanceDataPage, TokenAllowanceData>(path);
    }
}