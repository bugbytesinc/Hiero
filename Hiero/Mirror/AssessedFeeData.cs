using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;

/// <summary>
/// Represents custom assessed fees imposed as a result
/// of a token transfer.
/// </summary>
public class AssessedFeeData
{
    /// <summary>
    /// The amount of token or crypto assessed
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Amount { get; set; }
    /// <summary>
    /// The account receiving the token or crypto fee.
    /// </summary>
    [JsonPropertyName("collector_account_id")]
    public EntityId Collector { get; set; } = default!;
    /// <summary>
    /// The accounts paying the token or crypto fee.
    /// </summary>
    [JsonPropertyName("effective_payer_account_ids")]
    [JsonConverter(typeof(EntityIdArrayConverter))]
    public EntityId[] Payers { get; set; } = default!;
    /// <summary>
    /// Payer of the token transferred, or None
    /// for tinybars.
    /// </summary>
    [JsonPropertyName("token_id")]
    public EntityId Token { get; set; } = default!;
}