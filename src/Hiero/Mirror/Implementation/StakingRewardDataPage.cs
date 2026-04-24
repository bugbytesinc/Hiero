// SPDX-License-Identifier: Apache-2.0
using System.Text.Json.Serialization;

namespace Hiero.Mirror.Implementation;
/// <summary>
/// Paged list of staking reward records.
/// </summary>
internal class StakingRewardDataPage : Page<StakingRewardData>
{
    /// <summary>
    /// List of staking reward records.
    /// </summary>
    [JsonPropertyName("rewards")]
    public StakingRewardData[]? Rewards { get; set; }
    /// <summary>
    /// Enumerates the list of staking reward records.
    /// </summary>
    /// <returns>
    /// An enumerator listing the staking reward records in the list.
    /// </returns>
    public override IEnumerable<StakingRewardData> GetItems()
    {
        return Rewards ?? Array.Empty<StakingRewardData>();
    }
}
