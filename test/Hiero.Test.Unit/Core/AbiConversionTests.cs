// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Hiero.Test.Helpers;
using System.Numerics;

namespace Hiero.Test.Unit.Core;

public class AbiConversionTests
{
    [Test]
    public async Task Can_Pack_EntityId_To_UInt160()
    {
        var address = new EntityId(2, 1, 3);
        var bytes = Abi.EncodeArguments(new[] { address });
        var hex = Hex.FromBytes(bytes)[^40..^0];
        await Assert.That(hex).IsEqualTo("0000000200000000000000010000000000000003");
    }

    [Test]
    public async Task Can_Pack_And_Unpack_EntityId()
    {
        var shard = Generator.Integer(1, 50);
        var realm = Generator.Integer(1, 50);
        var num = Generator.Integer(1, 50);
        var expected = new EntityId(shard, realm, num);
        var bytes = Abi.EncodeArguments(new[] { expected });
        var decoded = Abi.DecodeArguments(bytes, typeof(EntityId));
        await Assert.That(decoded).Count().IsEqualTo(1);
        var actual = decoded[0] as EntityId;
        await Assert.That(actual).IsNotNull();
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task Can_Pack_And_Unpack_EntityId_Array()
    {
        var expected = Enumerable.Range(3, Generator.Integer(4, 10)).Select(_ =>
        {
            var shard = Generator.Integer(1, 50);
            var realm = Generator.Integer(1, 50);
            var num = Generator.Integer(1, 50);
            return new EntityId(shard, realm, num);
        }).ToArray();
        var bytes = Abi.EncodeArguments(new[] { expected });
        var decoded = Abi.DecodeArguments(bytes, typeof(EntityId[]));
        await Assert.That(decoded).Count().IsEqualTo(1);
        var actual = decoded[0] as EntityId[];
        await Assert.That(actual).IsNotNull();
        await Assert.That(actual!.Length).IsEqualTo(expected.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            await Assert.That(actual[i]).IsEqualTo(expected[i]);
        }
    }

    // --- Gap coverage tests ---

    [Test]
    public async Task Null_Or_Empty_Args_Returns_Empty()
    {
        var nullResult = Abi.EncodeArguments(null!);
        var emptyResult = Abi.EncodeArguments([]);
        await Assert.That(nullResult.Length).IsEqualTo(0);
        await Assert.That(emptyResult.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Empty_Types_Returns_Empty_Array()
    {
        var data = new byte[32];
        var result = Abi.DecodeArguments(data, null!);
        await Assert.That(result).IsEmpty();
        var result2 = Abi.DecodeArguments(data);
        await Assert.That(result2).IsEmpty();
    }

    [Test]
    public async Task Can_Pack_And_Unpack_Bool()
    {
        var bytesTrue = Abi.EncodeArguments(new object[] { true });
        var bytesFalse = Abi.EncodeArguments(new object[] { false });
        var decodedTrue = Abi.DecodeArguments(bytesTrue, typeof(bool));
        var decodedFalse = Abi.DecodeArguments(bytesFalse, typeof(bool));
        await Assert.That((bool)decodedTrue[0]).IsTrue();
        await Assert.That((bool)decodedFalse[0]).IsFalse();
    }

    [Test]
    public async Task Can_Pack_And_Unpack_Int32()
    {
        var expected = Generator.Integer(1, 100_000);
        var bytes = Abi.EncodeArguments(new object[] { expected });
        var decoded = Abi.DecodeArguments(bytes, typeof(int));
        await Assert.That((int)decoded[0]).IsEqualTo(expected);
    }

    [Test]
    public async Task Can_Pack_And_Unpack_Int64()
    {
        var expected = (long)Generator.Integer(1, 100_000) * 100_000;
        var bytes = Abi.EncodeArguments(new object[] { expected });
        var decoded = Abi.DecodeArguments(bytes, typeof(long));
        await Assert.That((long)decoded[0]).IsEqualTo(expected);
    }

    [Test]
    public async Task Can_Pack_And_Unpack_String()
    {
        var expected = "Hello, Hiero ABI!";
        var bytes = Abi.EncodeArguments(new object[] { expected });
        var decoded = Abi.DecodeArguments(bytes, typeof(string));
        await Assert.That((string)decoded[0]).IsEqualTo(expected);
    }

    [Test]
    public async Task Can_Pack_And_Unpack_Byte_Array()
    {
        var expected = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03 };
        var bytes = Abi.EncodeArguments(new object[] { expected });
        var decoded = Abi.DecodeArguments(bytes, typeof(byte[]));
        var actual = (byte[])decoded[0];
        await Assert.That(actual.SequenceEqual(expected)).IsTrue();
    }

    [Test]
    public async Task Can_Pack_And_Unpack_BigInteger()
    {
        var expected = BigInteger.Parse("123456789012345678901234567890");
        var bytes = Abi.EncodeArguments(new object[] { expected });
        var decoded = Abi.DecodeArguments(bytes, typeof(BigInteger));
        await Assert.That((BigInteger)decoded[0]).IsEqualTo(expected);
    }

    [Test]
    public async Task Can_Pack_And_Unpack_BigInteger_Array()
    {
        var expected = new BigInteger[] { 100, 200, 300, BigInteger.Parse("999999999999999999") };
        var bytes = Abi.EncodeArguments(new object[] { expected });
        var decoded = Abi.DecodeArguments(bytes, typeof(BigInteger[]));
        var actual = (BigInteger[])decoded[0];
        await Assert.That(actual.SequenceEqual(expected)).IsTrue();
    }

    [Test]
    public async Task Can_Pack_And_Unpack_EvmAddress()
    {
        var evmBytes = new byte[20];
        evmBytes[0] = 0x12;
        evmBytes[19] = 0x34;
        var expected = new EvmAddress(evmBytes);
        var bytes = Abi.EncodeArguments(new object[] { expected });
        var decoded = Abi.DecodeArguments(bytes, typeof(EvmAddress));
        var actual = (EvmAddress)decoded[0];
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task Can_Pack_And_Unpack_EvmAddress_Array()
    {
        var expected = Enumerable.Range(0, 3).Select(i =>
        {
            var evmBytes = new byte[20];
            evmBytes[0] = (byte)(i + 1);
            evmBytes[19] = (byte)(i + 10);
            return new EvmAddress(evmBytes);
        }).ToArray();
        var bytes = Abi.EncodeArguments(new object[] { expected });
        var decoded = Abi.DecodeArguments(bytes, typeof(EvmAddress[]));
        var actual = (EvmAddress[])decoded[0];
        await Assert.That(actual.Length).IsEqualTo(expected.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            await Assert.That(actual[i]).IsEqualTo(expected[i]);
        }
    }

    [Test]
    public async Task Can_Pack_And_Unpack_Multiple_Arguments()
    {
        var address = new EntityId(0, 0, Generator.Integer(1, 50));
        var amount = (long)Generator.Integer(1000, 5000);
        var flag = true;
        var bytes = Abi.EncodeArguments(new object[] { address, amount, flag });
        var decoded = Abi.DecodeArguments(bytes, typeof(EntityId), typeof(long), typeof(bool));
        await Assert.That((EntityId)decoded[0]).IsEqualTo(address);
        await Assert.That((long)decoded[1]).IsEqualTo(amount);
        await Assert.That((bool)decoded[2]).IsTrue();
    }

    [Test]
    public async Task Unsupported_Type_Throws_InvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            Abi.EncodeArguments(new object[] { DateTime.UtcNow });
        });
        await Assert.That(exception.Message).Contains("DateTime");
    }

