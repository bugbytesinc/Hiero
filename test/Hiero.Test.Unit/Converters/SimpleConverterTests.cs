// SPDX-License-Identifier: Apache-2.0
using System.Numerics;
using System.Text.Json;
using Hiero.Converters;

namespace Hiero.Test.Unit.Converters;

public class SimpleConverterTests
{
    // ── Wrapper records for converters that need per-property attribution ──

    private record Base64Wrapper(
        [property: System.Text.Json.Serialization.JsonConverter(typeof(Base64StringToBytesConverter))]
        ReadOnlyMemory<byte> Data);

    private record BigIntWrapper(
        [property: System.Text.Json.Serialization.JsonConverter(typeof(BigIntegerConverter))]
        BigInteger Value);

    private record HexBytesWrapper(
        [property: System.Text.Json.Serialization.JsonConverter(typeof(HexStringToBytesConverter))]
        ReadOnlyMemory<byte> Data);

    private record FeeLimitWrapper(
        [property: System.Text.Json.Serialization.JsonConverter(typeof(FeeLimitFromStringConverter))]
        long Value);

    private record BoolWrapper(
        [property: System.Text.Json.Serialization.JsonConverter(typeof(BooleanMirrorConverter))]
        bool Value);

    private record IntWrapper(
        [property: System.Text.Json.Serialization.JsonConverter(typeof(IntMirrorConverter))]
        int Value);

    private record LongWrapper(
        [property: System.Text.Json.Serialization.JsonConverter(typeof(LongMirrorConverter))]
        long Value);

    private record ULongWrapper(
        [property: System.Text.Json.Serialization.JsonConverter(typeof(UnsignedLongMirrorConverter))]
        ulong Value);

    private record DurationWrapper(
        [property: System.Text.Json.Serialization.JsonConverter(typeof(ValidDurationInSecondsConverter))]
        TimeSpan Value);

