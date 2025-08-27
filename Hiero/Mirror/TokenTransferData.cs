using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;

/// <summary>
/// Represents a token transfer within a transaction.
/// </summary>
public class TokenTransferData
{
    /// <summary>
    /// The identifier of the token transferred.
    /// </summary>
    [JsonPropertyName("token_id")]
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// The account sending or receiving the token.
    /// </summary>
    [JsonPropertyName("account")]
    public EntityId Account { get; set; } = default!;
    /// <summary>
    /// The amount of token transferred (positive
    /// value means the account received token, 
    /// negative value means the account sent the token)
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Amount { get; set; }
    /// <summary>
    /// Flag indiciating this transfer was performed
    /// as an allowed transfer by a third party account.
    /// </summary>
    [JsonPropertyName("is_approval")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool IsAllowance { get; set; }
}
