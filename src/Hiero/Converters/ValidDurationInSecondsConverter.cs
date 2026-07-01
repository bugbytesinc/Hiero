// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation.Parsing;
using System.Buffers.Text;
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
        if (reader.TokenType == JsonTokenType.Null)
        {
            return TimeSpan.Zero;
        }
        if (NumericMirrorStringParser.TryGetDouble(ref reader, out var value))
        {
            return TimeSpan.FromSeconds(value);
        }
        return TimeSpan.Zero;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TimeSpan timespan, JsonSerializerOptions options)
    {
        Span<byte> buffer = stackalloc byte[11];
        if (!Utf8Formatter.TryFormat((int)timespan.TotalSeconds, buffer, out var bytesWritten))
        {
            throw new JsonException("Unable to format duration.");
        }
        writer.WriteStringValue(buffer[..bytesWritten]);
    }
}