    // ═══════════════════════════════════════════════════════════════════════
    //  1. Base64StringToBytesConverter
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Base64_Read_DecodesBase64StringToBytes()
    {
        var json = """{"Data":"AQIDBA=="}""";
        var result = JsonSerializer.Deserialize<Base64Wrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Data.ToArray()).IsEquivalentTo(new byte[] { 1, 2, 3, 4 });
    }

    [Test]
    public async Task Base64_Write_EncodesBytesToBase64String()
    {
        var wrapper = new Base64Wrapper(new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4 }));
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).IsEqualTo("""{"Data":"AQIDBA=="}""");
    }

    [Test]
    public async Task Base64_RoundTrip_PreservesBytes()
    {
        var original = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0xCA, 0xFE };
        var wrapper = new Base64Wrapper(new ReadOnlyMemory<byte>(original));
        var json = JsonSerializer.Serialize(wrapper);
        var deserialized = JsonSerializer.Deserialize<Base64Wrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Data.ToArray()).IsEquivalentTo(original);
    }

    [Test]
    public async Task Base64_EmptyBytes_RoundTripsAsEmptyString()
    {
        var wrapper = new Base64Wrapper(ReadOnlyMemory<byte>.Empty);
        var json = JsonSerializer.Serialize(wrapper);
        var deserialized = JsonSerializer.Deserialize<Base64Wrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        var length = deserialized!.Data.Length;
        await Assert.That(length).IsEqualTo(0);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  2. BigIntegerConverter
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task BigInteger_Read_DecodesHexString()
    {
        var json = """{"Value":"0xdeadbeef"}""";
        var result = JsonSerializer.Deserialize<BigIntWrapper>(json);
        await Assert.That(result).IsNotNull();
        var expected = new BigInteger(0xDEADBEEF);
        await Assert.That(result!.Value).IsEqualTo(expected);
    }

    [Test]
    public async Task BigInteger_Write_EncodesToHexString()
    {
        var wrapper = new BigIntWrapper(new BigInteger(0xDEADBEEF));
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).IsEqualTo("""{"Value":"0xdeadbeef"}""");
    }

    [Test]
    public async Task BigInteger_Zero_WritesAs0x0()
    {
        var wrapper = new BigIntWrapper(BigInteger.Zero);
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).IsEqualTo("""{"Value":"0x0"}""");
    }

    [Test]
    public async Task BigInteger_Zero_ReadsFrom0x0()
    {
        var json = """{"Value":"0x0"}""";
        var result = JsonSerializer.Deserialize<BigIntWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(BigInteger.Zero);
    }

    [Test]
    public async Task BigInteger_RoundTrip_PreservesValue()
    {
        var original = BigInteger.Parse("123456789012345678901234567890");
        var wrapper = new BigIntWrapper(original);
        var json = JsonSerializer.Serialize(wrapper);
        var deserialized = JsonSerializer.Deserialize<BigIntWrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Value).IsEqualTo(original);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  3. HexStringToBytesConverter
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task HexBytes_Read_DecodesHexStringToBytes()
    {
        var json = """{"Data":"0xdeadbeef"}""";
        var result = JsonSerializer.Deserialize<HexBytesWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Data.ToArray()).IsEquivalentTo(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });
    }

    [Test]
    public async Task HexBytes_Write_EncodesBytesToHexString()
    {
        var wrapper = new HexBytesWrapper(new ReadOnlyMemory<byte>(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }));
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).IsEqualTo("""{"Data":"0xdeadbeef"}""");
    }

    [Test]
    public async Task HexBytes_RoundTrip_PreservesBytes()
    {
        var original = new byte[] { 0xCA, 0xFE, 0xBA, 0xBE };
        var wrapper = new HexBytesWrapper(new ReadOnlyMemory<byte>(original));
        var json = JsonSerializer.Serialize(wrapper);
        var deserialized = JsonSerializer.Deserialize<HexBytesWrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Data.ToArray()).IsEquivalentTo(original);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  4. FeeLimitFromStringConverter
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task FeeLimit_Read_ParsesStringToLong()
    {
        var json = """{"Value":"1000"}""";
        var result = JsonSerializer.Deserialize<FeeLimitWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(1000L);
    }

    [Test]
    public async Task FeeLimit_Write_SerializesLongToString()
    {
        var wrapper = new FeeLimitWrapper(1000L);
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).IsEqualTo("""{"Value":"1000"}""");
    }

    [Test]
    public async Task FeeLimit_RoundTrip_PreservesValue()
    {
        var wrapper = new FeeLimitWrapper(500_000_000L);
        var json = JsonSerializer.Serialize(wrapper);
        var deserialized = JsonSerializer.Deserialize<FeeLimitWrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Value).IsEqualTo(500_000_000L);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  5. BooleanMirrorConverter
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task BoolMirror_Read_JsonTrue_ReturnsTrue()
    {
        var json = """{"Value":true}""";
        var result = JsonSerializer.Deserialize<BoolWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsTrue();
    }

    [Test]
    public async Task BoolMirror_Read_StringTrue_ReturnsTrue()
    {
        var json = """{"Value":"true"}""";
        var result = JsonSerializer.Deserialize<BoolWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsTrue();
    }

    [Test]
    public async Task BoolMirror_Read_StringFalse_ReturnsFalse()
    {
        var json = """{"Value":"false"}""";
        var result = JsonSerializer.Deserialize<BoolWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsFalse();
    }

    [Test]
    public async Task BoolMirror_Write_True_WritesJsonBoolean()
    {
        var wrapper = new BoolWrapper(true);
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).IsEqualTo("""{"Value":true}""");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  6. IntMirrorConverter
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task IntMirror_Read_NumericValue_ReturnsInt()
    {
        var json = """{"Value":42}""";
        var result = JsonSerializer.Deserialize<IntWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(42);
    }

    [Test]
    public async Task IntMirror_Read_StringValue_ReturnsInt()
    {
        var json = """{"Value":"42"}""";
        var result = JsonSerializer.Deserialize<IntWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(42);
    }

    [Test]
    public async Task IntMirror_Read_NullValue_ReturnsZero()
    {
        var json = """{"Value":null}""";
        var result = JsonSerializer.Deserialize<IntWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(0);
    }

    [Test]
    public async Task IntMirror_RoundTrip_PreservesValue()
    {
        var wrapper = new IntWrapper(99);
        var json = JsonSerializer.Serialize(wrapper);
        var deserialized = JsonSerializer.Deserialize<IntWrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Value).IsEqualTo(99);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  7. LongMirrorConverter
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task LongMirror_Read_NumericValue_ReturnsLong()
    {
        var json = """{"Value":9876543210}""";
        var result = JsonSerializer.Deserialize<LongWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(9876543210L);
    }

    [Test]
    public async Task LongMirror_Read_StringValue_ReturnsLong()
    {
        var json = """{"Value":"9876543210"}""";
        var result = JsonSerializer.Deserialize<LongWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(9876543210L);
    }

    [Test]
    public async Task LongMirror_Read_NullValue_ReturnsZero()
    {
        var json = """{"Value":null}""";
        var result = JsonSerializer.Deserialize<LongWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(0L);
    }

    [Test]
    public async Task LongMirror_RoundTrip_PreservesValue()
    {
        var wrapper = new LongWrapper(long.MaxValue);
        var json = JsonSerializer.Serialize(wrapper);
        var deserialized = JsonSerializer.Deserialize<LongWrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Value).IsEqualTo(long.MaxValue);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  8. UnsignedLongMirrorConverter
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task ULongMirror_Read_NumericValue_ReturnsULong()
    {
        var json = """{"Value":12345678901234}""";
        var result = JsonSerializer.Deserialize<ULongWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(12345678901234UL);
    }

    [Test]
    public async Task ULongMirror_Read_StringValue_ReturnsULong()
    {
        var json = """{"Value":"12345678901234"}""";
        var result = JsonSerializer.Deserialize<ULongWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(12345678901234UL);
    }

    [Test]
    public async Task ULongMirror_Read_NullValue_ReturnsZero()
    {
        var json = """{"Value":null}""";
        var result = JsonSerializer.Deserialize<ULongWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(0UL);
    }

    [Test]
    public async Task ULongMirror_RoundTrip_PreservesValue()
    {
        var wrapper = new ULongWrapper(ulong.MaxValue);
        var json = JsonSerializer.Serialize(wrapper);
        var deserialized = JsonSerializer.Deserialize<ULongWrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Value).IsEqualTo(ulong.MaxValue);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  9. ValidDurationInSecondsConverter
    // ═══════════════════════════════════════════════════════════════════════

    [Test]
    public async Task Duration_Read_ParsesStringToTimeSpan()
    {
        var json = """{"Value":"120"}""";
        var result = JsonSerializer.Deserialize<DurationWrapper>(json);
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value).IsEqualTo(TimeSpan.FromSeconds(120));
    }

    [Test]
    public async Task Duration_Write_SerializesTimeSpanToSecondsString()
    {
        var wrapper = new DurationWrapper(TimeSpan.FromSeconds(120));
        var json = JsonSerializer.Serialize(wrapper);
        await Assert.That(json).IsEqualTo("""{"Value":"120"}""");
    }

    [Test]
    public async Task Duration_RoundTrip_PreservesWholeSeconds()
    {
        var original = TimeSpan.FromSeconds(3600);
        var wrapper = new DurationWrapper(original);
        var json = JsonSerializer.Serialize(wrapper);
        var deserialized = JsonSerializer.Deserialize<DurationWrapper>(json);
        await Assert.That(deserialized).IsNotNull();
        await Assert.That(deserialized!.Value).IsEqualTo(original);
    }
}
