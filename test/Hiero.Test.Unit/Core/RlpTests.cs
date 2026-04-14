// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8625 // Null arguments are intentional in these tests
using System.Numerics;
using System.Text;
using Hiero.Test.Helpers;

namespace Hiero.Test.Unit.Core;

public class RlpTests
{
    [Test]
    public async Task Can_Encode_And_Decode_Null()
    {
        byte[] encoded = Rlp.Encode(null);
        await Assert.That(encoded.Length).IsEqualTo(1);
        await Assert.That(encoded[0]).IsEqualTo((byte)0x80);
        object[] output = Rlp.Decode(encoded);
        await Assert.That(output.Length).IsEqualTo(1);
        await Assert.That((byte[])output[0]).IsEmpty();
    }

    [Test]
    public async Task Can_Encode_And_Decode_Null_In_List()
    {
        byte[] encoded = Rlp.Encode([null]);
        await Assert.That(encoded.Length).IsEqualTo(1);
        await Assert.That(encoded[0]).IsEqualTo((byte)0x80);
        object[] output = Rlp.Decode(encoded);
        await Assert.That(output.Length).IsEqualTo(1);
        await Assert.That((byte[])output[0]).IsEmpty();
    }

    [Test]
    public async Task Can_Encode_And_Decode_Empty_List()
    {
        byte[] encoded = Rlp.Encode([]);
        await Assert.That(encoded.Length).IsEqualTo(1);
        await Assert.That(encoded[0]).IsEqualTo((byte)0xc0);
        object[] output = Rlp.Decode(encoded);
        await Assert.That(output.Length).IsEqualTo(1);
        await Assert.That((object[])output[0]).IsEmpty();
    }

    [Test]
    public async Task Can_Encode_And_Decode_Single_Byte()
    {
        byte[] input = [(byte)Generator.Integer(0, 100)];
        byte[] output = Rlp.Encode(input);
        await Assert.That(output.Length).IsEqualTo(1);
        await Assert.That(output[0]).IsEqualTo(input[0]);
    }

    [Test]
    public async Task Can_Encode_And_Decode_Large_Byte()
    {
        byte[] input = [(byte)Generator.Integer(200, 225)];
        byte[] encoded = Rlp.Encode(input);
        await Assert.That(encoded.Length).IsEqualTo(2);
        await Assert.That(encoded[1]).IsEqualTo(input[0]);
        object[] output = Rlp.Decode(encoded);
        await Assert.That(output.Length).IsEqualTo(1);
        await Assert.That(((byte[])output[0])[0]).IsEqualTo(input[0]);
    }

    [Test]
    public async Task Can_Encode_And_Decode_Small_Array()
    {
        byte[] input = Encoding.UTF8.GetBytes(Generator.Code(10));
        byte[] encoded = Rlp.Encode(input);
        object[] output = Rlp.Decode(encoded);
        await Assert.That(output.Length).IsEqualTo(1);
        var result = output[0] as byte[];
        await Assert.That(result).IsNotNull();
        await Assert.That(Hex.FromBytes(result!)).IsEqualTo(Hex.FromBytes(input));
    }

    [Test]
    public async Task Can_Encode_And_Decode_A_Structure()
    {
        object[] input = [
            new BigInteger(Generator.Integer(0, 1000)),
            Generator.Code(200),
            Generator.SHA384Hash(),
            0,
            "This is a test",
            new object[]{
                "Nested Stuff",
                Generator.Code(200),
            },
            100
        ];
        byte[] encoded = Rlp.Encode(input);
        object[] output = Rlp.Decode(encoded);
        byte[] rencoded = Rlp.Encode(output);
        await Assert.That(Hex.FromBytes(rencoded)).IsEqualTo(Hex.FromBytes(encoded));
        byte[] rerencoded = Rlp.Encode(output[0]);
        await Assert.That(Hex.FromBytes(rerencoded)).IsEqualTo(Hex.FromBytes(encoded));
    }

    [Test]
    public async Task Can_Encode_String()
    {
        byte[] encoded = Rlp.Encode("hello");
        object[] output = Rlp.Decode(encoded);
        await Assert.That(output.Length).IsEqualTo(1);
        await Assert.That(Encoding.UTF8.GetString((byte[])output[0])).IsEqualTo("hello");
    }

    [Test]
    public async Task Can_Encode_BigInteger()
    {
        var value = new BigInteger(256);
        byte[] encoded = Rlp.Encode(value);
        object[] output = Rlp.Decode(encoded);
        await Assert.That(output.Length).IsEqualTo(1);
        var decoded = new BigInteger((byte[])output[0], true, true);
        await Assert.That(decoded).IsEqualTo(value);
    }

    [Test]
    public async Task Can_Encode_Zero_Integer()
    {
        byte[] encoded = Rlp.Encode(0);
        await Assert.That(encoded.Length).IsEqualTo(1);
        await Assert.That(encoded[0]).IsEqualTo((byte)0x80);
    }

    [Test]
    public async Task Negative_Integer_Throws_Error()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            Rlp.Encode(new BigInteger(-1));
        });
    }

    [Test]
    public async Task Unsupported_Type_Throws_Error()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
        {
            Rlp.Encode(DateTime.UtcNow);
        });
        await Assert.That(exception.Message).Contains("Unable to RLP Encode value of type");
    }

    [Test]
    public async Task Can_Encode_Long()
    {
        byte[] encoded = Rlp.Encode(42L);
        object[] output = Rlp.Decode(encoded);
        await Assert.That(output.Length).IsEqualTo(1);
        await Assert.That(((byte[])output[0])[0]).IsEqualTo((byte)42);
    }

    [Test]
    public async Task Can_Encode_ULong()
    {
        byte[] encoded = Rlp.Encode(42UL);
        object[] output = Rlp.Decode(encoded);
        await Assert.That(output.Length).IsEqualTo(1);
        await Assert.That(((byte[])output[0])[0]).IsEqualTo((byte)42);
    }

    [Test]
    public async Task Can_Encode_ReadOnlyMemory_Bytes()
    {
        ReadOnlyMemory<byte> input = new byte[] { 1, 2, 3, 4, 5 };
        byte[] encoded = Rlp.Encode(input);
        object[] output = Rlp.Decode(encoded);
        await Assert.That(output.Length).IsEqualTo(1);
        await Assert.That(((byte[])output[0]).SequenceEqual(input.ToArray())).IsTrue();
    }

    [Test]
    public async Task Can_Encode_Large_Byte_Array()
    {
        byte[] input = new byte[60];
        Random.Shared.NextBytes(input);
        byte[] encoded = Rlp.Encode(input);
        object[] output = Rlp.Decode(encoded);
        await Assert.That(output.Length).IsEqualTo(1);
        await Assert.That(((byte[])output[0]).SequenceEqual(input)).IsTrue();
    }

    [Test]
    public async Task Can_Encode_Empty_Byte_Array()
    {
        byte[] encoded = Rlp.Encode(Array.Empty<byte>());
        await Assert.That(encoded.Length).IsEqualTo(1);
        await Assert.That(encoded[0]).IsEqualTo((byte)0x80);
    }
}
