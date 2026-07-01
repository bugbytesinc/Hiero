// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Boolean JSON converter that tolerates string representations from mirror node responses.
/// </summary>
public sealed class BooleanMirrorConverter : JsonConverter<bool>
{
    /// <inheritdoc />
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.String => IsTrueString(ref reader),
            _ => false
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }

    private static bool IsTrueString(ref Utf8JsonReader reader)
    {
        if (reader.HasValueSequence || reader.ValueIsEscaped)
        {
            return string.Equals(reader.GetString(), "true", StringComparison.OrdinalIgnoreCase);
        }
        var value = reader.ValueSpan;
        return value.Length == 4 &&
            ToLowerAscii(value[0]) == (byte)'t' &&
            ToLowerAscii(value[1]) == (byte)'r' &&
            ToLowerAscii(value[2]) == (byte)'u' &&
            ToLowerAscii(value[3]) == (byte)'e';
    }

    private static byte ToLowerAscii(byte value)
    {
        return value is >= (byte)'A' and <= (byte)'Z' ? (byte)(value + 32) : value;
    }
}
