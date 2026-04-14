// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using System.Text.Json.Serialization;
using Hiero.Converters;
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Converters;

public class DomainConverterTests
{
    // ── Wrapper records for converters that need per-property attribution ──

    private record ExchangeRateTimestampWrapper(
        [property: JsonConverter(typeof(ConsensusTimeStampForExchangeRateConverter))]
        ConsensusTimeStamp Value);

    private record TokenExpirationTimestampWrapper(
        [property: JsonConverter(typeof(ConsensusTimeStampForTokenExpirationConverter))]
        ConsensusTimeStamp Value);

    private record FreezeWrapper(
        [property: JsonConverter(typeof(FreezeStatusConverter))]
        TokenTradableStatus Value);

    private record PauseWrapper(
        [property: JsonConverter(typeof(PauseStatusConverter))]
        TokenTradableStatus Value);

    private record KycWrapper(
        [property: JsonConverter(typeof(TokenKycStatusConverter))]
        TokenKycStatus Value);

    private record TokenTypeWrapper(
        [property: JsonConverter(typeof(TokenTypeConverter))]
        TokenType Value);

    private record TxIdMirrorWrapper(
        [property: JsonConverter(typeof(TransactionIdMirrorConverter))]
        TransactionId Value);

    private record EncodedParamsWrapper(
        [property: JsonConverter(typeof(EncodedParamsConverter))]
        EncodedParams Value);

    // ═══════════════════════════════════════════════════════════════════════
    //  1. ConsensusTimeStampConverter (has [JsonConverter] on type)
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task ConsensusTimeStamp_Serialize_ProducesDecimalSecondsString()
    {
        var ts = new ConsensusTimeStamp(1234567890L, 123456789);
        var json = JsonSerializer.Serialize(ts);
        await Assert.That(json).IsEqualTo("\"1234567890.123456789\"");
    }

    [Test]
    public async Task ConsensusTimeStamp_Deserialize_ParsesDecimalSecondsString()
    {
        var json = "\"1234567890.123456789\"";
        var ts = JsonSerializer.Deserialize<ConsensusTimeStamp>(json);
        await Assert.That(ts.Seconds).IsEqualTo(1234567890.123456789m);
    }

