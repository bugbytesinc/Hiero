using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Structured Transaction ID JSON Converter (for mirror node chunk information)
/// </summary>
public sealed class TransactionIdStructuredConverter : JsonConverter<TransactionId>
{
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
                var propertyName = reader.GetString();
                reader.Read();
                switch (propertyName)
                {
                    case "account_id":
                        accountId = new EntityIdConverter().Read(ref reader, typeof(EntityId), options);
                        break;

                    case "nonce":
                        nonce = reader.GetInt32();
                        break;

                    case "scheduled":
                        scheduled = reader.GetBoolean();
                        break;

                    case "transaction_valid_start":
                        validStart = new ConsensusTimeStampConverter().Read(ref reader, typeof(ConsensusTimeStamp), options);
                        break;
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
        writer.WritePropertyName("account_id");
        new EntityIdConverter().Write(writer, id.Payer, options);
        writer.WritePropertyName("nonce");
        writer.WriteNumberValue(id.ChildNonce);
        writer.WritePropertyName("scheduled");
        writer.WriteBooleanValue(id.Scheduled);
        writer.WritePropertyName("transaction_valid_start");
        writer.WriteStringValue($"{id.ValidStartSeconds}.{id.ValidStartNanos.ToString().PadLeft(9, '0')}");
        writer.WriteEndObject();
    }
}
