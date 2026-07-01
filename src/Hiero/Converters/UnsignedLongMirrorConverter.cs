// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation.Parsing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Reads a <see cref="ulong"/> from a JSON number or numeric string (a mirror node
/// quirk), treating <c>null</c> or unparseable input as zero.
/// </summary>
public sealed class UnsignedLongMirrorConverter : JsonConverter<ulong>
{
    /// <inheritdoc />
    public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => 0,
            JsonTokenType.Number => reader.TryGetUInt64(out ulong result) ? result : 0,
            JsonTokenType.String => NumericMirrorStringParser.TryGetUInt64(ref reader, out ulong result) ? result : 0,
            _ => 0
        };
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
    {
        // Yes this could be lossy
        writer.WriteNumberValue(value);
    }
}
