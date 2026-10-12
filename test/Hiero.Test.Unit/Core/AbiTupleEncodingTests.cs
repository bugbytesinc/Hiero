// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using System.Numerics;

namespace Hiero.Test.Unit.Core;

/// <summary>
/// Tuple encodings checked against an independent ABI implementation
/// (eth-abi 5): a tuple is encoded in place unless a member is dynamic.
/// </summary>
public class AbiTupleEncodingTests
{
    private static readonly EntityId Address = new(0, 0, 0x1234);

    [Test]
    public async Task A_Static_Tuple_Is_Encoded_In_Place()
    {
        var encoded = Abi.EncodeArguments([new AbiTuple(Address, new BigInteger(7))]);
        await Assert.That(Convert.ToHexStringLower(encoded.Span)).IsEqualTo(
            "0000000000000000000000000000000000000000000000000000000000001234" +
            "0000000000000000000000000000000000000000000000000000000000000007");
    }

    [Test]
    public async Task Arguments_After_A_Static_Tuple_Stay_In_Place()
    {
        var encoded = Abi.EncodeArguments([new AbiTuple(Address, new BigInteger(7)), new BigInteger(9)]);
        await Assert.That(Convert.ToHexStringLower(encoded.Span)).IsEqualTo(
            "0000000000000000000000000000000000000000000000000000000000001234" +
            "0000000000000000000000000000000000000000000000000000000000000007" +
            "0000000000000000000000000000000000000000000000000000000000000009");
    }

    [Test]
    public async Task A_Tuple_With_A_Dynamic_Member_Is_Encoded_Behind_An_Offset()
    {
        var encoded = Abi.EncodeArguments([new AbiTuple("hi", new BigInteger(7)), new BigInteger(9)]);
        await Assert.That(Convert.ToHexStringLower(encoded.Span)).IsEqualTo(
            "0000000000000000000000000000000000000000000000000000000000000040" +
            "0000000000000000000000000000000000000000000000000000000000000009" +
            "0000000000000000000000000000000000000000000000000000000000000040" +
            "0000000000000000000000000000000000000000000000000000000000000007" +
            "0000000000000000000000000000000000000000000000000000000000000002" +
            "6869000000000000000000000000000000000000000000000000000000000000");
    }

    [Test]
    public async Task A_Nested_Static_Tuple_Is_Encoded_In_Place()
    {
        var encoded = Abi.EncodeArguments([new AbiTuple(new AbiTuple(BigInteger.One, new BigInteger(2)), true)]);
        await Assert.That(Convert.ToHexStringLower(encoded.Span)).IsEqualTo(
            "0000000000000000000000000000000000000000000000000000000000000001" +
            "0000000000000000000000000000000000000000000000000000000000000002" +
            "0000000000000000000000000000000000000000000000000000000000000001");
    }

    [Test]
    public async Task A_Tuple_Holding_A_Dynamic_Tuple_Is_Itself_Dynamic()
    {
        var encoded = Abi.EncodeArguments([new AbiTuple(new AbiTuple("ab", new BigInteger(3)), new BigInteger(4))]);
        await Assert.That(Convert.ToHexStringLower(encoded.Span)).IsEqualTo(
            "0000000000000000000000000000000000000000000000000000000000000020" +
            "0000000000000000000000000000000000000000000000000000000000000040" +
            "0000000000000000000000000000000000000000000000000000000000000004" +
            "0000000000000000000000000000000000000000000000000000000000000040" +
            "0000000000000000000000000000000000000000000000000000000000000003" +
            "0000000000000000000000000000000000000000000000000000000000000002" +
            "6162000000000000000000000000000000000000000000000000000000000000");
    }

    [Test]
    public async Task The_Selector_Names_The_Tuple_Members()
    {
        var encoded = Abi.EncodeFunctionWithArguments("f", [new AbiTuple(Address, new BigInteger(7)), new BigInteger(9)]);
        await Assert.That(Convert.ToHexStringLower(encoded.Span[..4])).IsEqualTo("5b54233b");
    }
}