    [Test]
    public async Task ConsensusTimeStamp_RoundTrip_PreservesValue()
    {
        var original = new ConsensusTimeStamp(1700000000L, 999999999);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<ConsensusTimeStamp>(json);
        await Assert.That(deserialized).IsEqualTo(original);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  2. EntityIdConverter (has [JsonConverter] on type)
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task EntityId_Serialize_ProducesShardRealmNum()
    {
        var entity = new EntityId(0, 0, 5);
        var json = JsonSerializer.Serialize(entity);
        await Assert.That(json).IsEqualTo("\"0.0.5\"");
    }

    [Test]
    public async Task EntityId_Deserialize_ParsesShardRealmNum()
    {
        var json = "\"0.0.5\"";
        var entity = JsonSerializer.Deserialize<EntityId>(json);
        await Assert.That(entity).IsEqualTo(new EntityId(0, 0, 5));
    }

    [Test]
    public async Task EntityId_RoundTrip_PreservesRandomValues()
    {
        var shard = (long)Generator.Integer(0, 10);
        var realm = (long)Generator.Integer(0, 100);
        var num = (long)Generator.Integer(1, 999999);
        var original = new EntityId(shard, realm, num);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<EntityId>(json);
        await Assert.That(deserialized).IsEqualTo(original);
    }

    [Test]
    public async Task EntityId_Deserialize_EmptyString_ReturnsNone()
    {
        var json = "\"\"";
        var entity = JsonSerializer.Deserialize<EntityId>(json);
        await Assert.That(entity).IsEqualTo(EntityId.None);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  3. EvmAddressConverter (has [JsonConverter] on type)
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task EvmAddress_RoundTrip_PreservesKnownBytes()
    {
        var bytes = new byte[20];
        for (int i = 0; i < 20; i++) bytes[i] = (byte)(i + 1);
        var original = new EvmAddress(bytes);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<EvmAddress>(json);
        await Assert.That(deserialized).IsEqualTo(original);
    }

    [Test]
    public async Task EvmAddress_Serialize_ProducesHexStringWith0xPrefix()
    {
        var bytes = new byte[20];
        bytes[19] = 0x01;
        var addr = new EvmAddress(bytes);
        var json = JsonSerializer.Serialize(addr);
        await Assert.That(json).Contains("0x");
        // Should be a 42-character hex string (0x + 40 hex chars) inside quotes
        var value = JsonSerializer.Deserialize<string>(json)!;
        await Assert.That(value.Length).IsEqualTo(42);
        await Assert.That(value.StartsWith("0x")).IsTrue();
    }

    [Test]
    public async Task EvmAddress_Deserialize_None_ProducesZeroAddress()
    {
        var json = "\"0x0000000000000000000000000000000000000000\"";
        var addr = JsonSerializer.Deserialize<EvmAddress>(json);
        await Assert.That(addr).IsEqualTo(EvmAddress.None);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  4. NftConverter (has [JsonConverter] on type)
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Nft_Serialize_ProducesTokenHashSerial()
    {
        var nft = new Hiero.Nft(new EntityId(0, 0, 5), 3);
        var json = JsonSerializer.Serialize(nft);
        await Assert.That(json).IsEqualTo("\"0.0.5#3\"");
    }

    [Test]
    public async Task Nft_Deserialize_ParsesTokenHashSerial()
    {
        var json = "\"0.0.5#3\"";
        var nft = JsonSerializer.Deserialize<Hiero.Nft>(json);
        await Assert.That(nft).IsNotNull();
        await Assert.That(nft!.Token).IsEqualTo(new EntityId(0, 0, 5));
        await Assert.That(nft.SerialNumber).IsEqualTo(3L);
    }

    [Test]
    public async Task Nft_RoundTrip_PreservesValue()
    {
        var token = new EntityId(0, 0, (long)Generator.Integer(1, 99999));
        var serial = (long)Generator.Integer(1, 10000);
        var original = new Hiero.Nft(token, serial);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<Hiero.Nft>(json);
        await Assert.That(deserialized).IsEqualTo(original);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  5. ConsensusNodeEndpointConverter (has [JsonConverter] on type)
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task ConsensusNodeEndpoint_Serialize_ProducesJsonObject()
    {
        var endpoint = new ConsensusNodeEndpoint(
            new EntityId(0, 0, 3),
            new Uri("https://0.testnet.hedera.com:50211"));
        var json = JsonSerializer.Serialize(endpoint);
        await Assert.That(json).Contains("\"address\"");
        await Assert.That(json).Contains("\"0.0.3\"");
        await Assert.That(json).Contains("\"url\"");
        await Assert.That(json).Contains("https://0.testnet.hedera.com:50211");
    }

    [Test]
    public async Task ConsensusNodeEndpoint_Deserialize_ParsesJsonObject()
    {
        var json = """{"address":"0.0.3","url":"https://0.testnet.hedera.com:50211"}""";
        var endpoint = JsonSerializer.Deserialize<ConsensusNodeEndpoint>(json);
        await Assert.That(endpoint).IsNotNull();
        await Assert.That(endpoint!.Node).IsEqualTo(new EntityId(0, 0, 3));
        await Assert.That(endpoint.Uri.Host).IsEqualTo("0.testnet.hedera.com");
    }

    [Test]
    public async Task ConsensusNodeEndpoint_RoundTrip_PreservesValue()
    {
        var original = new ConsensusNodeEndpoint(
            new EntityId(0, 0, 5),
            new Uri("https://2.testnet.hedera.com:50211"));
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<ConsensusNodeEndpoint>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Node).IsEqualTo(original.Node);
        await Assert.That(deserialized.Uri).IsEqualTo(original.Uri);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  6. ConsensusTimeStampForExchangeRateConverter (wrapper needed)
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task ExchangeRateTimestamp_Deserialize_ReadsDecimalNumber()
    {
        var json = """{"Value":1234567890.5}""";
        var result = JsonSerializer.Deserialize<ExchangeRateTimestampWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Seconds).IsEqualTo(1234567890.5m);
    }

    [Test]
    public async Task ExchangeRateTimestamp_Serialize_WritesSecondsNumber()
    {
        var ts = new ConsensusTimeStamp(1234567890L, 0);
        var wrapper = new ExchangeRateTimestampWrapper(ts);
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).IsEqualTo("""{"Value":1234567890}""");
    }

    [Test]
    public async Task ExchangeRateTimestamp_RoundTrip_PreservesWholeSeconds()
    {
        var original = new ConsensusTimeStamp(1700000000m);
        var wrapper = new ExchangeRateTimestampWrapper(original);
        var json = JsonSerializer.Serialize(wrapper);
        var deserialized = JsonSerializer.Deserialize<ExchangeRateTimestampWrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Value.Seconds).IsEqualTo(original.Seconds);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  7. ConsensusTimeStampForTokenExpirationConverter (wrapper needed)
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task TokenExpirationTimestamp_Deserialize_ReadsNanoseconds()
    {
        // 1234567890 seconds = 1234567890000000000 nanos
        var nanos = 1234567890000000000m;
        var json = $$$"""{"Value":{{{nanos}}}}""";
        var result = JsonSerializer.Deserialize<TokenExpirationTimestampWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.Seconds).IsEqualTo(1234567890m);
    }

    [Test]
    public async Task TokenExpirationTimestamp_Serialize_WritesNanoseconds()
    {
        var ts = new ConsensusTimeStamp(1234567890m);
        var wrapper = new TokenExpirationTimestampWrapper(ts);
        var json = JsonSerializer.Serialize(wrapper);
        // Seconds * 1000000000 = nanoseconds
        await Assert.That(json).IsEqualTo("""{"Value":1234567890000000000}""");
    }

    [Test]
    public async Task TokenExpirationTimestamp_RoundTrip_PreservesValue()
    {
        var original = new ConsensusTimeStamp(1700000000m);
        var wrapper = new TokenExpirationTimestampWrapper(original);
        var json = JsonSerializer.Serialize(wrapper);
        var deserialized = JsonSerializer.Deserialize<TokenExpirationTimestampWrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Value.Seconds).IsEqualTo(original.Seconds);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  8. FreezeStatusConverter (wrapper needed)
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task FreezeStatus_Frozen_MapsTsSuspended()
    {
        var json = """{"Value":"FROZEN"}""";
        var result = JsonSerializer.Deserialize<FreezeWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(TokenTradableStatus.Suspended);
    }

    [Test]
    public async Task FreezeStatus_Unfrozen_MapsToTradable()
    {
        var json = """{"Value":"UNFROZEN"}""";
        var result = JsonSerializer.Deserialize<FreezeWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task FreezeStatus_NotApplicable_MapsToNotApplicable()
    {
        var json = """{"Value":"NOT_APPLICABLE"}""";
        var result = JsonSerializer.Deserialize<FreezeWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(TokenTradableStatus.NotApplicable);
    }

    [Test]
    public async Task FreezeStatus_Serialize_Suspended_WritesFrozen()
    {
        var wrapper = new FreezeWrapper(TokenTradableStatus.Suspended);
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).IsEqualTo("""{"Value":"FROZEN"}""");
    }

    [Test]
    public async Task FreezeStatus_Serialize_Tradable_WritesUnfrozen()
    {
        var wrapper = new FreezeWrapper(TokenTradableStatus.Tradable);
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).IsEqualTo("""{"Value":"UNFROZEN"}""");
    }

