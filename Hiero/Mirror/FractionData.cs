using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;

/// <summary>
/// Represents a fraction.
/// </summary>
public class FractionData
{
    /// <summary>
    /// Numerator
    /// </summary>
    [JsonPropertyName("numerator")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Numerator { get; set; }
    /// <summary>
    /// Denominator
    /// </summary>
    [JsonPropertyName("denominator")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Denominator { get; set; }
}
