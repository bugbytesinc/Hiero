using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Long Converter that tollerates null values.
/// </summary>
public sealed class LongMirrorConverter : JsonConverter<long>
{
    /// <inheritdoc />
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => 0,
            JsonTokenType.Number => reader.TryGetInt64(out long result) ? result : 0,
            JsonTokenType.String => long.TryParse(reader.GetString(), out long result) ? result : 0,
            _ => 0
        };
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        // Yes this could be lossy
        writer.WriteNumberValue(value);
    }
}
