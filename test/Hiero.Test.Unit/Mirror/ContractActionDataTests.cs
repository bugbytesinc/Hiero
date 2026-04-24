// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;

namespace Hiero.Test.Unit.Mirror;

public class ContractActionDataTests
{
    [Test]
    public async Task Deserializes_All_Fields_Of_Populated_Action()
    {
        var json = """
        {
            "call_depth": 1,
            "call_operation_type": "CALL",
            "call_type": "CALL",
            "caller": "0.0.1001",
            "caller_type": "ACCOUNT",
            "from": "0x00000000000000000000000000000000000003e9",
            "gas": 50000,
            "gas_used": 31415,
            "index": 0,
            "input": "0xdeadbeef",
            "recipient": "0.0.2002",
            "recipient_type": "CONTRACT",
            "result_data": "0x1234",
            "result_data_type": "OUTPUT",
            "timestamp": "1700000000.123456789",
            "to": "0x00000000000000000000000000000000000007d2",
            "value": 100
        }
        """;
        var a = JsonSerializer.Deserialize<ContractActionData>(json);
        await Assert.That(a).IsNotNull();
        await Assert.That(a!.CallDepth).IsEqualTo(1);
        await Assert.That(a.CallOperationType).IsEqualTo("CALL");
        await Assert.That(a.CallType).IsEqualTo("CALL");
        await Assert.That(a.Caller).IsEqualTo(new EntityId(0, 0, 1001));
        await Assert.That(a.CallerType).IsEqualTo("ACCOUNT");
        await Assert.That(a.Gas).IsEqualTo(50000L);
        await Assert.That(a.GasUsed).IsEqualTo(31415L);
        await Assert.That(a.Index).IsEqualTo(0);
        await Assert.That(Hex.FromBytes(a.Input)).IsEqualTo("deadbeef");
        await Assert.That(a.Recipient).IsEqualTo(new EntityId(0, 0, 2002));
        await Assert.That(a.RecipientType).IsEqualTo("CONTRACT");
        await Assert.That(Hex.FromBytes(a.ResultData)).IsEqualTo("1234");
        await Assert.That(a.ResultDataType).IsEqualTo("OUTPUT");
        await Assert.That(a.Timestamp).IsEqualTo(new ConsensusTimeStamp(1_700_000_000L, 123_456_789));
        await Assert.That(a.Value).IsEqualTo(100L);
        await Assert.That(a.To).IsNotNull();
        await Assert.That(a.From.ToString()).IsEqualTo("0x00000000000000000000000000000000000003E9");
        await Assert.That(a.To!.ToString()).IsEqualTo("0x00000000000000000000000000000000000007D2");
    }

    [Test]
    public async Task Null_To_Distinct_From_Zero_Address_To()
    {
        var nullTo = """
        {
            "call_depth": 0,
            "call_operation_type": "CREATE",
            "call_type": "CREATE",
            "caller": "0.0.1",
            "caller_type": "ACCOUNT",
            "from": "0x0000000000000000000000000000000000000001",
            "gas": 0,
            "gas_used": 0,
            "index": 0,
            "recipient": "0.0.2",
            "result_data_type": "OUTPUT",
            "timestamp": "1.0",
            "to": null,
            "value": 0
        }
        """;
        var zeroTo = """
        {
            "call_depth": 0,
            "call_operation_type": "CREATE",
            "call_type": "CREATE",
            "caller": "0.0.1",
            "caller_type": "ACCOUNT",
            "from": "0x0000000000000000000000000000000000000001",
            "gas": 0,
            "gas_used": 0,
            "index": 0,
            "recipient": "0.0.2",
            "result_data_type": "OUTPUT",
            "timestamp": "1.0",
            "to": "0x0000000000000000000000000000000000000000",
            "value": 0
        }
        """;
        var a1 = JsonSerializer.Deserialize<ContractActionData>(nullTo)!;
        var a2 = JsonSerializer.Deserialize<ContractActionData>(zeroTo)!;
        await Assert.That(a1.To).IsNull();
        await Assert.That(a2.To).IsNotNull();
        await Assert.That(a2.To!.Bytes.Length).IsEqualTo(20);
        await Assert.That(a2.To.Equals(EvmAddress.None)).IsTrue();
    }
}
