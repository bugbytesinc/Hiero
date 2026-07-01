// SPDX-License-Identifier: Apache-2.0
using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Converts a JSON array of hexadecimal strings to an array of <see cref="ReadOnlyMemory{T}"/> byte buffers and back.
/// </summary>
public sealed class HexStringArraytoBytesArrayConverter : JsonConverter<ReadOnlyMemory<byte>[]>
{
    /// <summary>
    /// Converter for converting hex strings to bytes.
    /// </summary>
    private static readonly HexStringToBytesConverter _hexStringToBytesConverter = new();
    /// <inheritdoc />
    public override ReadOnlyMemory<byte>[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

        var values = ArrayPool<ReadOnlyMemory<byte>>.Shared.Rent(4);
        var count = 0;
        try
        {
            while (reader.TokenType != JsonTokenType.EndArray)
            {
                if (count == values.Length)
                {
                    var next = ArrayPool<ReadOnlyMemory<byte>>.Shared.Rent(values.Length * 2);
                    Array.Copy(values, next, values.Length);
                    ArrayPool<ReadOnlyMemory<byte>>.Shared.Return(values, clearArray: true);
                    values = next;
                }
                values[count++] = _hexStringToBytesConverter.Read(ref reader, typeof(ReadOnlyMemory<byte>), options!);
                reader.Read();
            }
            var result = new ReadOnlyMemory<byte>[count];
            Array.Copy(values, result, count);
            return result;
        }
        finally
        {
            ArrayPool<ReadOnlyMemory<byte>>.Shared.Return(values, clearArray: true);
        }
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
