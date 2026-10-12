// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;
/// <summary>
/// EIP-2930 Access List JSON Converter
/// </summary>
/// <remarks>
/// The mirror node reports a contract result's access list as an array of
/// entries, but versions up to at least v0.152 reported it as an opaque
/// string. That form carries nothing an <see cref="AccessListEntryData"/>
/// can hold, so it reads as null rather than failing the whole result.
/// </remarks>
public sealed class AccessListConverter : JsonConverter<AccessListEntryData[]?>
{
    private static readonly EvmAddressConverter _evmAddressConverter = new();
    private static readonly HexStringArraytoBytesArrayConverter _storageKeysConverter = new();
    /// <inheritdoc />
    public override AccessListEntryData[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            reader.Skip();
            return null;
        }
        var entries = new List<AccessListEntryData>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                reader.Skip();
                continue;
            }
            var entry = new AccessListEntryData();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.ValueTextEquals("address"u8))
                {
                    reader.Read();
                    entry.Address = _evmAddressConverter.Read(ref reader, typeof(EvmAddress), options);
                }
                else if (reader.ValueTextEquals("storage_keys"u8))
                {
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.StartArray)
                    {
                        entry.StorageKeys = _storageKeysConverter.Read(ref reader, typeof(ReadOnlyMemory<byte>[]), options);
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
                else
                {
                    reader.Read();
                    reader.Skip();
                }
            }
            entries.Add(entry);
        }
        return [.. entries];
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, AccessListEntryData[]? entries, JsonSerializerOptions options)
    {
        if (entries is null)
        {
            writer.WriteNullValue();
            return;
        }
        writer.WriteStartArray();
        foreach (var entry in entries)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("address"u8);
            _evmAddressConverter.Write(writer, entry.Address, options);
            writer.WritePropertyName("storage_keys"u8);
            _storageKeysConverter.Write(writer, entry.StorageKeys, options);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
}
