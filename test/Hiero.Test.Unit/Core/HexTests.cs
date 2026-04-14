// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Test.Unit.Core;

public class HexTests
{
    [Test]
    public async Task RoundTrip_EncodeThenDecode_ReturnsOriginalBytes()
    {
        var original = new byte[] { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF };
        var hex = Hex.FromBytes(new ReadOnlyMemory<byte>(original));
        var decoded = Hex.ToBytes(hex);
        await Assert.That(decoded.ToArray()).IsEquivalentTo(original);
    }

    [Test]
    public async Task FromBytes_KnownValue_ReturnsExpectedHex()
    {
        var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        var result = Hex.FromBytes(new ReadOnlyMemory<byte>(bytes));
        await Assert.That(result).IsEqualTo("deadbeef");
    }

    [Test]
    public async Task FromBytes_EmptyBytes_ReturnsEmptyString()
    {
        var result = Hex.FromBytes(ReadOnlyMemory<byte>.Empty);
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ToBytes_EmptyString_ReturnsEmptyMemory()
    {
        var result = Hex.ToBytes(string.Empty);
        var length = result.Length;
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task ToBytes_NullString_ThrowsArgumentNullException()
    {
        string? input = null;
        var ex = Assert.Throws<ArgumentNullException>(() => { Hex.ToBytes(input!); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task ToBytes_Whitespace_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => { Hex.ToBytes("  "); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task ToBytes_OddLengthString_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => { Hex.ToBytes("abc"); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task ToBytes_InvalidHexChars_ThrowsArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => { Hex.ToBytes("zzzz"); });
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task FromBytes_ByteArrayOverload_Works()
    {
        var bytes = new byte[] { 0xCA, 0xFE };
        var result = Hex.FromBytes(bytes);
        await Assert.That(result).IsEqualTo("cafe");
    }

    [Test]
    public async Task FromBytes_ReadOnlySpanOverload_Works()
    {
        var bytes = new byte[] { 0xBA, 0xBE };
        var result = Hex.FromBytes(new ReadOnlySpan<byte>(bytes));
        await Assert.That(result).IsEqualTo("babe");
    }

    [Test]
    public async Task TryDecode_ValidHex_ReturnsTrue()
    {
        var hex = "deadbeef";
        var buffer = new byte[4];
        var result = Hex.TryDecode(hex.AsSpan(), buffer, out var bytesWritten);
        await Assert.That(result).IsTrue();
        var expectedBytesWritten = 4;
        await Assert.That(bytesWritten).IsEqualTo(expectedBytesWritten);
    }

    [Test]
    public async Task TryDecode_InvalidHex_ReturnsFalse()
    {
        var hex = "zzzz";
        var buffer = new byte[2];
        var result = Hex.TryDecode(hex.AsSpan(), buffer, out _);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TryDecode_BufferTooSmall_ReturnsFalse()
    {
        var hex = "deadbeef";
        var buffer = new byte[1];
        var result = Hex.TryDecode(hex.AsSpan(), buffer, out _);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TryEncode_ValidBuffer_ReturnsTrue()
    {
        var bytes = new byte[] { 0xDE, 0xAD };
        var charBuffer = new char[4];
        var result = Hex.TryEncode(new ReadOnlySpan<byte>(bytes), charBuffer, out var charsWritten);
        await Assert.That(result).IsTrue();
        var expectedCharsWritten = 4;
        await Assert.That(charsWritten).IsEqualTo(expectedCharsWritten);
    }

    [Test]
    public async Task TryEncode_BufferTooSmall_ReturnsFalse()
    {
        var bytes = new byte[] { 0xDE, 0xAD };
        var charBuffer = new char[1];
        var result = Hex.TryEncode(new ReadOnlySpan<byte>(bytes), charBuffer, out _);
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ToBytes_UppercaseHex_DecodesCaseInsensitively()
    {
        var result = Hex.ToBytes("DEADBEEF");
        var expected = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        await Assert.That(result.ToArray()).IsEquivalentTo(expected);
    }
}
