// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation.Parsing;
using Hiero.Implementation.Formatting;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Converts a <see cref="ConsensusTimeStamp"/> to and from its canonical
/// dotted <c>seconds.nanos</c> JSON string form.
/// </summary>
public sealed class ConsensusTimeStampConverter : JsonConverter<ConsensusTimeStamp>
{
    /// <inheritdoc />
    public override ConsensusTimeStamp Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            return ConsensusTimeStamp.MinValue;
        }
        if (reader.HasValueSequence || reader.ValueIsEscaped)
        {
            return ConsensusTimeStampParser.TryParse(reader.GetString(), out var timeStamp)
                ? timeStamp
                : ConsensusTimeStamp.MinValue;
        }
        return ConsensusTimeStampParser.TryParse(reader.ValueSpan, out var spanTimeStamp)
            ? spanTimeStamp
            : ConsensusTimeStamp.MinValue;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ConsensusTimeStamp timeStamp, JsonSerializerOptions options)
    {
        Span<char> buffer = stackalloc char[64];
        if (ConsensusTimeStampFormatter.TryFormat(timeStamp, buffer, out var charsWritten))
        {
            writer.WriteStringValue(buffer[..charsWritten]);
            return;
        }
        writer.WriteStringValue(timeStamp.ToString());
    }
}
