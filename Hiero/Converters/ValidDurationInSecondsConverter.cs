using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Converter for <see cref="TimeSpan"/> values that represent a valid duration in seconds.
/// </summary>
public sealed class ValidDurationInSecondsConverter : JsonConverter<TimeSpan>
{
    /// <inheritdoc />
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (double.TryParse(reader.GetString(), out var value))
        {
            return TimeSpan.FromSeconds(value);
        }
        return TimeSpan.Zero;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TimeSpan timespan, JsonSerializerOptions options)
    {
        writer.WriteStringValue(((int)timespan.TotalSeconds).ToString());
    }
}