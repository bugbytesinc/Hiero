// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Nullable-long converter that preserves explicit <c>null</c>
/// values rather than coercing them to zero. Mirrors the
/// string/number tolerance of <see cref="LongMirrorConverter"/>.
/// </summary>
public sealed class NullableLongMirrorConverter : JsonConverter<long?>
{
    /// <inheritdoc />
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.Number => reader.TryGetInt64(out long result) ? result : (long?)null,
            JsonTokenType.String => long.TryParse(reader.GetString(), out long result) ? result : (long?)null,
            _ => null
        };
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteNumberValue(value.Value);
        }
    }
}
