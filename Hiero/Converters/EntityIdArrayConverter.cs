using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Address Payer Array JSON Converter
/// </summary>
public sealed class EntityIdArrayConverter : JsonConverter<EntityId[]>
{
    /// <summary>
    /// Converter for EntityId, used to convert each individual EntityId in the array.
    /// </summary>
    private static EntityIdConverter _entityIdConverter = new();
    /// <inheritdoc />
    public override EntityId[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }
        reader.Read();

        var list = new List<EntityId>();
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            list.Add(_entityIdConverter.Read(ref reader, typeof(EntityId), options!));
            reader.Read();
        }
        return list.ToArray();
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EntityId[] addresses, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var address in addresses)
        {
            _entityIdConverter.Write(writer, address, options);
        }
        writer.WriteEndArray();
    }
}