// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;

namespace Hiero.Test.Unit.Mirror;

public class NetworkStakeDataTests
{
    [Test]
    public async Task Deserializes_Full_OpenAPI_Example()
    {
        var json = """
        {
            "max_stake_rewarded": 10,
            "max_staking_reward_rate_per_hbar": 17808,
            "max_total_reward": 20,
            "node_reward_fee_fraction": 1.0,
            "reserved_staking_rewards": 30,
            "reward_balance_threshold": 40,
            "stake_total": 35000000000000000,
            "staking_period": {"from":"1655164800.000000000","to":"1655251200.000000000"},
            "staking_period_duration": 1440,
            "staking_periods_stored": 365,
            "staking_reward_fee_fraction": 1.0,
            "staking_reward_rate": 100000000000,
            "staking_start_threshold": 25000000000000000,
            "unreserved_staking_reward_balance": 50
        }
        """;
        var data = JsonSerializer.Deserialize<NetworkStakeData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.MaxStakeRewarded).IsEqualTo(10L);
        await Assert.That(data.MaxStakingRewardRatePerHbar).IsEqualTo(17808L);
        await Assert.That(data.MaxTotalReward).IsEqualTo(20L);
        await Assert.That(data.NodeRewardFeeFraction).IsEqualTo(1.0);
        await Assert.That(data.ReservedStakingRewards).IsEqualTo(30L);
        await Assert.That(data.RewardBalanceThreshold).IsEqualTo(40L);
        await Assert.That(data.StakeTotal).IsEqualTo(35_000_000_000_000_000L);
        await Assert.That(data.StakingPeriod.Starting).IsEqualTo(new ConsensusTimeStamp(1_655_164_800L, 0));
        await Assert.That(data.StakingPeriod.Ending).IsEqualTo(new ConsensusTimeStamp(1_655_251_200L, 0));
        await Assert.That(data.StakingPeriodDuration).IsEqualTo(1440L);
        await Assert.That(data.StakingPeriodsStored).IsEqualTo(365L);
        await Assert.That(data.StakingRewardFeeFraction).IsEqualTo(1.0);
        await Assert.That(data.StakingRewardRate).IsEqualTo(100_000_000_000L);
        await Assert.That(data.StakingStartThreshold).IsEqualTo(25_000_000_000_000_000L);
        await Assert.That(data.UnreservedStakingRewardBalance).IsEqualTo(50L);
    }

    [Test]
    public async Task Deserializes_Fractional_Fee_Fractions()
    {
        var json = """
        {
            "node_reward_fee_fraction": 0.125,
            "staking_reward_fee_fraction": 0.875
        }
        """;
        var data = JsonSerializer.Deserialize<NetworkStakeData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.NodeRewardFeeFraction).IsEqualTo(0.125);
        await Assert.That(data.StakingRewardFeeFraction).IsEqualTo(0.875);
    }

    [Test]
    public async Task Defaults_Are_Zero_When_Fields_Absent()
    {
        var json = """{}""";
        var data = JsonSerializer.Deserialize<NetworkStakeData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.MaxStakeRewarded).IsEqualTo(0L);
        await Assert.That(data.NodeRewardFeeFraction).IsEqualTo(0.0);
        await Assert.That(data.StakingPeriod).IsNull();
    }
}
