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
}
