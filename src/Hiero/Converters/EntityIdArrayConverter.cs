// SPDX-License-Identifier: Apache-2.0
using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Entity ID Array JSON Converter
/// </summary>
public sealed class EntityIdArrayConverter : JsonConverter<EntityId[]>
{
    /// <summary>
    /// Converter for EntityId, used to convert each individual EntityId in the array.
    /// </summary>
    private static readonly EntityIdConverter _entityIdConverter = new();
    /// <inheritdoc />
    public override EntityId[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }
        reader.Read();
        if (reader.TokenType == JsonTokenType.EndArray)
        {
            return [];
        }

        var values = ArrayPool<EntityId>.Shared.Rent(4);
        var count = 0;
        try
        {
            while (reader.TokenType != JsonTokenType.EndArray)
            {
                if (count == values.Length)
                {
                    var next = ArrayPool<EntityId>.Shared.Rent(values.Length * 2);
                    Array.Copy(values, next, values.Length);
                    ArrayPool<EntityId>.Shared.Return(values, clearArray: true);
                    values = next;
                }
                values[count++] = _entityIdConverter.Read(ref reader, typeof(EntityId), options!);
                reader.Read();
            }
            var result = new EntityId[count];
            Array.Copy(values, result, count);
            return result;
        }
        finally
        {
            ArrayPool<EntityId>.Shared.Return(values, clearArray: true);
        }
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
