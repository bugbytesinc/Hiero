// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8600, CS8602, CS8604
using System.Security.Cryptography;

namespace Hiero.Test.Unit.Core;

public class EvmHashTests
{
    private static byte[] RandomHashBytes()
    {
        var buf = new byte[32];
        RandomNumberGenerator.Fill(buf);
        return buf;
    }

    [Test]
    public async Task Equivalent_EvmHashes_Are_Considered_Equal()
    {
        var bytes = RandomHashBytes();
        var hash1 = new EvmHash(bytes);
        var hash2 = new EvmHash(bytes);
        await Assert.That(hash1).IsEqualTo(hash2);
        await Assert.That(hash1 == hash2).IsTrue();
        await Assert.That(hash1 != hash2).IsFalse();
        await Assert.That(hash1.Equals(hash2)).IsTrue();
        await Assert.That(hash2.Equals(hash1)).IsTrue();
        await Assert.That(null as EvmHash == null as EvmHash).IsTrue();
        await Assert.That(EvmHash.None.Equals(EvmHash.None)).IsTrue();
    }

    [Test]
    public async Task Dissimilar_EvmHashes_Are_Not_Considered_Equal()
    {
        var hash1 = new EvmHash(RandomHashBytes());
        var hash2 = new EvmHash(RandomHashBytes());
        await Assert.That(hash1).IsNotEqualTo(hash2);
        await Assert.That(hash1 == hash2).IsFalse();
        await Assert.That(hash1 != hash2).IsTrue();
    }

    [Test]
    public async Task Comparing_With_Null_Is_Not_Considered_Equal()
    {
        object asNull = null;
        var hash = new EvmHash(RandomHashBytes());
        await Assert.That(hash == null).IsFalse();
        await Assert.That(null == hash).IsFalse();
        await Assert.That(hash != null).IsTrue();
        await Assert.That(hash.Equals(null as EvmHash)).IsFalse();
        await Assert.That(hash.Equals(asNull)).IsFalse();
        await Assert.That(hash.Equals(EvmHash.None)).IsFalse();
    }

    [Test]
    public async Task Other_Objects_Are_Not_Considered_Equal()
    {
        var hash = new EvmHash(RandomHashBytes());
        await Assert.That(hash.Equals("Something that is not an EvmHash")).IsFalse();
    }

    [Test]
    public async Task Cast_As_Object_Is_Considered_Equal()
    {
        var bytes = RandomHashBytes();
        var hash = new EvmHash(bytes);
        object equivalent = new EvmHash(bytes);
        await Assert.That(hash.Equals(equivalent)).IsTrue();
        await Assert.That(equivalent.Equals(hash)).IsTrue();
    }

    [Test]
    public async Task Reference_Equal_Is_Considered_Equal()
    {
        var hash = new EvmHash(RandomHashBytes());
        object reference = hash;
        await Assert.That(hash.Equals(reference)).IsTrue();
        await Assert.That(reference.Equals(hash)).IsTrue();
    }

    [Test]
    public async Task Equal_EvmHashes_Have_Equal_HashCodes()
    {
        var bytes = RandomHashBytes();
        var hash1 = new EvmHash(bytes);
        var hash2 = new EvmHash(bytes);
        await Assert.That(hash1.GetHashCode()).IsEqualTo(hash2.GetHashCode());
    }

    [Test]
    public async Task Dissimilar_EvmHashes_Have_Different_HashCodes()
    {
        var hash1 = new EvmHash(RandomHashBytes());
        var hash2 = new EvmHash(RandomHashBytes());
        await Assert.That(hash1.GetHashCode()).IsNotEqualTo(hash2.GetHashCode());
    }

    [Test]
    public async Task None_Is_All_Zeros()
    {
        var none = EvmHash.None;
        await Assert.That(none.Bytes.Length).IsEqualTo(32);
        await Assert.That(none.Bytes.ToArray().All(b => b == 0)).IsTrue();
    }

    [Test]
    public async Task Bytes_Property_Returns_Correct_Bytes()
    {
        var bytes = RandomHashBytes();
        var hash = new EvmHash(bytes);
        await Assert.That(hash.Bytes.ToArray().SequenceEqual(bytes)).IsTrue();
    }

    [Test]
    public async Task Constructor_With_Short_Length_Throws_Error()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new EvmHash(new byte[31]);
        });
        await Assert.That(exception.ParamName).IsEqualTo("bytes");
        await Assert.That(exception.Message).StartsWith("The encoded bytes must have a length of 32.");
    }

    [Test]
    public async Task Constructor_With_Long_Length_Throws_Error()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            new EvmHash(new byte[48]); // SHA-384 length, a common mistake
        });
        await Assert.That(exception.ParamName).IsEqualTo("bytes");
    }

    [Test]
    public async Task ToString_Returns_Zero_x_Prefixed_64_Hex_Chars()
    {
        var hash = new EvmHash(RandomHashBytes());
        var result = hash.ToString();
        await Assert.That(result).StartsWith("0x");
        await Assert.That(result.Length).IsEqualTo(66);
    }

    [Test]
    public async Task ToString_Is_Lowercase_Hex()
    {
        var hash = new EvmHash(RandomHashBytes());
        var result = hash.ToString();
        var body = result[2..];
        await Assert.That(body.ToLowerInvariant()).IsEqualTo(body);
    }

    [Test]
    public async Task TryParse_Round_Trips_Through_ToString_With_Prefix()
    {
        var hash = new EvmHash(RandomHashBytes());
        var success = EvmHash.TryParse(hash.ToString(), out var parsed);
        await Assert.That(success).IsTrue();
        await Assert.That(parsed).IsEqualTo(hash);
    }

    [Test]
    public async Task TryParse_Accepts_String_Without_Prefix()
    {
        var hash = new EvmHash(RandomHashBytes());
        var stripped = hash.ToString()[2..];
        var success = EvmHash.TryParse(stripped, out var parsed);
        await Assert.That(success).IsTrue();
        await Assert.That(parsed).IsEqualTo(hash);
    }

    [Test]
    public async Task TryParse_Invalid_String_Returns_False()
    {
        var success = EvmHash.TryParse("not-a-hash", out var parsed);
        await Assert.That(success).IsFalse();
        await Assert.That(parsed).IsNull();
    }

    [Test]
    public async Task TryParse_Null_String_Returns_False()
    {
        var success = EvmHash.TryParse(null as string, out var parsed);
        await Assert.That(success).IsFalse();
        await Assert.That(parsed).IsNull();
    }

    [Test]
    public async Task TryParse_Wrong_Length_Returns_False()
    {
        var success = EvmHash.TryParse("0x1234", out var parsed);
        await Assert.That(success).IsFalse();
        await Assert.That(parsed).IsNull();
    }

    [Test]
    public async Task Implicit_Operator_From_ReadOnlyMemory_Bytes()
    {
        ReadOnlyMemory<byte> bytes = RandomHashBytes();
        EvmHash hash = bytes;
        await Assert.That(hash).IsNotNull();
        await Assert.That(hash.Bytes.ToArray().SequenceEqual(bytes.ToArray())).IsTrue();
    }

    [Test]
    public async Task Implicit_Operator_From_ReadOnlySpan_Bytes()
    {
        var raw = RandomHashBytes();
        EvmHash hash = raw.AsSpan();
        await Assert.That(hash).IsNotNull();
        await Assert.That(hash.Bytes.ToArray().SequenceEqual(raw)).IsTrue();
    }
}
