// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Converts a <see cref="ConsensusTimeStamp"/> to and from a numeric JSON value of
/// whole seconds. Specific to exchange-rate data, which expresses its expiration as a
/// bare seconds number rather than the dotted seconds.nanos string used elsewhere.
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
