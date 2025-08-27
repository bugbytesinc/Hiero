using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;

/// <summary>
/// Represents the fixed fees for a token.
/// </summary>
public class FixedFeeData
{
    /// <summary>
    /// Are collecor accounts exempt from paying fees.
    /// </summary>
    [JsonPropertyName("all_collectors_are_exempt")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool CollectorsExempt { get; set; }
    /// <summary>
    /// Amount of fixed fee to collect.
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Amount { get; set; }
    /// <summary>
    /// The account receiving the fees.
    /// </summary>
    [JsonPropertyName("collector_account_id")]
    public EntityId Collector { get; set; } = default!;
    /// <summary>
    /// The token that the fee is denominated
    /// in, or NONE for hBar.
    /// </summary>
    [JsonPropertyName("denominating_token_id")]
    public EntityId FeeToken { get; set; } = default!;
}
