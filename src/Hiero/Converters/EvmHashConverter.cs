// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation.Parsing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Converts an <see cref="EvmHash"/> to and from a <c>0x</c>-prefixed hexadecimal JSON string,
/// including as a property name.
/// </summary>
public sealed class EvmHashConverter : JsonConverter<EvmHash>
{
    /// <inheritdoc />
    public override EvmHash Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not (JsonTokenType.String or JsonTokenType.PropertyName))
        {
            return EvmHash.None;
        }
        if (reader.HasValueSequence || reader.ValueIsEscaped)
        {
            var value = reader.GetString();
            return EvmHashParser.TryParse(value, out var hash) ? hash : EvmHash.None;
        }
        return EvmHashParser.TryParse(reader.ValueSpan, out var spanHash) ? spanHash : EvmHash.None;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EvmHash hash, JsonSerializerOptions options)
    {
        Span<char> buffer = stackalloc char[66];
        WriteHashChars(hash, buffer);
        writer.WriteStringValue(buffer);
    }
    /// <inheritdoc />
    public override EvmHash ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Read(ref reader, typeToConvert, options);
    }
    /// <inheritdoc />
    public override void WriteAsPropertyName(Utf8JsonWriter writer, EvmHash value, JsonSerializerOptions options)
    {
        Span<char> buffer = stackalloc char[66];
        WriteHashChars(value, buffer);
        writer.WritePropertyName(buffer);
    }

    private static void WriteHashChars(EvmHash hash, Span<char> destination)
    {
        destination[0] = '0';
        destination[1] = 'x';
        Convert.TryToHexStringLower(hash.Bytes, destination[2..], out _);
    }
}
