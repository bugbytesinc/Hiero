// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation.Parsing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Reads a <see cref="long"/> from a JSON number or numeric string (a mirror node
/// quirk), treating <c>null</c> or unparseable input as zero.
/// </summary>
public sealed class LongMirrorConverter : JsonConverter<long>
{
    /// <inheritdoc />
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => 0,
            JsonTokenType.Number => reader.TryGetInt64(out long result) ? result : 0,
            JsonTokenType.String => NumericMirrorStringParser.TryGetInt64(ref reader, out long result) ? result : 0,
            _ => 0
        };
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        // Yes this could be lossy
        writer.WriteNumberValue(value);
    }
}
