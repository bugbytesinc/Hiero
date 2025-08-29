using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// EVM Address JSON Converter
/// </summary>
public sealed class EvmAddressConverter : JsonConverter<EvmAddress>
{
    /// <inheritdoc />
    public override EvmAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value is not null && EvmAddress.TryParse(value, out var evmAddress) ? evmAddress : EvmAddress.None;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EvmAddress evmAddress, JsonSerializerOptions options)
    {
        writer.WriteStringValue(evmAddress.Bytes.IsEmpty ?
            "0x0000000000000000000000000000000000000000" :
            evmAddress.ToString());
    }
    /// <inheritdoc />
    public override EvmAddress ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Read(ref reader, typeToConvert, options);
    }
    /// <inheritdoc />
    public override void WriteAsPropertyName(Utf8JsonWriter writer, EvmAddress value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.ToString());
    }
}
