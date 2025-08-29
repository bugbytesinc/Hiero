using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Consensus Timestamp JSON Converter
/// </summary>
public sealed class ConsensusTimeStampConverter : JsonConverter<ConsensusTimeStamp>
{
    /// <inheritdoc />
    public override ConsensusTimeStamp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (decimal.TryParse(reader.GetString(), out decimal epoch))
        {
            return new ConsensusTimeStamp(epoch);
        }
        return ConsensusTimeStamp.MinValue;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ConsensusTimeStamp timeStamp, JsonSerializerOptions options)
    {
        writer.WriteStringValue(timeStamp.ToString());
    }
}
