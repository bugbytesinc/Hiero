using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Address Payer Array JSON Converter
/// </summary>
public sealed class HexStringArraytoBytesArrayConverter : JsonConverter<ReadOnlyMemory<byte>[]>
{
    /// <summary>
    /// Converter for converting hex strings to bytes.
    /// </summary>
    private static HexStringToBytesConverter _hexStringToBytesConverter = new();
    /// <inheritdoc />
    public override ReadOnlyMemory<byte>[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }
        reader.Read();
        var list = new List<ReadOnlyMemory<byte>>();
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            list.Add(_hexStringToBytesConverter.Read(ref reader, typeof(ReadOnlyMemory<byte>), options!));
            reader.Read();
        }
        return list.ToArray();
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ReadOnlyMemory<byte>[] values, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var address in values)
        {
            _hexStringToBytesConverter.Write(writer, address, options);
        }
        writer.WriteEndArray();
    }
}