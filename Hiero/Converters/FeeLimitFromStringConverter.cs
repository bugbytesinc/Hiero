using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Fee Limit JSON Converter (from string value)
/// </summary>
public sealed class FeeLimitFromStringConverter : JsonConverter<long>
{
    /// <inheritdoc />
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (long.TryParse(reader.GetString(), out var value))
        {
            return value;
        }
        return 0;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, long feeLimit, JsonSerializerOptions options)
    {
        writer.WriteStringValue(feeLimit.ToString());
    }
}