// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Proto;

namespace Hiero.Test.Unit.Core;

public class ResponseCodeTests
{
    [Test]
    public async Task ResponseCode_Has_All_Protobuf_Values()
    {
        var missing = new List<int>();
        foreach (int protoValue in Enum.GetValues(typeof(ResponseCodeEnum)))
        {
            if (!Enum.IsDefined(typeof(ResponseCode), protoValue))
            {
                missing.Add(protoValue);
            }
        }
        await Assert.That(missing).IsEmpty();
    }

    [Test]
    public async Task ResponseCode_Has_No_Unmatched_Protobuf_Values()
    {
        var unmatched = new List<int>();
        foreach (int sdkValue in Enum.GetValues(typeof(ResponseCode)))
        {
            if (!Enum.IsDefined(typeof(ResponseCodeEnum), sdkValue) && sdkValue >= 0)
            {
                unmatched.Add(sdkValue);
            }
        }
        await Assert.That(unmatched).IsEmpty();
    }

    [Test]
    public async Task Converter_Can_Round_Trip_All_ResponseCode_Values()
    {
        var failures = new List<string>();
        foreach (ResponseCode code in Enum.GetValues(typeof(ResponseCode)))
        {
            var json = JsonSerializer.Serialize(code);
            var deserialized = JsonSerializer.Deserialize<ResponseCode>(json);
            if (deserialized != code)
            {
                failures.Add($"ResponseCode.{code} ({(int)code}) serialized as {json}, deserialized as {deserialized}");
            }
        }
        await Assert.That(failures).IsEmpty();
    }

    [Test]
    public async Task Success_Code_Has_Expected_Value()
    {
        int success = (int)ResponseCode.Success;
        await Assert.That(success).IsEqualTo(22);
    }

    [Test]
    public async Task Ok_Code_Has_Expected_Value()
    {
        int ok = (int)ResponseCode.Ok;
        await Assert.That(ok).IsEqualTo(0);
    }

    [Test]
    public async Task RpcError_Is_Negative()
    {
        int rpcError = (int)ResponseCode.RpcError;
        await Assert.That(rpcError).IsNegative();
    }

    [Test]
    public async Task Success_Serializes_To_Expected_String()
    {
        var json = JsonSerializer.Serialize(ResponseCode.Success);
        await Assert.That(json).IsEqualTo("\"SUCCESS\"");
    }

    [Test]
    public async Task Can_Deserialize_Known_String_Value()
    {
        var code = JsonSerializer.Deserialize<ResponseCode>("\"INVALID_SIGNATURE\"");
        await Assert.That(code).IsEqualTo(ResponseCode.InvalidSignature);
    }

    [Test]
    public async Task Unknown_String_Deserializes_To_Unrecognized()
    {
        // The network adds response codes over time, so an SDK build will
        // eventually meet one it does not map.  That must degrade, not throw.
        var code = JsonSerializer.Deserialize<ResponseCode>("\"SOME_FUTURE_STATUS_CODE\"");
        await Assert.That(code).IsEqualTo(ResponseCode.Unrecognized);
    }

    [Test]
    public async Task Null_Deserializes_To_Unrecognized()
    {
        var code = JsonSerializer.Deserialize<ResponseCode>("null");
        await Assert.That(code).IsEqualTo(ResponseCode.Unrecognized);
    }

    [Test]
    public async Task Non_String_Token_Deserializes_To_Unrecognized()
    {
        var code = JsonSerializer.Deserialize<ResponseCode>("22");
        await Assert.That(code).IsEqualTo(ResponseCode.Unrecognized);
    }

    [Test]
    public async Task Unreadable_Status_Does_Not_Fail_Its_Enclosing_Object()
    {
        // The reason unreadable statuses degrade instead of throwing: these
        // arrive inside larger Mirror Node payloads, and one bad value must not
        // cost the caller the whole page.
        var json = """{"Status":"SOME_FUTURE_STATUS_CODE","Nonce":7}""";
        var row = JsonSerializer.Deserialize<StatusRow>(json);
        await Assert.That(row!.Status).IsEqualTo(ResponseCode.Unrecognized);
        await Assert.That(row.Nonce).IsEqualTo(7);
    }

    [Test]
    public async Task Unrecognized_Serializes_To_Named_Marker()
    {
        // It must name itself rather than emitting an empty string or a bare
        // number, so a persisted value stays readable.
        var json = JsonSerializer.Serialize(ResponseCode.Unrecognized);
        await Assert.That(json).IsEqualTo("\"UNRECOGNIZED\"");
    }

    [Test]
    public async Task Undeclared_Enum_Value_Serializes_To_Unrecognized_Marker()
    {
        var json = JsonSerializer.Serialize((ResponseCode)(-4242));
        await Assert.That(json).IsEqualTo("\"UNRECOGNIZED\"");
        var code = JsonSerializer.Deserialize<ResponseCode>(json);
        await Assert.That(code).IsEqualTo(ResponseCode.Unrecognized);
    }

    [Test]
    public async Task Fallback_Value_Is_A_Named_Declared_Member()
    {
        // Guards the defect this replaced: the converter used to return a bare
        // (ResponseCode)(-500), which matched no member, so it stringified as
        // "-500" and no switch arm could catch it.  Assert on the value the
        // converter actually produces, not on the enum literal.
        var fallback = JsonSerializer.Deserialize<ResponseCode>("\"NOT_A_REAL_STATUS\"");
        await Assert.That(Enum.IsDefined(typeof(ResponseCode), fallback)).IsTrue();
        await Assert.That(fallback.ToString()).IsEqualTo(nameof(ResponseCode.Unrecognized));
        await Assert.That(fallback).IsNotEqualTo(ResponseCode.RpcError);
    }

    private sealed class StatusRow
    {
        public ResponseCode Status { get; set; }
        public int Nonce { get; set; }
    }
}
