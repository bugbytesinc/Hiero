using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Encoded Params data 64 base encoded string JSON Converter.
/// </summary>
public sealed class EncodedParamsConverter : JsonConverter<EncodedParams>
{
    /// <inheritdoc />
    public override EncodedParams Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var valueInHex = reader.GetString();
        if (!string.IsNullOrWhiteSpace(valueInHex))
        {
            try
            {
                if (valueInHex.StartsWith("0x"))
                {
                    valueInHex = valueInHex[2..];
                }
                return new EncodedParams(Hex.ToBytes(valueInHex));
            }
            catch
            {
                // Punt.
            }
        }
        return new EncodedParams(ReadOnlyMemory<byte>.Empty);
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EncodedParams encodedParams, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"0x{Hex.FromBytes(encodedParams.Data)}");
    }
}
