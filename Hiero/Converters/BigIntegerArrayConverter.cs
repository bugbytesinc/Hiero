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
        var list = new List<BigInteger>();
        while (reader.TokenType != JsonTokenType.EndArray)
        {
            list.Add(_bigIntegerConverter.Read(ref reader, typeof(BigInteger), options!));
            reader.Read();
        }
        return list.ToArray();
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