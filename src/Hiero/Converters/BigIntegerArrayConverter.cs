// SPDX-License-Identifier: Apache-2.0
using System.Buffers;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Converts a JSON array of strings (representing hex values) to an array of <see cref="BigInteger"/>s and vice versa.
/// </summary>
public sealed class BigIntegerArrayConverter : JsonConverter<BigInteger[]>
{
    /// <summary>
    /// Converter for BigInteger values, used to convert each individual value in the array.
    /// </summary>
    private static readonly BigIntegerConverter _bigIntegerConverter = new();
    /// <inheritdoc />
    public override BigInteger[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected StartArray token for BigInteger array.");
        }
        reader.Read();
        if (reader.TokenType == JsonTokenType.EndArray)
        {
            return [];
        }

        var values = ArrayPool<BigInteger>.Shared.Rent(4);
        var count = 0;
        try
        {
            while (reader.TokenType != JsonTokenType.EndArray)
            {
                if (count == values.Length)
                {
                    var next = ArrayPool<BigInteger>.Shared.Rent(values.Length * 2);
                    Array.Copy(values, next, values.Length);
                    ArrayPool<BigInteger>.Shared.Return(values, clearArray: true);
                    values = next;
                }
                values[count++] = _bigIntegerConverter.Read(ref reader, typeof(BigInteger), options!);
                reader.Read();
            }
            var result = new BigInteger[count];
            Array.Copy(values, result, count);
            return result;
        }
        finally
        {
            ArrayPool<BigInteger>.Shared.Return(values, clearArray: true);
        }
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, BigInteger[] values, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var address in values)
        {
            _bigIntegerConverter.Write(writer, address, options);
        }
        writer.WriteEndArray();
    }
}
