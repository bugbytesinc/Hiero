// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;

namespace Hiero.Test.Unit.Mirror;

public class TokenAirdropDataTests
{
    [Test]
    public async Task Deserializes_Fungible_Record_From_OpenAPI_Example()
    {
        var json = """
        {
            "amount":10,
            "receiver_id":"0.0.15",
            "sender_id":"0.0.10",
            "serial_number":null,
            "timestamp":{"from":"1651560386.060890949","to":"1651560386.661997287"},
            "token_id":"0.0.99"
        }
        """;
        var data = JsonSerializer.Deserialize<TokenAirdropData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Amount).IsEqualTo(10L);
        await Assert.That(data.Receiver).IsEqualTo(new EntityId(0, 0, 15));
        await Assert.That(data.Sender).IsEqualTo(new EntityId(0, 0, 10));
        await Assert.That(data.SerialNumber).IsNull();
        await Assert.That(data.Token).IsEqualTo(new EntityId(0, 0, 99));
        await Assert.That(data.Timestamp).IsNotNull();
        await Assert.That(data.Timestamp!.Starting).IsEqualTo(new ConsensusTimeStamp(1_651_560_386L, 60_890_949));
        await Assert.That(data.Timestamp.Ending).IsEqualTo(new ConsensusTimeStamp(1_651_560_386L, 661_997_287));
    }

    [Test]
    public async Task Deserializes_Nft_Record_With_Serial_Number()
    {
        var json = """
        {
            "amount":1,
            "receiver_id":"0.0.15",
            "sender_id":"0.0.10",
            "serial_number":42,
            "timestamp":{"from":"1700000000.000000000","to":"1700000001.000000000"},
            "token_id":"0.0.99"
        }
        """;
        var data = JsonSerializer.Deserialize<TokenAirdropData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.SerialNumber).IsEqualTo(42L);
        await Assert.That(data.Amount).IsEqualTo(1L);
    }

    [Test]
    public async Task Absent_SerialNumber_Defaults_To_Null()
    {
        var json = """
        {
            "amount":100,
            "receiver_id":"0.0.15",
            "sender_id":"0.0.10",
            "timestamp":{"from":"1700000000.000000000","to":null},
            "token_id":"0.0.99"
        }
        """;
        var data = JsonSerializer.Deserialize<TokenAirdropData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.SerialNumber).IsNull();
        await Assert.That(data.Timestamp).IsNotNull();
        await Assert.That(data.Timestamp!.Ending).IsNull();
    }

    [Test]
    public async Task Page_Envelope_Reads_Airdrops_Array()
    {
        var json = """
        {
            "airdrops":[
                {"amount":10,"receiver_id":"0.0.15","sender_id":"0.0.10","serial_number":null,"timestamp":{"from":"1651560386.060890949","to":"1651560386.661997287"},"token_id":"0.0.99"},
                {"amount":1,"receiver_id":"0.0.15","sender_id":"0.0.10","serial_number":7,"timestamp":{"from":"1700000000.000000000","to":"1700000001.000000000"},"token_id":"0.0.100"}
            ],
            "links":{"next":null}
        }
        """;
        var page = JsonSerializer.Deserialize<Hiero.Mirror.Implementation.TokenAirdropDataPage>(json);
        await Assert.That(page).IsNotNull();
        var items = page!.GetItems().ToArray();
        await Assert.That(items.Length).IsEqualTo(2);
        await Assert.That(items[0].SerialNumber).IsNull();
        await Assert.That(items[1].SerialNumber).IsEqualTo(7L);
        await Assert.That(items[1].Token).IsEqualTo(new EntityId(0, 0, 100));
    }
}
