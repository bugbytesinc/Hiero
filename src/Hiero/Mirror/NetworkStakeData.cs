// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using Hiero.Mirror.Implementation;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;
/// <summary>
/// Network-wide staking parameters and balances as reported by the
/// mirror node. Monetary fields are expressed in tinybars; fee
/// fractions are real numbers between 0 and 1.
/// </summary>
public class NetworkStakeData
{
    /// <summary>
    /// The maximum amount of tinybar that can be staked for reward
    /// while still achieving the maximum per-hbar reward rate.
    /// </summary>
    [JsonPropertyName("max_stake_rewarded")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long MaxStakeRewarded { get; set; }
    /// <summary>
    /// The maximum reward rate, in tinybars per whole hbar, that any
    /// account can receive in a day.
    /// </summary>
    [JsonPropertyName("max_staking_reward_rate_per_hbar")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long MaxStakingRewardRatePerHbar { get; set; }
    /// <summary>
    /// The total tinybars to be paid as staking rewards in the ending
    /// period, after applying the settings for the 0.0.800 balance
    /// threshold and the maximum stake rewarded.
    /// </summary>
    [JsonPropertyName("max_total_reward")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long MaxTotalReward { get; set; }
    /// <summary>
    /// The fraction between zero and one of the network and service
    /// fees paid to the node reward account 0.0.801.
    /// </summary>
    [JsonPropertyName("node_reward_fee_fraction")]
    public double NodeRewardFeeFraction { get; set; }
    /// <summary>
    /// The amount of the staking reward funds of account 0.0.800
    /// reserved to pay pending rewards that have been earned but not
    /// collected.
    /// </summary>
    [JsonPropertyName("reserved_staking_rewards")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long ReservedStakingRewards { get; set; }
    /// <summary>
    /// The unreserved tinybar balance of account 0.0.800 required to
    /// achieve the maximum per-hbar reward rate.
    /// </summary>
    [JsonPropertyName("reward_balance_threshold")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long RewardBalanceThreshold { get; set; }
    /// <summary>
    /// The total amount staked to the network in tinybars at the
    /// start of the current staking period.
    /// </summary>
    [JsonPropertyName("stake_total")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long StakeTotal { get; set; }
    /// <summary>
    /// The consensus-timestamp range defining the current staking
    /// period.
    /// </summary>
    [JsonPropertyName("staking_period")]
    public TimestampRangeData StakingPeriod { get; set; } = default!;
    /// <summary>
    /// The length of a staking period, in minutes.
    /// </summary>
    [JsonPropertyName("staking_period_duration")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long StakingPeriodDuration { get; set; }
    /// <summary>
    /// The number of staking periods for which the reward is stored
    /// for each node.
    /// </summary>
    [JsonPropertyName("staking_periods_stored")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long StakingPeriodsStored { get; set; }
    /// <summary>
    /// The fraction between zero and one of the network and service
    /// fees paid to the staking reward account 0.0.800.
    /// </summary>
    [JsonPropertyName("staking_reward_fee_fraction")]
    public double StakingRewardFeeFraction { get; set; }
    /// <summary>
    /// The total number of tinybars to be distributed as staking
    /// rewards each period.
    /// </summary>
    [JsonPropertyName("staking_reward_rate")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long StakingRewardRate { get; set; }
    /// <summary>
    /// The minimum balance of staking reward account 0.0.800 required
    /// to activate rewards.
    /// </summary>
    [JsonPropertyName("staking_start_threshold")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long StakingStartThreshold { get; set; }
    /// <summary>
    /// The unreserved balance of account 0.0.800 at the close of the
    /// just-ending period; used to compute the HIP-782 balance ratio.
    /// </summary>
    [JsonPropertyName("unreserved_staking_reward_balance")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long UnreservedStakingRewardBalance { get; set; }
}
/// <summary>
/// Extension methods for querying network stake data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class NetworkStakeDataExtensions
{
    /// <summary>
    /// Retrieves the network's current staking parameters and
    /// balances via <c>/api/v1/network/stake</c>. Returns a single
    /// snapshot covering the in-progress staking period.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <returns>
    /// The network stake data, or null if not found.
    /// </returns>
    public static Task<NetworkStakeData?> GetNetworkStakeAsync(this MirrorRestClient client)
    {
        return client.GetSingleItemAsync("network/stake", MirrorJsonContext.Default.NetworkStakeData);
    }
}
