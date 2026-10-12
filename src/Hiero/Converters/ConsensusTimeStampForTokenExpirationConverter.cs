// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Converts a <see cref="ConsensusTimeStamp"/> to and from a numeric JSON value of
/// nanoseconds since the epoch. Specific to the expiry timestamp returned in token info,
/// which is reported in whole nanoseconds rather than the dotted seconds.nanos string used elsewhere.
/// </summary>
public sealed class ConsensusTimeStampForTokenExpirationConverter : JsonConverter<ConsensusTimeStamp>
{
    /// <inheritdoc />
    public override ConsensusTimeStamp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            // The mirror node reports a token without one as null.
            return ConsensusTimeStamp.MinValue;
        }
        return new ConsensusTimeStamp(reader.GetDecimal() / 1000000000m);
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ConsensusTimeStamp timeStamp, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(timeStamp.Seconds * 1000000000m);
    }
}
