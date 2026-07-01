// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation.Parsing;
using System.Buffers.Text;
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
        if (reader.TokenType == JsonTokenType.Null)
        {
            return 0;
        }
        if (NumericMirrorStringParser.TryGetInt64(ref reader, out var value))
        {
            return value;
        }
        return 0;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, long feeLimit, JsonSerializerOptions options)
    {
        Span<byte> buffer = stackalloc byte[20];
        if (!Utf8Formatter.TryFormat(feeLimit, buffer, out var bytesWritten))
        {
            throw new JsonException("Unable to format fee limit.");
        }
        writer.WriteStringValue(buffer[..bytesWritten]);
    }
}
