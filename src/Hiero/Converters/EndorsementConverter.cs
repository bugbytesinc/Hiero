// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Hiero.Implementation;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Endorsement JSON Converter
/// </summary>
public sealed class EndorsementConverter : JsonConverter<Endorsement>
{
    private enum SerializedType
    {
        Unknown,
        Ed25519,
        EcdsaSecp256K1,
        ProtobufEncoded
    }

    /// <inheritdoc />
    public override Endorsement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var type = SerializedType.Unknown;
        ReadOnlyMemory<byte> data = ReadOnlyMemory<byte>.Empty;
        var hasData = false;
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                if (reader.ValueTextEquals("_type"u8))
                {
                    reader.Read();
                    type = ReadType(ref reader);
                }
                else if (reader.ValueTextEquals("key"u8))
                {
                    reader.Read();
                    data = reader.ReadHexData();
                    hasData = true;
                }
                else
                {
                    reader.Skip();
                }
            }
        }
        if (type == SerializedType.ProtobufEncoded && (!hasData || data.Length == 0))
        {
            return Endorsement.None;
        }
        if (type == SerializedType.Unknown || !hasData || data.Length == 0)
        {
            throw new JsonException();
        }
        return type switch
        {
            SerializedType.Ed25519 => new Endorsement(KeyType.Ed25519, data),
            SerializedType.EcdsaSecp256K1 => new Endorsement(KeyType.ECDSASecp256K1, data),
            SerializedType.ProtobufEncoded => Proto.Key.Parser.ParseFrom(data.Span).ToEndorsement(),
            _ => throw new JsonException(),
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, Endorsement endorsement, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        switch (endorsement.Type)
        {
            case KeyType.Ed25519:
                writer.WriteString("_type"u8, "ED25519"u8);
                writer.WriteHexString("key"u8, ((Ed25519EndorsementData)endorsement._data).RawPublicKey);
                break;
            case KeyType.ECDSASecp256K1:
                writer.WriteString("_type"u8, "ECDSA_SECP256K1"u8);
                writer.WriteHexString("key"u8, ((EcdsaSecp256K1EndorsementData)endorsement._data).RawPublicKey);
                break;
            default:
                writer.WriteString("_type"u8, "ProtobufEncoded"u8);
                writer.WriteHexString("key"u8, endorsement == Endorsement.None ? [] : new Proto.Key(endorsement).ToByteArray());
                break;
        }
        writer.WriteEndObject();
    }

    private static SerializedType ReadType(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            return SerializedType.Unknown;
        }
        if (!reader.HasValueSequence && !reader.ValueIsEscaped)
        {
            if (reader.ValueTextEquals("ED25519"u8))
            {
                return SerializedType.Ed25519;
            }
            if (reader.ValueTextEquals("ECDSA_SECP256K1"u8))
            {
                return SerializedType.EcdsaSecp256K1;
            }
            if (reader.ValueTextEquals("ProtobufEncoded"u8))
            {
                return SerializedType.ProtobufEncoded;
            }
            return SerializedType.Unknown;
        }
        return reader.GetString() switch
        {
            "ED25519" => SerializedType.Ed25519,
            "ECDSA_SECP256K1" => SerializedType.EcdsaSecp256K1,
            "ProtobufEncoded" => SerializedType.ProtobufEncoded,
            _ => SerializedType.Unknown,
        };
    }
}
