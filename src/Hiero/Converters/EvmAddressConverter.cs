// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation.Parsing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// EVM Address JSON Converter
/// </summary>
public sealed class EvmAddressConverter : JsonConverter<EvmAddress>
{
    /// <inheritdoc />
    public override EvmAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not (JsonTokenType.String or JsonTokenType.PropertyName))
        {
            return EvmAddress.None;
        }
        if (reader.HasValueSequence || reader.ValueIsEscaped)
        {
            var value = reader.GetString();
            return EvmAddressParser.TryParse(value, out var evmAddress) ? evmAddress : EvmAddress.None;
        }
        return EvmAddressParser.TryParse(reader.ValueSpan, out var spanAddress) ? spanAddress : EvmAddress.None;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EvmAddress evmAddress, JsonSerializerOptions options)
    {
        Span<char> buffer = stackalloc char[42];
        if (evmAddress.TryFormat(buffer, out var charsWritten, default, default))
        {
            writer.WriteStringValue(buffer[..charsWritten]);
            return;
        }
        writer.WriteStringValue(evmAddress.ToString());
    }
    /// <inheritdoc />
    public override EvmAddress ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Read(ref reader, typeToConvert, options);
    }
    /// <inheritdoc />
    public override void WriteAsPropertyName(Utf8JsonWriter writer, EvmAddress value, JsonSerializerOptions options)
    {
        Span<char> buffer = stackalloc char[42];
        if (value.TryFormat(buffer, out var charsWritten, default, default))
        {
            writer.WritePropertyName(buffer[..charsWritten]);
            return;
        }
        writer.WritePropertyName(value.ToString());
    }
}