    [Test]
    public async Task FreezeStatus_Serialize_NotApplicable_WritesNotApplicable()
    {
        var wrapper = new FreezeWrapper(TokenTradableStatus.NotApplicable);
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).IsEqualTo("""{"Value":"NOT_APPLICABLE"}""");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  9. PauseStatusConverter (wrapper needed)
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task PauseStatus_Paused_MapsToSuspended()
    {
        var json = """{"Value":"PAUSED"}""";
        var result = JsonSerializer.Deserialize<PauseWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(TokenTradableStatus.Suspended);
    }

    [Test]
    public async Task PauseStatus_Unpaused_MapsToTradable()
    {
        var json = """{"Value":"UNPAUSED"}""";
        var result = JsonSerializer.Deserialize<PauseWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task PauseStatus_NotApplicable_MapsToNotApplicable()
    {
        var json = """{"Value":"NOT_APPLICABLE"}""";
        var result = JsonSerializer.Deserialize<PauseWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(TokenTradableStatus.NotApplicable);
    }

    [Test]
    public async Task PauseStatus_Serialize_Suspended_WritesPaused()
    {
        var wrapper = new PauseWrapper(TokenTradableStatus.Suspended);
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).IsEqualTo("""{"Value":"PAUSED"}""");
    }

    [Test]
    public async Task PauseStatus_Serialize_Tradable_WritesUnpaused()
    {
        var wrapper = new PauseWrapper(TokenTradableStatus.Tradable);
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).IsEqualTo("""{"Value":"UNPAUSED"}""");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  10. TokenKycStatusConverter (wrapper needed)
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task TokenKycStatus_Granted_MapsToGranted()
    {
        var json = """{"Value":"GRANTED"}""";
        var result = JsonSerializer.Deserialize<KycWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(TokenKycStatus.Granted);
    }

