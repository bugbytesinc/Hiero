using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// TransactionId Converter for reading and writing JSON
/// when interacting with a mirror node.
/// </summary>
public sealed class TransactionIdMirrorConverter : JsonConverter<TransactionId>
{
    /// <inheritdoc />
    public override TransactionId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var valueAsString = reader.GetString();
        if (!string.IsNullOrWhiteSpace(valueAsString))
        {
            var parts = valueAsString.Split('-');
            if (parts.Length > 1 &&
                parts.Length < 4 &&
                EntityId.TryParseShardRealmNum(parts[0], out var address) &&
                long.TryParse(parts[1], out var seconds) &&
                int.TryParse((parts.Length == 3 ? parts[2] : null) ?? "0", out var nanos))
            {
                return new TransactionId(address, seconds, nanos);
            }
        }
        return TransactionId.None;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TransactionId txId, JsonSerializerOptions options)
    {
        if (txId == null || txId == TransactionId.None)
        {
            writer.WriteStringValue(string.Empty);
        }
        else
        {
            writer.WriteStringValue($"{txId.Payer}-{txId.ValidStartSeconds}-{txId.ValidStartNanos:000000000}");
        }
    }
}