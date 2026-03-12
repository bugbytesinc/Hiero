using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// JSON converter that reads base64-encoded strings and writes them as byte arrays.
/// </summary>
public sealed class Base64StringToBytesConverter : JsonConverter<ReadOnlyMemory<byte>>
{
    /// <inheritdoc />
    public override ReadOnlyMemory<byte> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var valueInBase64 = reader.GetString();
        if (!string.IsNullOrWhiteSpace(valueInBase64))
        {
            try
            {
                return Convert.FromBase64String(valueInBase64);
            }
            catch
            {
                // Punt.
            }
        }
        return ReadOnlyMemory<byte>.Empty;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ReadOnlyMemory<byte> bytes, JsonSerializerOptions options)
    {
        writer.WriteStringValue(Convert.ToBase64String(bytes.Span));
    }
}
