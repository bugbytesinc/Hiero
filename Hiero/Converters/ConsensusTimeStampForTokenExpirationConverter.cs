using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Consensus Timestamp JSON Converter (from microseconds long value)
/// that is special to ONLY the expiry timestamp returned for token info.
/// </summary>
public sealed class ConsensusTimeStampForTokenExpirationConverter : JsonConverter<ConsensusTimeStamp>
{
    /// <inheritdoc />
    public override ConsensusTimeStamp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new ConsensusTimeStamp(reader.GetDecimal() / 1000000000m);
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ConsensusTimeStamp timeStamp, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(timeStamp.Seconds * 1000000000m);
    }
}
