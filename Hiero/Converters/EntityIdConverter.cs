using Google.Protobuf;
using Proto;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Hiero.Converters;

/// <summary>
/// Entity ID JSON Converter, can handle both hapi ids and evm addresses.
/// </summary>
public sealed class EntityIdConverter : JsonConverter<EntityId>
{
    /// <inheritdoc />
    public override EntityId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (!string.IsNullOrWhiteSpace(value))
        {
            if (value.StartsWith("0x"))
            {
                // This is probably contract encoded form (at least the 0x
                // indicates a HEX encoded binary value), may still be
                // something other than an EVM Payer, need to try.
                try
                {
                    var bytes = Hex.ToBytes(value[2..]);
                    if (bytes.Length == 20)
                    {
                        // This is HAPI Contract form or an outright EVM value
                        // HAPI will have a certain pattern we can guess.
                        var shard = new BigInteger(bytes.Slice(0, 4).Span, true, true);
                        var realm = new BigInteger(bytes.Slice(4, 8).Span, true, true);
                        var num = new BigInteger(bytes.Slice(12, 8).Span, false, true);
                        // Assume for now shard & realm are zero, don't
                        // know how long this will be valid, good guess for now.
                        if (shard.IsZero && realm.IsZero && num.Sign == 1)
                        {
                            return new EntityId((long)shard, (long)realm, (long)num);
                        }
                        // Not a small enough number, must be a EvmAddress (EVM Payer)
                        return new EvmAddress(bytes);
                    }
                    try
                    {
                        // Maybe the bytes are parsable as an
                        // Ed25519 or ECDSA public key
                        return new EntityId(0, 0, new Endorsement(bytes));
                    }
                    catch
                    {
                        // fall thru to possibility of complex key
                        // encoded as protobuf instead.
                    }
                    var byteString = ByteString.CopyFrom(bytes.Span);
                    return new EntityId(0, 0, Key.Parser.ParseFrom(byteString).ToEndorsement());
                }
                catch
                {
                    // Unhappy case, can't figure out what this actually
                    // represents, fall thru and let it return None
                }
            }
            else if (EntityId.TryParseShardRealmNum(value.AsSpan(), out EntityId? entityId))
            {
                return entityId;
            }
        }
        return EntityId.None;
    }
    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EntityId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
    /// <inheritdoc />
    public override EntityId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Read(ref reader, typeToConvert, options);
    }
    /// <inheritdoc />
    public override void WriteAsPropertyName(Utf8JsonWriter writer, [DisallowNull] EntityId value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.ToString());
    }
}
