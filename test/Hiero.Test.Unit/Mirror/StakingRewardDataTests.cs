// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;

namespace Hiero.Test.Unit.Mirror;

public class StakingRewardDataTests
{
    [Test]
    public async Task Deserializes_Full_Record_From_OpenAPI_Example()
    {
        var json = """
        {
            "account_id":"0.0.1000",
            "amount":10,
            "timestamp":"1234567890.000000001"
        }
        """;
        var data = JsonSerializer.Deserialize<StakingRewardData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Account).IsEqualTo(new EntityId(0, 0, 1000));
        await Assert.That(data.Amount).IsEqualTo(10L);
        await Assert.That(data.Timestamp).IsEqualTo(new ConsensusTimeStamp(1_234_567_890L, 1));
    }

    [Test]
    public async Task Tolerates_String_Encoded_Amount()
    {
        var json = """
        {
            "account_id":"0.0.55",
            "amount":"9223372036854775000",
            "timestamp":"1700000000.000000000"
        }
        """;
        var data = JsonSerializer.Deserialize<StakingRewardData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Amount).IsEqualTo(9_223_372_036_854_775_000L);
    }

    [Test]
    public async Task Defaults_Are_Zero_When_Fields_Absent()
    {
        var json = """{}""";
        var data = JsonSerializer.Deserialize<StakingRewardData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Amount).IsEqualTo(0L);
        await Assert.That(data.Timestamp).IsEqualTo(default(ConsensusTimeStamp));
    }

    [Test]
    public async Task Reads_Page_Envelope_From_Rewards_Endpoint()
    {
        var json = """
        {
            "rewards":[
                {"account_id":"0.0.1000","amount":10,"timestamp":"1234567890.000000001"},
                {"account_id":"0.0.1000","amount":20,"timestamp":"1234567900.000000002"}
            ],
            "links":{"next":null}
        }
        """;
        var page = JsonSerializer.Deserialize<Hiero.Mirror.Implementation.StakingRewardDataPage>(json);
        await Assert.That(page).IsNotNull();
        var items = page!.GetItems().ToArray();
        await Assert.That(items.Length).IsEqualTo(2);
        await Assert.That(items[0].Amount).IsEqualTo(10L);
        await Assert.That(items[1].Amount).IsEqualTo(20L);
        await Assert.That(items[1].Timestamp).IsEqualTo(new ConsensusTimeStamp(1_234_567_900L, 2));
    }
}
