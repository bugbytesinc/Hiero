// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;

namespace Hiero.Test.Unit.Mirror;

public class NftAllowanceDataTests
{
    [Test]
    public async Task Deserializes_Full_Record_From_OpenAPI_Example()
    {
        var json = """
        {
            "approved_for_all":false,
            "owner":"0.0.11",
            "spender":"0.0.15",
            "timestamp":{"from":"1651560386.060890949","to":"1651560386.661997287"},
            "token_id":"0.0.99"
        }
        """;
        var data = JsonSerializer.Deserialize<NftAllowanceData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.ApprovedForAll).IsFalse();
        await Assert.That(data.Owner).IsEqualTo(new EntityId(0, 0, 11));
        await Assert.That(data.Spender).IsEqualTo(new EntityId(0, 0, 15));
        await Assert.That(data.Token).IsEqualTo(new EntityId(0, 0, 99));
        await Assert.That(data.Timestamp).IsNotNull();
        await Assert.That(data.Timestamp!.Starting).IsEqualTo(new ConsensusTimeStamp(1_651_560_386L, 60_890_949));
        await Assert.That(data.Timestamp.Ending).IsEqualTo(new ConsensusTimeStamp(1_651_560_386L, 661_997_287));
    }

    [Test]
    public async Task ApprovedForAll_True_Is_Round_Tripped()
    {
        var json = """
        {
            "approved_for_all":true,
            "owner":"0.0.11",
            "spender":"0.0.15",
            "timestamp":{"from":"1700000000.000000000","to":null},
            "token_id":"0.0.99"
        }
        """;
        var data = JsonSerializer.Deserialize<NftAllowanceData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.ApprovedForAll).IsTrue();
        await Assert.That(data.Timestamp).IsNotNull();
        await Assert.That(data.Timestamp!.Ending).IsNull();
    }

    [Test]
    public async Task Defaults_Are_Safe_When_Fields_Absent()
    {
        var json = """{}""";
        var data = JsonSerializer.Deserialize<NftAllowanceData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.ApprovedForAll).IsFalse();
        await Assert.That(data.Timestamp).IsNull();
    }

    [Test]
    public async Task Page_Envelope_Reads_Allowances_Array()
    {
        var json = """
        {
            "allowances":[
                {"approved_for_all":false,"owner":"0.0.11","spender":"0.0.15","timestamp":{"from":"1700000000.000000000","to":"1700000001.000000000"},"token_id":"0.0.99"},
                {"approved_for_all":true,"owner":"0.0.11","spender":"0.0.16","timestamp":{"from":"1700000002.000000000","to":null},"token_id":"0.0.100"}
            ],
            "links":{"next":null}
        }
        """;
        var page = JsonSerializer.Deserialize<Hiero.Mirror.Implementation.NftAllowanceDataPage>(json);
        await Assert.That(page).IsNotNull();
        var items = page!.GetItems().ToArray();
        await Assert.That(items.Length).IsEqualTo(2);
        await Assert.That(items[0].ApprovedForAll).IsFalse();
        await Assert.That(items[0].Spender).IsEqualTo(new EntityId(0, 0, 15));
        await Assert.That(items[1].ApprovedForAll).IsTrue();
        await Assert.That(items[1].Token).IsEqualTo(new EntityId(0, 0, 100));
    }
}
