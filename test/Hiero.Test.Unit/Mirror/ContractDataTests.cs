// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;

namespace Hiero.Test.Unit.Mirror;

public class ContractDataTests
{
    [Test]
    public async Task Nonce_Field_Deserializes_When_Present()
    {
        var json = """
        {
            "contract_id":"0.0.100",
            "nonce":5
        }
        """;
        var data = JsonSerializer.Deserialize<ContractData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Nonce).IsEqualTo(5L);
    }

    [Test]
    public async Task Nonce_Defaults_To_Zero_When_Absent()
    {
        var json = """{"contract_id":"0.0.100"}""";
        var data = JsonSerializer.Deserialize<ContractData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Nonce).IsEqualTo(0L);
    }

    [Test]
    public async Task Page_Envelope_Reads_Contracts_Array()
    {
        var json = """
        {
            "contracts":[
                {"contract_id":"0.0.100","nonce":0,"memo":"first"},
                {"contract_id":"0.0.101","nonce":3,"memo":"second"}
            ],
            "links":{"next":null}
        }
        """;
        var page = JsonSerializer.Deserialize<Hiero.Mirror.Implementation.ContractDataPage>(json);
        await Assert.That(page).IsNotNull();
        var items = page!.GetItems().ToArray();
        await Assert.That(items.Length).IsEqualTo(2);
        await Assert.That(items[0].HapiAddress).IsEqualTo(new EntityId(0, 0, 100));
        await Assert.That(items[0].Nonce).IsEqualTo(0L);
        await Assert.That(items[1].HapiAddress).IsEqualTo(new EntityId(0, 0, 101));
        await Assert.That(items[1].Nonce).IsEqualTo(3L);
    }
}
