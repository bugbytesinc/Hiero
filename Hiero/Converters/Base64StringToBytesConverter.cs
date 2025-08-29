using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// 64 base encoded memo _entityIdConverter
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
