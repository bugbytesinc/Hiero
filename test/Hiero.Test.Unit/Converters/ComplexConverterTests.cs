// SPDX-License-Identifier: Apache-2.0
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hiero.Converters;
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Converters;

public class ComplexConverterTests
{
    // --- Wrapper records for converters that are not [JsonConverter] on the type ---
    private record BigIntArrayWrapper([property: JsonConverter(typeof(BigIntegerArrayConverter))] BigInteger[] Values);
    private record EntityIdArrayWrapper([property: JsonConverter(typeof(EntityIdArrayConverter))] EntityId[] Values);
    private record EvmAddressArrayWrapper([property: JsonConverter(typeof(EvmAddressArrayConverter))] EvmAddress[] Values);
    private record HexArrayWrapper([property: JsonConverter(typeof(HexStringArraytoBytesArrayConverter))] ReadOnlyMemory<byte>[] Values);
    private record TxIdStructuredWrapper([property: JsonConverter(typeof(TransactionIdStructuredConverter))] TransactionId Value);

    // ========================================================================
    // 1. BigIntegerArrayConverter
    // ========================================================================

    [Test]
    public async Task BigIntegerArrayConverter_Deserializes_Hex_Strings_To_BigInteger_Array()
    {
        var json = """{"Values":["0x1","0xa","0xff"]}""";
        var result = JsonSerializer.Deserialize<BigIntArrayWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Values.Length).IsEqualTo(3);
        await Assert.That(result.Values[0]).IsEqualTo(new BigInteger(1));
        await Assert.That(result.Values[1]).IsEqualTo(new BigInteger(10));
        await Assert.That(result.Values[2]).IsEqualTo(new BigInteger(255));
    }

    [Test]
    public async Task BigIntegerArrayConverter_Serializes_BigInteger_Array_To_Hex_Strings()
    {
        var wrapper = new BigIntArrayWrapper([new BigInteger(0), new BigInteger(1), new BigInteger(255)]);
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).Contains("0x0");
        await Assert.That(json).Contains("0x1");
        await Assert.That(json).Contains("0xff");
    }

    [Test]
    public async Task BigIntegerArrayConverter_Round_Trips()
    {
        var original = new BigIntArrayWrapper([new BigInteger(0), new BigInteger(42), new BigInteger(1000000)]);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<BigIntArrayWrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Values.Length).IsEqualTo(original.Values.Length);
        for (int i = 0; i < original.Values.Length; i++)
        {
            await Assert.That(deserialized.Values[i]).IsEqualTo(original.Values[i]);
        }
    }

    [Test]
    public async Task BigIntegerArrayConverter_Handles_Empty_Array()
    {
        var wrapper = new BigIntArrayWrapper([]);
        var json = JsonSerializer.Serialize(wrapper);
        var deserialized = JsonSerializer.Deserialize<BigIntArrayWrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Values.Length).IsEqualTo(0);
    }

    // ========================================================================
    // 2. EntityIdArrayConverter
    // ========================================================================

    [Test]
    public async Task EntityIdArrayConverter_Deserializes_String_Array_To_EntityId_Array()
    {
        var json = """{"Values":["0.0.5","0.0.100","1.2.3"]}""";
        var result = JsonSerializer.Deserialize<EntityIdArrayWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Values.Length).IsEqualTo(3);
        await Assert.That(result.Values[0]).IsEqualTo(new EntityId(0, 0, 5));
        await Assert.That(result.Values[1]).IsEqualTo(new EntityId(0, 0, 100));
        await Assert.That(result.Values[2]).IsEqualTo(new EntityId(1, 2, 3));
    }

    [Test]
    public async Task EntityIdArrayConverter_Serializes_EntityId_Array()
    {
        var wrapper = new EntityIdArrayWrapper([new EntityId(0, 0, 5), new EntityId(0, 0, 100)]);
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).Contains("0.0.5");
        await Assert.That(json).Contains("0.0.100");
    }

    [Test]
    public async Task EntityIdArrayConverter_Round_Trips()
    {
        var original = new EntityIdArrayWrapper([new EntityId(0, 0, 5), new EntityId(0, 0, 99), new EntityId(1, 2, 3)]);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<EntityIdArrayWrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Values.Length).IsEqualTo(original.Values.Length);
        for (int i = 0; i < original.Values.Length; i++)
        {
            await Assert.That(deserialized.Values[i]).IsEqualTo(original.Values[i]);
        }
    }

    [Test]
    public async Task EntityIdArrayConverter_Handles_Empty_Array()
    {
        var wrapper = new EntityIdArrayWrapper([]);
        var json = JsonSerializer.Serialize(wrapper);
        var deserialized = JsonSerializer.Deserialize<EntityIdArrayWrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Values.Length).IsEqualTo(0);
    }

    // ========================================================================
    // 3. EvmAddressArrayConverter
    // ========================================================================

    [Test]
    public async Task EvmAddressArrayConverter_Deserializes_Hex_Strings_To_EvmAddress_Array()
    {
        var addr1Bytes = new byte[20];
        addr1Bytes[19] = 0x01;
        var addr2Bytes = new byte[20];
        addr2Bytes[19] = 0x02;
        var addr1Hex = "0x" + Hex.FromBytes(addr1Bytes);
        var addr2Hex = "0x" + Hex.FromBytes(addr2Bytes);
        var json = $$$"""{"Values":["{{{addr1Hex}}}","{{{addr2Hex}}}"]}""";
        var result = JsonSerializer.Deserialize<EvmAddressArrayWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Values.Length).IsEqualTo(2);
        await Assert.That(result.Values[0]).IsEqualTo(new EvmAddress(addr1Bytes));
        await Assert.That(result.Values[1]).IsEqualTo(new EvmAddress(addr2Bytes));
    }

    [Test]
    public async Task EvmAddressArrayConverter_Serializes_EvmAddress_Array()
    {
        var bytes1 = Generator.KeyPair().publicKey[^20..];
        var bytes2 = Generator.KeyPair().publicKey[^20..];
        var wrapper = new EvmAddressArrayWrapper([new EvmAddress(bytes1), new EvmAddress(bytes2)]);
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).Contains("Values");
        // The serialized JSON should contain two hex-encoded addresses
        var deserialized = JsonSerializer.Deserialize<EvmAddressArrayWrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Values.Length).IsEqualTo(2);
    }

    [Test]
    public async Task EvmAddressArrayConverter_Round_Trips()
    {
        var bytes1 = Generator.KeyPair().publicKey[^20..];
        var bytes2 = Generator.KeyPair().publicKey[^20..];
        var original = new EvmAddressArrayWrapper([new EvmAddress(bytes1), new EvmAddress(bytes2)]);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<EvmAddressArrayWrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Values.Length).IsEqualTo(original.Values.Length);
        for (int i = 0; i < original.Values.Length; i++)
        {
            await Assert.That(deserialized.Values[i]).IsEqualTo(original.Values[i]);
        }
    }

    // ========================================================================
    // 4. HexStringArraytoBytesArrayConverter
    // ========================================================================

    [Test]
    public async Task HexStringArraytoBytesArrayConverter_Deserializes_Hex_Strings_To_Byte_Arrays()
    {
        var json = """{"Values":["0x0102","0xaabb","0xff"]}""";
        var result = JsonSerializer.Deserialize<HexArrayWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Values.Length).IsEqualTo(3);
        await Assert.That(result.Values[0].Span.SequenceEqual(new byte[] { 0x01, 0x02 })).IsTrue();
        await Assert.That(result.Values[1].Span.SequenceEqual(new byte[] { 0xaa, 0xbb })).IsTrue();
        await Assert.That(result.Values[2].Span.SequenceEqual(new byte[] { 0xff })).IsTrue();
    }

    [Test]
    public async Task HexStringArraytoBytesArrayConverter_Serializes_Byte_Arrays_To_Hex_Strings()
    {
        var wrapper = new HexArrayWrapper([new byte[] { 0x01, 0x02 }, new byte[] { 0xaa, 0xbb }]);
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).Contains("0x0102");
        await Assert.That(json).Contains("0xaabb");
    }

    [Test]
    public async Task HexStringArraytoBytesArrayConverter_Round_Trips()
    {
        var originalBytes = new ReadOnlyMemory<byte>[]
        {
            new byte[] { 0x01, 0x02, 0x03 },
            new byte[] { 0xde, 0xad, 0xbe, 0xef }
        };
        var original = new HexArrayWrapper(originalBytes);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<HexArrayWrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Values.Length).IsEqualTo(original.Values.Length);
        for (int i = 0; i < original.Values.Length; i++)
        {
            await Assert.That(deserialized.Values[i].Span.SequenceEqual(original.Values[i].Span)).IsTrue();
        }
    }

    // ========================================================================
    // 5. EndorsementConverter
    // ========================================================================

    [Test]
    public async Task EndorsementConverter_Serializes_Ed25519_Key()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var endorsement = new Endorsement(publicKey);
        var json = JsonSerializer.Serialize(endorsement);
        await Assert.That(json).Contains("ED25519");
        await Assert.That(json).Contains("_type");
        await Assert.That(json).Contains("key");
    }

    [Test]
    public async Task EndorsementConverter_Deserializes_Ed25519_Key()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var original = new Endorsement(publicKey);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<Endorsement>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Type).IsEqualTo(KeyType.Ed25519);
    }

    [Test]
    public async Task EndorsementConverter_Serializes_ECDSASecp256K1_Key()
    {
        var (publicKey, _) = Generator.Secp256k1KeyPair();
        var endorsement = new Endorsement(publicKey);
        var json = JsonSerializer.Serialize(endorsement);
        await Assert.That(json).Contains("ECDSA_SECP256K1");
        await Assert.That(json).Contains("_type");
        await Assert.That(json).Contains("key");
    }

    [Test]
    public async Task EndorsementConverter_Deserializes_ECDSASecp256K1_Key()
    {
        var (publicKey, _) = Generator.Secp256k1KeyPair();
        var original = new Endorsement(publicKey);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<Endorsement>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Type).IsEqualTo(KeyType.ECDSASecp256K1);
    }

    [Test]
    public async Task EndorsementConverter_Round_Trips_Ed25519()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var original = new Endorsement(publicKey);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<Endorsement>(json);
        await Assert.That(deserialized).IsEqualTo(original);
    }

    [Test]
    public async Task EndorsementConverter_Round_Trips_ECDSASecp256K1()
    {
        var (publicKey, _) = Generator.Secp256k1KeyPair();
        var original = new Endorsement(publicKey);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<Endorsement>(json);
        await Assert.That(deserialized).IsEqualTo(original);
    }

    // ========================================================================
    // 6. TransactionIdStructuredConverter
    // ========================================================================

    [Test]
    public async Task TransactionIdStructuredConverter_Deserializes_Structured_Json()
    {
        var json = """{"Value":{"account_id":"0.0.5","nonce":0,"scheduled":false,"transaction_valid_start":"1234567890.123456789"}}""";
        var result = JsonSerializer.Deserialize<TxIdStructuredWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsNotNull();
        await Assert.That(result.Value.Payer).IsEqualTo(new EntityId(0, 0, 5));
        await Assert.That(result.Value.ValidStartSeconds).IsEqualTo(1234567890L);
        await Assert.That(result.Value.ValidStartNanos).IsEqualTo(123456789);
        await Assert.That(result.Value.Scheduled).IsFalse();
        await Assert.That(result.Value.ChildNonce).IsEqualTo(0);
    }

    [Test]
    public async Task TransactionIdStructuredConverter_Serializes_To_Structured_Json()
    {
        var payer = new EntityId(0, 0, 5);
        var txId = new TransactionId(payer, 1234567890L, 123456789);
        var wrapper = new TxIdStructuredWrapper(txId);
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).Contains("account_id");
        await Assert.That(json).Contains("0.0.5");
        await Assert.That(json).Contains("transaction_valid_start");
        await Assert.That(json).Contains("1234567890.123456789");
        await Assert.That(json).Contains("nonce");
        await Assert.That(json).Contains("scheduled");
    }

    [Test]
    public async Task TransactionIdStructuredConverter_Round_Trips()
    {
        var payer = new EntityId(0, 0, Generator.Integer(10, 200));
        var seconds = (long)Generator.Integer(1000000, 2000000000);
        var nanos = Generator.Integer(0, 999999999);
        var txId = new TransactionId(payer, seconds, nanos);
        var original = new TxIdStructuredWrapper(txId);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<TxIdStructuredWrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Value.Payer).IsEqualTo(payer);
        await Assert.That(deserialized.Value.ValidStartSeconds).IsEqualTo(seconds);
        await Assert.That(deserialized.Value.ValidStartNanos).IsEqualTo(nanos);
        await Assert.That(deserialized.Value.Scheduled).IsFalse();
        await Assert.That(deserialized.Value.ChildNonce).IsEqualTo(0);
    }

    // ========================================================================
    // 7. ResponseCodeConverter
    // ========================================================================

    [Test]
    public async Task ResponseCodeConverter_Serializes_Success()
    {
        var json = JsonSerializer.Serialize(ResponseCode.Success);
        await Assert.That(json).IsEqualTo("\"SUCCESS\"");
    }

    [Test]
    public async Task ResponseCodeConverter_Serializes_InvalidTransaction()
    {
        var json = JsonSerializer.Serialize(ResponseCode.InvalidTransaction);
        await Assert.That(json).IsEqualTo("\"INVALID_TRANSACTION\"");
    }

    [Test]
    public async Task ResponseCodeConverter_Deserializes_Success()
    {
        var code = JsonSerializer.Deserialize<ResponseCode>("\"SUCCESS\"");
        await Assert.That(code).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task ResponseCodeConverter_Round_Trips_Multiple_Values()
    {
        var codes = new[]
        {
            ResponseCode.Success,
            ResponseCode.InvalidTransaction,
            ResponseCode.Ok,
            ResponseCode.PayerAccountNotFound,
            ResponseCode.InsufficientTxFee
        };
        foreach (var code in codes)
        {
            var json = JsonSerializer.Serialize(code);
            var deserialized = JsonSerializer.Deserialize<ResponseCode>(json);
            await Assert.That(deserialized).IsEqualTo(code);
        }
    }
}
