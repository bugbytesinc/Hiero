using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Address EvmAddress Array JSON Converter
/// </summary>
public sealed class EvmAddressArrayConverter : JsonConverter<EvmAddress[]>
{
    /// <inheritdoc />
    public override EvmAddress[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }
        reader.Read();
        var converter = new EvmAddressConverter();
        var list = new List<EvmAddress>();
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            list.Add(converter.Read(ref reader, typeof(EvmAddress), options!));
            reader.Read();
        }
        return list.ToArray();
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EvmAddress[] monikers, JsonSerializerOptions options)
    {
        var converter = new EvmAddressConverter();
        writer.WriteStartArray();
        foreach (var moniker in monikers)
        {
            converter.Write(writer, moniker, options);
        }
        writer.WriteEndArray();
    }
}