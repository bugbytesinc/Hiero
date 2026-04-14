// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Hiero.Test.Helpers;
using System.Numerics;

namespace Hiero.Test.Unit.Contract;

public class EncodedParamsTests
{
    // ── Constructor: ReadOnlyMemory<byte> ─────────────────────────────────────

    [Test]
    public async Task Constructor_From_Bytes_Stores_Data()
    {
        var raw = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var ep = new EncodedParams(raw.AsMemory());
        await Assert.That(ep.Size).IsEqualTo(4);
        await Assert.That(ep.Data.ToArray().SequenceEqual(raw)).IsTrue();
    }

    [Test]
    public async Task Constructor_From_Empty_Bytes_Produces_Empty()
    {
        var ep = new EncodedParams(ReadOnlyMemory<byte>.Empty);
        await Assert.That(ep.Size).IsEqualTo(0);
        await Assert.That(ep.Data.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Constructor_From_Bytes_Copies_Data_Not_Reference()
    {
        var raw = new byte[] { 0x10, 0x20, 0x30 };
        var ep = new EncodedParams(raw.AsMemory());
        // Mutate source array — internal copy should be unaffected
        raw[0] = 0xFF;
        await Assert.That(ep.Data.Span[0]).IsEqualTo((byte)0x10);
    }

    // ── Constructor: string (hex) ─────────────────────────────────────────────

    [Test]
    public async Task Constructor_From_Null_String_Produces_Empty()
    {
        var ep = new EncodedParams((string)null!);
        await Assert.That(ep.Size).IsEqualTo(0);
    }

    [Test]
    public async Task Constructor_From_Empty_String_Produces_Empty()
    {
        var ep = new EncodedParams(string.Empty);
        await Assert.That(ep.Size).IsEqualTo(0);
    }

    [Test]
    public async Task Constructor_From_Whitespace_String_Produces_Empty()
    {
        var ep = new EncodedParams("   ");
        await Assert.That(ep.Size).IsEqualTo(0);
    }

    [Test]
    public async Task Constructor_From_Hex_String_Decodes_Bytes()
    {
        // Encode a known int and get its hex representation
        var expected = 42;
        var encoded = Abi.EncodeArguments(new object[] { expected });
        var hex = Hex.FromBytes(encoded);
        var ep = new EncodedParams(hex);
        await Assert.That(ep.Size).IsEqualTo(encoded.Length);
        await Assert.That(ep.Data.ToArray().SequenceEqual(encoded.ToArray())).IsTrue();
    }

    [Test]
    public async Task Constructor_From_Hex_String_With_0x_Prefix_Decodes_Bytes()
    {
        var expected = 99;
        var encoded = Abi.EncodeArguments(new object[] { expected });
        var hex = "0x" + Hex.FromBytes(encoded);
        var ep = new EncodedParams(hex);
        await Assert.That(ep.Size).IsEqualTo(encoded.Length);
        await Assert.That(ep.Data.ToArray().SequenceEqual(encoded.ToArray())).IsTrue();
    }

    [Test]
    public async Task Constructor_From_Non_Hex_String_Falls_Back_To_Abi_Encoding()
    {
        // A plain ASCII string that is not valid hex should be ABI-encoded as a string
        var rawString = "Hello, World!";
        var ep = new EncodedParams(rawString);
        // Should be non-empty (ABI-encoded string is at least 64 bytes)
        await Assert.That(ep.Size).IsGreaterThan(0);
        // Decoding the result should recover the original string
        var decoded = ep.As<string>();
        await Assert.That(decoded).IsEqualTo(rawString);
    }

    [Test]
    public async Task Constructor_From_Non_Hex_String_With_0x_Still_Tries_Hex_Then_Falls_Back()
    {
        // "0x" followed by non-hex characters — Hex.TryDecode will fail, so it should ABI-encode the stripped portion
        var rawString = "0xNotHex!!";
        var ep = new EncodedParams(rawString);
        await Assert.That(ep.Size).IsGreaterThan(0);
    }

    // ── Size property ─────────────────────────────────────────────────────────

    [Test]
    public async Task Size_Reflects_Length_Of_Encoded_Data()
    {
        var encoded = Abi.EncodeArguments(new object[] { 1, 2L, true });
        var ep = new EncodedParams(encoded);
        await Assert.That(ep.Size).IsEqualTo(encoded.Length);
    }

    // ── Data property ─────────────────────────────────────────────────────────

    [Test]
    public async Task Data_Returns_ReadOnly_View_Of_Internal_Bytes()
    {
        var expected = Generator.Integer(1, 10_000);
        var encoded = Abi.EncodeArguments(new object[] { expected });
        var ep = new EncodedParams(encoded);
        var data = ep.Data;
        await Assert.That(data.Length).IsEqualTo(encoded.Length);
        await Assert.That(data.ToArray().SequenceEqual(encoded.ToArray())).IsTrue();
    }

    // ── As<T> (single argument) ───────────────────────────────────────────────

    [Test]
    public async Task As_Single_Int_Decodes_Correctly()
    {
        var expected = 42;
        var encoded = Abi.EncodeArguments(new object[] { expected });
        var ep = new EncodedParams(encoded);
        var result = ep.As<int>();
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task As_Single_Long_Decodes_Correctly()
    {
        var expected = (long)Generator.Integer(100_000, 999_999) * 1_000_000L;
        var encoded = Abi.EncodeArguments(new object[] { expected });
        var ep = new EncodedParams(encoded);
        await Assert.That(ep.As<long>()).IsEqualTo(expected);
    }

    [Test]
    public async Task As_Single_Bool_True_Decodes_Correctly()
    {
        var encoded = Abi.EncodeArguments(new object[] { true });
        var ep = new EncodedParams(encoded);
        await Assert.That(ep.As<bool>()).IsTrue();
    }

    [Test]
    public async Task As_Single_Bool_False_Decodes_Correctly()
    {
        var encoded = Abi.EncodeArguments(new object[] { false });
        var ep = new EncodedParams(encoded);
        await Assert.That(ep.As<bool>()).IsFalse();
    }

    [Test]
    public async Task As_Single_String_Decodes_Correctly()
    {
        var expected = Generator.Memo(5, 20);
        var encoded = Abi.EncodeArguments(new object[] { expected });
        var ep = new EncodedParams(encoded);
        await Assert.That(ep.As<string>()).IsEqualTo(expected);
    }

    [Test]
    public async Task As_Single_UInt64_Decodes_Correctly()
    {
        var expected = (ulong)Generator.Integer(1, 1_000_000) * 1_000_000UL;
        var encoded = Abi.EncodeArguments(new object[] { expected });
        var ep = new EncodedParams(encoded);
        await Assert.That(ep.As<ulong>()).IsEqualTo(expected);
    }

    [Test]
    public async Task As_Single_BigInteger_Decodes_Correctly()
    {
        var expected = BigInteger.Parse("987654321098765432109876543210");
        var encoded = Abi.EncodeArguments(new object[] { expected });
        var ep = new EncodedParams(encoded);
        await Assert.That(ep.As<BigInteger>()).IsEqualTo(expected);
    }

    [Test]
    public async Task As_Single_ByteArray_Decodes_Correctly()
    {
        var expected = new byte[] { 0xCA, 0xFE, 0xBA, 0xBE };
        var encoded = Abi.EncodeArguments(new object[] { expected });
        var ep = new EncodedParams(encoded);
        var result = ep.As<byte[]>();
        await Assert.That(result.SequenceEqual(expected)).IsTrue();
    }

    [Test]
    public async Task As_Single_EntityId_Decodes_Correctly()
    {
        var expected = new EntityId(0, 0, Generator.Integer(1, 100));
        var encoded = Abi.EncodeArguments(new object[] { expected });
        var ep = new EncodedParams(encoded);
        var result = ep.As<EntityId>();
        await Assert.That(result).IsEqualTo(expected);
    }

    // ── As<T1,T2> (two arguments) ─────────────────────────────────────────────

    [Test]
    public async Task As_Two_Args_Decodes_Both_Correctly()
    {
        var v1 = Generator.Integer(1, 500);
        var v2 = (long)Generator.Integer(1, 500) * 10_000L;
        var encoded = Abi.EncodeArguments(new object[] { v1, v2 });
        var ep = new EncodedParams(encoded);
        var (r1, r2) = ep.As<int, long>();
        await Assert.That(r1).IsEqualTo(v1);
        await Assert.That(r2).IsEqualTo(v2);
    }

    [Test]
    public async Task As_Two_Args_Int_And_Bool_Decodes_Correctly()
    {
        var v1 = Generator.Integer(1, 100);
        var v2 = true;
        var encoded = Abi.EncodeArguments(new object[] { v1, v2 });
        var ep = new EncodedParams(encoded);
        var (r1, r2) = ep.As<int, bool>();
        await Assert.That(r1).IsEqualTo(v1);
        await Assert.That(r2).IsTrue();
    }

    // ── As<T1,T2,T3> (three arguments) ───────────────────────────────────────

    [Test]
    public async Task As_Three_Args_Decodes_All_Correctly()
    {
        var v1 = Generator.Integer(1, 100);
        var v2 = (long)Generator.Integer(100, 1000);
        var v3 = Generator.Memo(3, 10);
        var encoded = Abi.EncodeArguments(new object[] { v1, v2, v3 });
        var ep = new EncodedParams(encoded);
        var (r1, r2, r3) = ep.As<int, long, string>();
        await Assert.That(r1).IsEqualTo(v1);
        await Assert.That(r2).IsEqualTo(v2);
        await Assert.That(r3).IsEqualTo(v3);
    }

    [Test]
    public async Task As_Three_Args_Int_Bool_ULong_Decodes_Correctly()
    {
        var v1 = Generator.Integer(1, 999);
        var v2 = false;
        var v3 = (ulong)Generator.Integer(1, 100_000);
        var encoded = Abi.EncodeArguments(new object[] { v1, v2, v3 });
        var ep = new EncodedParams(encoded);
        var (r1, r2, r3) = ep.As<int, bool, ulong>();
        await Assert.That(r1).IsEqualTo(v1);
        await Assert.That(r2).IsFalse();
        await Assert.That(r3).IsEqualTo(v3);
    }

    // ── As<T1,T2,T3,T4> (four arguments) ─────────────────────────────────────

    [Test]
    public async Task As_Four_Args_Decodes_All_Correctly()
    {
        var v1 = Generator.Integer(1, 100);
        var v2 = (long)Generator.Integer(1, 100);
        var v3 = true;
        var v4 = (ulong)Generator.Integer(1, 100);
        var encoded = Abi.EncodeArguments(new object[] { v1, v2, v3, v4 });
        var ep = new EncodedParams(encoded);
        var (r1, r2, r3, r4) = ep.As<int, long, bool, ulong>();
        await Assert.That(r1).IsEqualTo(v1);
        await Assert.That(r2).IsEqualTo(v2);
        await Assert.That(r3).IsTrue();
        await Assert.That(r4).IsEqualTo(v4);
    }

    // ── As<T1,T2,T3,T4,T5> (five arguments) ──────────────────────────────────

    [Test]
    public async Task As_Five_Args_Decodes_All_Correctly()
    {
        var v1 = Generator.Integer(1, 50);
        var v2 = (long)Generator.Integer(1, 50);
        var v3 = true;
        var v4 = (ulong)Generator.Integer(1, 50);
        var v5 = Generator.Memo(4, 8);
        var encoded = Abi.EncodeArguments(new object[] { v1, v2, v3, v4, v5 });
        var ep = new EncodedParams(encoded);
        var (r1, r2, r3, r4, r5) = ep.As<int, long, bool, ulong, string>();
        await Assert.That(r1).IsEqualTo(v1);
        await Assert.That(r2).IsEqualTo(v2);
        await Assert.That(r3).IsTrue();
        await Assert.That(r4).IsEqualTo(v4);
        await Assert.That(r5).IsEqualTo(v5);
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetAll_Single_Type_Decodes_Correctly()
    {
        var expected = Generator.Integer(100, 999);
        var encoded = Abi.EncodeArguments(new object[] { expected });
        var ep = new EncodedParams(encoded);
        var result = ep.GetAll(typeof(int));
        await Assert.That(result.Length).IsEqualTo(1);
        await Assert.That((int)result[0]).IsEqualTo(expected);
    }

    [Test]
    public async Task GetAll_Multiple_Types_Decodes_All_Correctly()
    {
        var v1 = Generator.Integer(1, 100);
        var v2 = (long)Generator.Integer(1, 100);
        var v3 = Generator.Memo(3, 8);
        var encoded = Abi.EncodeArguments(new object[] { v1, v2, v3 });
        var ep = new EncodedParams(encoded);
        var result = ep.GetAll(typeof(int), typeof(long), typeof(string));
        await Assert.That(result.Length).IsEqualTo(3);
        await Assert.That((int)result[0]).IsEqualTo(v1);
        await Assert.That((long)result[1]).IsEqualTo(v2);
        await Assert.That((string)result[2]).IsEqualTo(v3);
    }

    [Test]
    public async Task GetAll_No_Types_Returns_Empty_Array()
    {
        var encoded = Abi.EncodeArguments(new object[] { 42 });
        var ep = new EncodedParams(encoded);
        var result = ep.GetAll();
        await Assert.That(result).IsEmpty();
    }

    // ── bytesToSkip parameter ─────────────────────────────────────────────────

    [Test]
    public async Task As_With_BytesToSkip_Skips_Leading_Bytes_Before_Decoding()
    {
        // Prepend 4 bytes (simulating a function selector) before the ABI data
        var expected = Generator.Integer(1, 500);
        var abiData = Abi.EncodeArguments(new object[] { expected }).ToArray();
        var selector = new byte[] { 0xAB, 0xCD, 0xEF, 0x12 };
        var combined = selector.Concat(abiData).ToArray();
        var ep = new EncodedParams(combined.AsMemory());
        var result = ep.As<int>(bytesToSkip: 4);
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task As_Two_Args_With_BytesToSkip_Skips_Correctly()
    {
        var v1 = Generator.Integer(1, 100);
        var v2 = (long)Generator.Integer(1, 100);
        var abiData = Abi.EncodeArguments(new object[] { v1, v2 }).ToArray();
        var prefix = new byte[4] { 0x01, 0x02, 0x03, 0x04 };
        var combined = prefix.Concat(abiData).ToArray();
        var ep = new EncodedParams(combined.AsMemory());
        var (r1, r2) = ep.As<int, long>(bytesToSkip: 4);
        await Assert.That(r1).IsEqualTo(v1);
        await Assert.That(r2).IsEqualTo(v2);
    }

    [Test]
    public async Task As_Three_Args_With_BytesToSkip_Skips_Correctly()
    {
        var v1 = Generator.Integer(1, 50);
        var v2 = (long)Generator.Integer(1, 50);
        var v3 = true;
        var abiData = Abi.EncodeArguments(new object[] { v1, v2, v3 }).ToArray();
        var prefix = new byte[8];
        var combined = prefix.Concat(abiData).ToArray();
        var ep = new EncodedParams(combined.AsMemory());
        var (r1, r2, r3) = ep.As<int, long, bool>(bytesToSkip: 8);
        await Assert.That(r1).IsEqualTo(v1);
        await Assert.That(r2).IsEqualTo(v2);
        await Assert.That(r3).IsTrue();
    }

    [Test]
    public async Task As_Four_Args_With_BytesToSkip_Skips_Correctly()
    {
        var v1 = Generator.Integer(1, 50);
        var v2 = (long)Generator.Integer(1, 50);
        var v3 = false;
        var v4 = (ulong)Generator.Integer(1, 50);
        var abiData = Abi.EncodeArguments(new object[] { v1, v2, v3, v4 }).ToArray();
        var prefix = new byte[4];
        var combined = prefix.Concat(abiData).ToArray();
        var ep = new EncodedParams(combined.AsMemory());
        var (r1, r2, r3, r4) = ep.As<int, long, bool, ulong>(bytesToSkip: 4);
        await Assert.That(r1).IsEqualTo(v1);
        await Assert.That(r2).IsEqualTo(v2);
        await Assert.That(r3).IsFalse();
        await Assert.That(r4).IsEqualTo(v4);
    }

    [Test]
    public async Task As_Five_Args_With_BytesToSkip_Skips_Correctly()
    {
        var v1 = Generator.Integer(1, 30);
        var v2 = (long)Generator.Integer(1, 30);
        var v3 = true;
        var v4 = (ulong)Generator.Integer(1, 30);
        var v5 = Generator.Memo(3, 6);
        var abiData = Abi.EncodeArguments(new object[] { v1, v2, v3, v4, v5 }).ToArray();
        var prefix = new byte[4];
        var combined = prefix.Concat(abiData).ToArray();
        var ep = new EncodedParams(combined.AsMemory());
        var (r1, r2, r3, r4, r5) = ep.As<int, long, bool, ulong, string>(bytesToSkip: 4);
        await Assert.That(r1).IsEqualTo(v1);
        await Assert.That(r2).IsEqualTo(v2);
        await Assert.That(r3).IsTrue();
        await Assert.That(r4).IsEqualTo(v4);
        await Assert.That(r5).IsEqualTo(v5);
    }

    [Test]
    public async Task As_With_Zero_BytesToSkip_Decodes_From_Start()
    {
        var expected = Generator.Integer(1, 100);
        var encoded = Abi.EncodeArguments(new object[] { expected });
        var ep = new EncodedParams(encoded);
        // Explicit 0 should behave identically to the default
        var result = ep.As<int>(bytesToSkip: 0);
        await Assert.That(result).IsEqualTo(expected);
    }

    // ── Round-trip: string constructor → As<T> ────────────────────────────────

    [Test]
    public async Task RoundTrip_Hex_String_To_Int_Via_As()
    {
        var expected = Generator.Integer(1, 50_000);
        var encoded = Abi.EncodeArguments(new object[] { expected });
        var hex = Hex.FromBytes(encoded);
        var ep = new EncodedParams(hex);
        await Assert.That(ep.As<int>()).IsEqualTo(expected);
    }

    [Test]
    public async Task RoundTrip_0x_Prefixed_Hex_To_String_Via_As()
    {
        var expected = Generator.Memo(5, 15);
        var encoded = Abi.EncodeArguments(new object[] { expected });
        var hex = "0x" + Hex.FromBytes(encoded);
        var ep = new EncodedParams(hex);
        await Assert.That(ep.As<string>()).IsEqualTo(expected);
    }
}
