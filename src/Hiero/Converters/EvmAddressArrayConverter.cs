// SPDX-License-Identifier: Apache-2.0
using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// EVM Address Array JSON Converter
/// </summary>
public sealed class EvmAddressArrayConverter : JsonConverter<EvmAddress[]>
{
    private static readonly EvmAddressConverter _converter = new();

    /// <inheritdoc />
    public override EvmAddress[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

        var values = ArrayPool<EvmAddress>.Shared.Rent(4);
        var count = 0;
        try
        {
            while (reader.TokenType != JsonTokenType.EndArray)
            {
                if (count == values.Length)
                {
                    var next = ArrayPool<EvmAddress>.Shared.Rent(values.Length * 2);
                    Array.Copy(values, next, values.Length);
                    ArrayPool<EvmAddress>.Shared.Return(values, clearArray: true);
                    values = next;
                }
                values[count++] = _converter.Read(ref reader, typeof(EvmAddress), options!);
                reader.Read();
            }
            var result = new EvmAddress[count];
            Array.Copy(values, result, count);
            return result;
        }
        finally
        {
            ArrayPool<EvmAddress>.Shared.Return(values, clearArray: true);
        }
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EvmAddress[] addresses, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var address in addresses)
        {
            _converter.Write(writer, address, options);
        }
        writer.WriteEndArray();
    }
}
