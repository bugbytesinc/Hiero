// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// Converts a base64-encoded JSON string to a <see cref="ReadOnlyMemory{T}"/> of bytes and back.
/// </summary>
public sealed class Base64StringToBytesConverter : JsonConverter<ReadOnlyMemory<byte>>
{
    /// <inheritdoc />
    public override ReadOnlyMemory<byte> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            return ReadOnlyMemory<byte>.Empty;
        }
        return reader.TryGetBytesFromBase64(out var bytes) && bytes is not null
            ? bytes
            : ReadOnlyMemory<byte>.Empty;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ReadOnlyMemory<byte> bytes, JsonSerializerOptions options)
    {
        writer.WriteBase64StringValue(bytes.Span);
    }
}
