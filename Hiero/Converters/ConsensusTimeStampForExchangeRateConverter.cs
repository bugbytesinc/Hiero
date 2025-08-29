using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Consensus Timestamp JSON Converter (from long value) that is special
/// for the EXCHANGE RATE data, becaue it is a different format from the other 
/// timestamp formats.
/// </summary>
public sealed class ConsensusTimeStampForExchangeRateConverter : JsonConverter<ConsensusTimeStamp>
{
    /// <inheritdoc />
    public override ConsensusTimeStamp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new ConsensusTimeStamp(reader.GetDecimal());
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ConsensusTimeStamp timeStamp, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(timeStamp.Seconds);
    }
}
