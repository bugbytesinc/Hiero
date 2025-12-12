using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;

/// <summary>
/// Represents an hBar transfer within a transaction.
/// </summary>
public class CryptoTransferData
{
    /// <summary>
    /// The account sending or receiving hBar.
    /// </summary>
    [JsonPropertyName("account")]
    public EntityId Account { get; set; } = default!;
    /// <summary>
    /// The amount tinybars transferred (a positive
    /// value means the account received tinybars, 
    /// negative value means the account sent tinybars)
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Amount { get; set; }
    /// <summary>
    /// Flag indicating this transfer was performed
    /// as an allowed transfer by a third party account.
    /// </summary>
    [JsonPropertyName("is_approval")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool IsAllowance { get; set; }
}