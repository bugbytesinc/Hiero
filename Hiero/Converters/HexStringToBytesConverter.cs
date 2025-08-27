using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Converter for converting Hex encoded strings into byte arrays.
/// </summary>
public sealed class HexStringToBytesConverter : JsonConverter<ReadOnlyMemory<byte>>
{
    /// <inheritdoc />
    public override ReadOnlyMemory<byte> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var valueInHex = reader.GetString();
        if (!string.IsNullOrWhiteSpace(valueInHex))
        {
            try
            {
                if (valueInHex.StartsWith("0x"))
                {
                    valueInHex = valueInHex[2..];
                }
                return Hex.ToBytes(valueInHex);
            }
            catch
            {
                // Punt.
            }
        }
        return ReadOnlyMemory<byte>.Empty;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ReadOnlyMemory<byte> bytes, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"0x{Hex.FromBytes(bytes)}");
    }
}
