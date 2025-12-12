using Google.Protobuf;
using Org.BouncyCastle.Crypto.Parameters;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Endorsement JSON Converter
/// </summary>
public sealed class EndorsementConverter : JsonConverter<Endorsement>
{
    /// <inheritdoc />
    public override Endorsement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? type = null;
        string? data = null;
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
                    case "_type":
                        type = reader.GetString();
                        break;

                    case "key":
                        data = reader.GetString();
                        break;
                }
            }
        }
        if ("ProtobufEncoded" == type && string.IsNullOrEmpty(data))
        {
            return Endorsement.None;
        }
        if (string.IsNullOrWhiteSpace(data) || string.IsNullOrWhiteSpace(type))
        {
            throw new JsonException();
        }
        var bytes = Hex.ToBytes(data);
        return type switch
        {
            "ED25519" => new Endorsement(KeyType.Ed25519, bytes),
            "ECDSA_SECP256K1" => new Endorsement(KeyType.ECDSASecp256K1, bytes),
            "ProtobufEncoded" => Proto.Key.Parser.ParseFrom(bytes.ToArray()).ToEndorsement(),
            _ => throw new JsonException(),
        };
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Endorsement endorsement, JsonSerializerOptions options)
    {
        string type;
        byte[] data;
        switch (endorsement.Type)
        {
            case KeyType.Ed25519:
                type = "ED25519";
                data = ((Ed25519PublicKeyParameters)endorsement._data).GetEncoded();
                break;
            case KeyType.ECDSASecp256K1:
                type = "ECDSA_SECP256K1";
                data = ((ECPublicKeyParameters)endorsement._data).Q.GetEncoded(true);
                break;
            default:
                type = "ProtobufEncoded";
                data = endorsement == Endorsement.None ? [] : new Proto.Key(endorsement).ToByteArray();
                break;
        }
        writer.WriteStartObject();
        writer.WritePropertyName("_type");
        writer.WriteStringValue(type);
        writer.WritePropertyName("key");
        writer.WriteStringValue(Hex.FromBytes(data));
        writer.WriteEndObject();
    }
}