    [Test]
    public async Task EncodeFunctionWithArguments_Produces_Selector_Plus_Args()
    {
        var address = new EntityId(0, 0, 5);
        var amount = 1000L;
        var result = Abi.EncodeFunctionWithArguments("transfer", new object[] { address, amount });
        // First 4 bytes are Keccak256 function selector
        await Assert.That(result.Length).IsGreaterThan(4);
        // Remainder should match EncodeArguments output
        var argsOnly = Abi.EncodeArguments(new object[] { address, amount });
        var resultArgs = result.Slice(4);
        await Assert.That(resultArgs.ToArray().SequenceEqual(argsOnly.ToArray())).IsTrue();
    }

    [Test]
    public async Task Can_Pack_And_Unpack_UInt8()
    {
        byte expected = 42;
        var bytes = Abi.EncodeArguments(new object[] { expected });
        var decoded = Abi.DecodeArguments(bytes, typeof(byte));
        await Assert.That((byte)decoded[0]).IsEqualTo(expected);
    }

    [Test]
    public async Task Can_Pack_And_Unpack_UInt16()
    {
        ushort expected = 12345;
        var bytes = Abi.EncodeArguments(new object[] { expected });
        var decoded = Abi.DecodeArguments(bytes, typeof(ushort));
        await Assert.That((ushort)decoded[0]).IsEqualTo(expected);
    }

    [Test]
    public async Task Can_Pack_And_Unpack_UInt32()
    {
        uint expected = 3_000_000_000;
        var bytes = Abi.EncodeArguments(new object[] { expected });
        var decoded = Abi.DecodeArguments(bytes, typeof(uint));
        await Assert.That((uint)decoded[0]).IsEqualTo(expected);
    }

    [Test]
    public async Task Can_Pack_And_Unpack_UInt64()
    {
        ulong expected = 10_000_000_000UL;
        var bytes = Abi.EncodeArguments(new object[] { expected });
        var decoded = Abi.DecodeArguments(bytes, typeof(ulong));
        await Assert.That((ulong)decoded[0]).IsEqualTo(expected);
    }
}