    [Test]
    public async Task TokenKycStatus_Revoked_MapsToRevoked()
    {
        var json = """{"Value":"REVOKED"}""";
        var result = JsonSerializer.Deserialize<KycWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(TokenKycStatus.Revoked);
    }

    [Test]
    public async Task TokenKycStatus_NotApplicable_MapsToNotApplicable()
    {
        var json = """{"Value":"NOT_APPLICABLE"}""";
        var result = JsonSerializer.Deserialize<KycWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(TokenKycStatus.NotApplicable);
    }

    [Test]
    public async Task TokenKycStatus_Serialize_Granted_WritesGranted()
    {
        var wrapper = new KycWrapper(TokenKycStatus.Granted);
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).IsEqualTo("""{"Value":"GRANTED"}""");
    }

    [Test]
    public async Task TokenKycStatus_Serialize_Revoked_WritesRevoked()
    {
        var wrapper = new KycWrapper(TokenKycStatus.Revoked);
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).IsEqualTo("""{"Value":"REVOKED"}""");
    }

    [Test]
    public async Task TokenKycStatus_Serialize_NotApplicable_WritesNotApplicable()
    {
        var wrapper = new KycWrapper(TokenKycStatus.NotApplicable);
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).IsEqualTo("""{"Value":"NOT_APPLICABLE"}""");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  11. TokenTypeConverter (has [JsonConverter] on type)
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task TokenType_FungibleCommon_MapsToFungible()
    {
        var json = "\"FUNGIBLE_COMMON\"";
        var result = JsonSerializer.Deserialize<TokenType>(json);
        await Assert.That(result).IsEqualTo(TokenType.Fungible);
    }

    [Test]
    public async Task TokenType_NonFungibleUnique_MapsToNonFungible()
    {
        var json = "\"NON_FUNGIBLE_UNIQUE\"";
        var result = JsonSerializer.Deserialize<TokenType>(json);
        await Assert.That(result).IsEqualTo(TokenType.NonFungible);
    }

    [Test]
    public async Task TokenType_Serialize_Fungible_WritesFungibleCommon()
    {
        var json = JsonSerializer.Serialize(TokenType.Fungible);
        await Assert.That(json).IsEqualTo("\"FUNGIBLE_COMMON\"");
    }

    [Test]
    public async Task TokenType_Serialize_NonFungible_WritesNonFungibleUnique()
    {
        var json = JsonSerializer.Serialize(TokenType.NonFungible);
        await Assert.That(json).IsEqualTo("\"NON_FUNGIBLE_UNIQUE\"");
    }

    [Test]
    public async Task TokenType_RoundTrip_PreservesFungible()
    {
        var json = JsonSerializer.Serialize(TokenType.Fungible);
        var deserialized = JsonSerializer.Deserialize<TokenType>(json);
        await Assert.That(deserialized).IsEqualTo(TokenType.Fungible);
    }

    [Test]
    public async Task TokenType_RoundTrip_PreservesNonFungible()
    {
        var json = JsonSerializer.Serialize(TokenType.NonFungible);
        var deserialized = JsonSerializer.Deserialize<TokenType>(json);
        await Assert.That(deserialized).IsEqualTo(TokenType.NonFungible);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  12. TransactionIdMirrorConverter (wrapper needed)
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task TransactionIdMirror_Deserialize_ParsesDashSeparatedFormat()
    {
        var json = """{"Value":"0.0.5-1234567890-123456789"}""";
        var result = JsonSerializer.Deserialize<TxIdMirrorWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsNotNull();
        await Assert.That(result.Value.Payer).IsEqualTo(new EntityId(0, 0, 5));
        await Assert.That(result.Value.ValidStartSeconds).IsEqualTo(1234567890L);
        await Assert.That(result.Value.ValidStartNanos).IsEqualTo(123456789);
    }

    [Test]
    public async Task TransactionIdMirror_Serialize_ProducesDashSeparatedFormat()
    {
        var payer = new EntityId(0, 0, 5);
        var txId = new TransactionId(payer, 1234567890L, 123456789);
        var wrapper = new TxIdMirrorWrapper(txId);
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).IsEqualTo("""{"Value":"0.0.5-1234567890-123456789"}""");
    }

    [Test]
    public async Task TransactionIdMirror_Serialize_PadsNanosToNineDigits()
    {
        var payer = new EntityId(0, 0, 10);
        var txId = new TransactionId(payer, 1000000000L, 1);
        var wrapper = new TxIdMirrorWrapper(txId);
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).IsEqualTo("""{"Value":"0.0.10-1000000000-000000001"}""");
    }

    [Test]
    public async Task TransactionIdMirror_RoundTrip_PreservesValue()
    {
        var payer = new EntityId(0, 0, (long)Generator.Integer(1, 9999));
        var seconds = (long)Generator.Integer(1000000000, 2000000000);
        var nanos = Generator.Integer(0, 999999999);
        var txId = new TransactionId(payer, seconds, nanos);
        var original = new TxIdMirrorWrapper(txId);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<TxIdMirrorWrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Value.Payer).IsEqualTo(payer);
        await Assert.That(deserialized.Value.ValidStartSeconds).IsEqualTo(seconds);
        await Assert.That(deserialized.Value.ValidStartNanos).IsEqualTo(nanos);
    }

    [Test]
    public async Task TransactionIdMirror_Deserialize_EmptyString_ReturnsNone()
    {
        var json = """{"Value":""}""";
        var result = JsonSerializer.Deserialize<TxIdMirrorWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(TransactionId.None);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  13. EncodedParamsConverter (has [JsonConverter] on type)
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task EncodedParams_Deserialize_ParsesHexPrefixedString()
    {
        var json = "\"0xdeadbeef\"";
        var result = JsonSerializer.Deserialize<EncodedParams>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Data.ToArray()).IsEquivalentTo(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
    }

    [Test]
    public async Task EncodedParams_Serialize_ProducesHexPrefixedString()
    {
        var data = new EncodedParams(new ReadOnlyMemory<byte>(new byte[] { 0xCA, 0xFE, 0xBA, 0xBE }));
        var json = JsonSerializer.Serialize(data);
        await Assert.That(json).IsEqualTo("\"0xcafebabe\"");
    }

    [Test]
    public async Task EncodedParams_RoundTrip_PreservesData()
    {
        var original = new EncodedParams(new ReadOnlyMemory<byte>(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }));
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<EncodedParams>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Data.ToArray()).IsEquivalentTo(original.Data.ToArray());
    }

    [Test]
    public async Task EncodedParams_EmptyData_RoundTrips()
    {
        var original = new EncodedParams(ReadOnlyMemory<byte>.Empty);
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<EncodedParams>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Data.Length).IsEqualTo(0);
    }
}
