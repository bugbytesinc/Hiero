// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Converts a JSON object whose values are hex-encoded byte strings
/// into a <see cref="Dictionary{TKey, TValue}"/> keyed by the raw
/// property name, with the values decoded to <see cref="ReadOnlyMemory{T}"/>
/// of bytes. Keys are preserved as their on-the-wire string form
/// (hex values for EVM storage keys are conventionally displayed
/// as hex, so this avoids a lossy round-trip through decoded bytes).
/// </summary>
public sealed class HexStringDictionaryToBytesConverter : JsonConverter<Dictionary<string, ReadOnlyMemory<byte>>>
{
    private static readonly HexStringToBytesConverter _hexConverter = new();

    /// <inheritdoc />
    public override Dictionary<string, ReadOnlyMemory<byte>> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        var result = new Dictionary<string, ReadOnlyMemory<byte>>();
        reader.Read();
        while (reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }
            var key = reader.GetString() ?? throw new JsonException();
            reader.Read();
            result[key] = _hexConverter.Read(ref reader, typeof(ReadOnlyMemory<byte>), options);
            reader.Read();
        }
        return result;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Dictionary<string, ReadOnlyMemory<byte>> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var kvp in value)
        {
            writer.WritePropertyName(kvp.Key);
            _hexConverter.Write(writer, kvp.Value, options);
        }
        writer.WriteEndObject();
    }
}
