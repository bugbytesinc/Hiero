// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// EVM Hash JSON Converter.
/// </summary>
public sealed class EvmHashConverter : JsonConverter<EvmHash>
{
    /// <inheritdoc />
    public override EvmHash Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value is not null && EvmHash.TryParse(value, out var hash) ? hash : EvmHash.None;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EvmHash hash, JsonSerializerOptions options)
    {
        writer.WriteStringValue(hash.Bytes.IsEmpty ?
            "0x0000000000000000000000000000000000000000000000000000000000000000" :
            hash.ToString());
    }
    /// <inheritdoc />
    public override EvmHash ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Read(ref reader, typeToConvert, options);
    }
    /// <inheritdoc />
    public override void WriteAsPropertyName(Utf8JsonWriter writer, EvmHash value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.ToString());
    }
}
