// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;

namespace Hiero.Test.Unit.Mirror;

public class OpcodesDataTests
{
    [Test]
    public async Task Deserializes_Full_Opcodes_Response_With_Projections_Populated()
    {
        var json = """
        {
            "address": "0x00000000000000000000000000000000000003e8",
            "contract_id": "0.0.1000",
            "failed": false,
            "gas": 42000,
            "return_value": "0x1234",
            "opcodes": [
                {
                    "depth": 1,
                    "gas": 41000,
                    "gas_cost": 3,
                    "op": "PUSH1",
                    "pc": 0,
                    "stack": ["0x00", "0x01"],
                    "memory": ["0xaabbccdd"],
                    "storage": {
                        "0x0000000000000000000000000000000000000000000000000000000000000001":
                        "0x00000000000000000000000000000000000000000000000000000000000000ff"
                    }
                },
                {
                    "depth": 2,
                    "gas": 40000,
                    "gas_cost": 5,
                    "op": "REVERT",
                    "pc": 17,
                    "reason": "0xdeadbeef",
                    "stack": null,
                    "memory": null,
                    "storage": null
                }
            ]
        }
        """;
        var data = JsonSerializer.Deserialize<OpcodesData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Failed).IsFalse();
        await Assert.That(data.Gas).IsEqualTo(42000L);
        await Assert.That(data.Contract).IsEqualTo(new EntityId(0, 0, 1000));
        await Assert.That(Hex.FromBytes(data.ReturnValue)).IsEqualTo("1234");
        await Assert.That(data.Opcodes.Length).IsEqualTo(2);

        var first = data.Opcodes[0];
        await Assert.That(first.Op).IsEqualTo("PUSH1");
        await Assert.That(first.Depth).IsEqualTo(1);
        await Assert.That(first.Gas).IsEqualTo(41000L);
        await Assert.That(first.GasCost).IsEqualTo(3L);
        await Assert.That(first.Pc).IsEqualTo(0);
        await Assert.That(first.Stack).IsNotNull();
        await Assert.That(first.Stack!.Length).IsEqualTo(2);
        await Assert.That(Hex.FromBytes(first.Stack[1])).IsEqualTo("01");
        await Assert.That(first.Memory).IsNotNull();
        await Assert.That(first.Memory!.Length).IsEqualTo(1);
        await Assert.That(Hex.FromBytes(first.Memory[0])).IsEqualTo("aabbccdd");
        await Assert.That(first.Storage).IsNotNull();
        await Assert.That(first.Storage!.Count).IsEqualTo(1);
        var kvp = first.Storage.Single();
        await Assert.That(kvp.Key).IsEqualTo("0x0000000000000000000000000000000000000000000000000000000000000001");
        await Assert.That(Hex.FromBytes(kvp.Value).EndsWith("ff")).IsTrue();

        var second = data.Opcodes[1];
        await Assert.That(second.Op).IsEqualTo("REVERT");
        await Assert.That(second.Stack).IsNull();
        await Assert.That(second.Memory).IsNull();
        await Assert.That(second.Storage).IsNull();
        await Assert.That(Hex.FromBytes(second.Reason)).IsEqualTo("deadbeef");
    }

    [Test]
    public async Task Deserializes_Opcode_With_Empty_Storage_Object()
    {
        var json = """
        {
            "address": "0x0000000000000000000000000000000000000000",
            "contract_id": "0.0.2",
            "failed": false,
            "gas": 21000,
            "return_value": "0x",
            "opcodes": [
                {
                    "depth": 1,
                    "gas": 21000,
                    "gas_cost": 0,
                    "op": "STOP",
                    "pc": 0,
                    "stack": [],
                    "memory": [],
                    "storage": {}
                }
            ]
        }
        """;
        var data = JsonSerializer.Deserialize<OpcodesData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Opcodes[0].Stack!.Length).IsEqualTo(0);
        await Assert.That(data.Opcodes[0].Memory!.Length).IsEqualTo(0);
        await Assert.That(data.Opcodes[0].Storage!.Count).IsEqualTo(0);
    }
}
