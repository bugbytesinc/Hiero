// SPDX-License-Identifier: Apache-2.0
using System.Buffers.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Structured Transaction ID JSON Converter (for mirror node chunk information)
/// </summary>
public sealed class TransactionIdStructuredConverter : JsonConverter<TransactionId>
{
    private static readonly EntityIdConverter _entityIdConverter = new();
    private static readonly ConsensusTimeStampConverter _consensusTimeStampConverter = new();
    /// <inheritdoc />
    public override TransactionId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        EntityId? accountId = null;
        int? nonce = null;
        bool? scheduled = null;
        ConsensusTimeStamp? validStart = null;
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                if (reader.ValueTextEquals("account_id"u8))
                {
                    reader.Read();
                    accountId = _entityIdConverter.Read(ref reader, typeof(EntityId), options);
                }
                else if (reader.ValueTextEquals("nonce"u8))
                {
                    reader.Read();
                    nonce = reader.GetInt32();
                }
                else if (reader.ValueTextEquals("scheduled"u8))
                {
                    reader.Read();
                    scheduled = reader.GetBoolean();
                }
                else if (reader.ValueTextEquals("transaction_valid_start"u8))
                {
                    reader.Read();
                    validStart = _consensusTimeStampConverter.Read(ref reader, typeof(ConsensusTimeStamp), options);
                }
                else
                {
                    reader.Read();
                    reader.Skip();
                }
            }
        }
        if (accountId is null || validStart is null)
        {
            throw new JsonException("Invalid Transaction Id");
        }
        var seconds = (long)validStart.Value.Seconds;
        var nanos = (int)((validStart.Value.Seconds - seconds) * 1000000000);
        return new TransactionId(accountId, seconds, nanos, scheduled ?? false, nonce ?? 0);
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TransactionId id, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("account_id"u8);
        _entityIdConverter.Write(writer, id.Payer, options);
        writer.WritePropertyName("nonce"u8);
        writer.WriteNumberValue(id.ChildNonce);
        writer.WritePropertyName("scheduled"u8);
        writer.WriteBooleanValue(id.Scheduled);
        writer.WritePropertyName("transaction_valid_start"u8);
        Span<byte> buffer = stackalloc byte[30];
        if (!Utf8Formatter.TryFormat(id.ValidStartSeconds, buffer, out var secondsLength))
        {
            throw new JsonException("Unable to format transaction valid start seconds.");
        }
        buffer[secondsLength] = (byte)'.';
        var nanosSpan = buffer[(secondsLength + 1)..];
        if (!Utf8Formatter.TryFormat(id.ValidStartNanos, nanosSpan, out var nanosLength))
        {
            throw new JsonException("Unable to format transaction valid start nanos.");
        }
        var paddingLength = Math.Max(0, 9 - nanosLength);
        if (paddingLength > 0)
        {
            nanosSpan[..nanosLength].CopyTo(nanosSpan[paddingLength..]);
            nanosSpan[..paddingLength].Fill((byte)'0');
        }
        writer.WriteStringValue(buffer[..(secondsLength + 1 + paddingLength + nanosLength)]);
        writer.WriteEndObject();
    }
}
