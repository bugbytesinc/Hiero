// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Converts a (optionally <c>0x</c>-prefixed) hexadecimal JSON string to a
/// <see cref="ReadOnlyMemory{T}"/> of bytes and back, writing with a <c>0x</c> prefix.
/// </summary>
public sealed class HexStringToBytesConverter : JsonConverter<ReadOnlyMemory<byte>>
{
    /// <inheritdoc />
    public override ReadOnlyMemory<byte> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.ReadHexData();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ReadOnlyMemory<byte> bytes, JsonSerializerOptions options)
    {
        writer.WriteHexStringValue(bytes.Span, true);
    }
}
