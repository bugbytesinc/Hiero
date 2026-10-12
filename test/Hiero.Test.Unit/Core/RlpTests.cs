// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8625 // Null arguments are intentional in these tests
using Hiero.Test.Helpers;
using System.Numerics;
using System.Text;

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
        await Assert.That(Convert.ToHexStringLower(result!)).IsEqualTo(Convert.ToHexStringLower(input));
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
        await Assert.That(Convert.ToHexStringLower(rencoded)).IsEqualTo(Convert.ToHexStringLower(encoded));
        byte[] rerencoded = Rlp.Encode(output[0]);
        await Assert.That(Convert.ToHexStringLower(rerencoded)).IsEqualTo(Convert.ToHexStringLower(encoded));
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

    [Test]
    public async Task Can_Decode_Multiple_Top_Level_Items()
    {
        byte[] encoded = [0x01, 0x02, 0x80, 0xc0];

        object[] output = Rlp.Decode(encoded);

        await Assert.That(output.Length).IsEqualTo(4);
        await Assert.That(((byte[])output[0])[0]).IsEqualTo((byte)0x01);
        await Assert.That(((byte[])output[1])[0]).IsEqualTo((byte)0x02);
        await Assert.That((byte[])output[2]).IsEmpty();
        await Assert.That((object[])output[3]).IsEmpty();
    }

    [Test]
    [Arguments(new byte[] { 0xBB, 0xFF, 0xFF, 0xFF, 0xFB })]
    [Arguments(new byte[] { 0xFB, 0xFF, 0xFF, 0xFF, 0xFB })]
    public async Task A_Length_That_Overflows_Fails_Instead_Of_Hanging(byte[] data)
    {
        // These lengths once read as negative numbers, leaving the decoder
        // spinning in place forever.
        var decode = Task.Run(() => Rlp.Decode(data));
        await Assert.ThrowsAsync<FormatException>(async () => await decode.WaitAsync(TimeSpan.FromSeconds(10)));
    }

    [Test]
    [Arguments(new byte[] { 0x83, 0x61 })]
    [Arguments(new byte[] { 0xC3, 0x01 })]
    [Arguments(new byte[] { 0xB9, 0x01 })]
    [Arguments(new byte[] { 0xB8, 0x05, 0x61 })]
    public async Task Truncated_Input_Fails_With_A_Format_Error(byte[] data)
    {
        await Assert.ThrowsAsync<FormatException>(async () => await Task.Run(() => Rlp.Decode(data)));
    }

    [Test]
    public async Task A_Length_Prefix_Longer_Than_Four_Bytes_Is_Rejected()
    {
        byte[] data = [0xBC, 0x00, 0x00, 0x00, 0x00, 0x01, 0x61];
        await Assert.ThrowsAsync<FormatException>(async () => await Task.Run(() => Rlp.Decode(data)));
    }
}
