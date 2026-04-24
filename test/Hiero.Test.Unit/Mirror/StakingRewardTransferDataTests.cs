// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;

namespace Hiero.Test.Unit.Mirror;

public class StakingRewardTransferDataTests
{
    [Test]
    public async Task Deserializes_Nested_Transfer_Record()
    {
        var json = """
        {
            "account":"0.0.3",
            "amount":150
        }
        """;
        var data = JsonSerializer.Deserialize<StakingRewardTransferData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Account).IsEqualTo(new EntityId(0, 0, 3));
        await Assert.That(data.Amount).IsEqualTo(150L);
    }

    [Test]
    public async Task Reads_Transfers_From_TransactionData_Envelope()
    {
        var json = """
        {
            "staking_reward_transfers":[
                {"account":"0.0.3","amount":150},
                {"account":"0.0.9","amount":200}
            ]
        }
        """;
        var data = JsonSerializer.Deserialize<TransactionData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.StakingRewards).IsNotNull();
        await Assert.That(data.StakingRewards!.Length).IsEqualTo(2);
        await Assert.That(data.StakingRewards[0].Account).IsEqualTo(new EntityId(0, 0, 3));
        await Assert.That(data.StakingRewards[0].Amount).IsEqualTo(150L);
        await Assert.That(data.StakingRewards[1].Account).IsEqualTo(new EntityId(0, 0, 9));
        await Assert.That(data.StakingRewards[1].Amount).IsEqualTo(200L);
    }
}
