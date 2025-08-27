using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Unsigned Long Converter that tollerates null values.
/// </summary>
public sealed class UnsignedLongMirrorConverter : JsonConverter<ulong>
{
    /// <inheritdoc />
    public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => 0,
            JsonTokenType.Number => reader.TryGetUInt64(out ulong result) ? result : 0,
            JsonTokenType.String => ulong.TryParse(reader.GetString(), out ulong result) ? result : 0,
            _ => 0
        };
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
    {
        // Yes this could be lossy
        writer.WriteNumberValue(value);
    }
}
