// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation.Parsing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Reads an <see cref="int"/> from a JSON number or numeric string (a mirror node
/// quirk), treating <c>null</c> or unparseable input as zero.
/// </summary>
public sealed class IntMirrorConverter : JsonConverter<int>
{
    /// <inheritdoc />
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => 0,
            JsonTokenType.Number => reader.TryGetInt32(out int result) ? result : 0,
            JsonTokenType.String => NumericMirrorStringParser.TryGetInt32(ref reader, out int result) ? result : 0,
            _ => 0
        };
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        // Yes this could be lossy
        writer.WriteNumberValue(value);
    }
}
