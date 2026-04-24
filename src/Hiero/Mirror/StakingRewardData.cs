// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using Hiero.Mirror.Paging;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// A single staking reward paid to an account, as reported
/// by the <c>/api/v1/accounts/{id}/rewards</c> mirror-node
/// endpoint.
/// </summary>
/// <remarks>
/// Distinct from <see cref="StakingRewardTransferData"/>,
/// which is the narrower per-transaction form carried inside
/// <see cref="TransactionData.StakingRewards"/>. The top-level
/// form additionally names the consensus instant at which the
/// reward was paid.
/// </remarks>
public class StakingRewardData
{
    /// <summary>
    /// The account that received the staking reward.
    /// </summary>
    [JsonPropertyName("account_id")]
    public EntityId Account { get; set; } = default!;
    /// <summary>
    /// The amount of the staking reward in tinybars.
    /// </summary>
    [JsonPropertyName("amount")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Amount { get; set; }
    /// <summary>
    /// The consensus timestamp at which the reward was paid.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public ConsensusTimeStamp Timestamp { get; set; }
}
/// <summary>
/// Extension methods for querying staking-reward data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class StakingRewardDataExtensions
{
    /// <summary>
    /// Enumerates the staking reward payouts received by a specific
    /// account from <c>/api/v1/accounts/{id}/rewards</c>. Use
    /// <see cref="TimestampFilter"/> to bracket a time range.
    /// Newest-first by default; pass <see cref="OrderBy.Ascending"/>
    /// to reverse.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="account">
    /// The account whose staking-reward history is requested.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters. The endpoint supports
    /// <see cref="TimestampFilter"/>, <see cref="PageLimit"/>, and
    /// <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of staking-reward records.
    /// </returns>
    public static IAsyncEnumerable<StakingRewardData> GetAccountStakingRewardsAsync(this MirrorRestClient client, EntityId account, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"accounts/{MirrorFormat(account)}/rewards", [new PageLimit(100), .. filters]);
        return client.GetPagedItemsAsync<StakingRewardDataPage, StakingRewardData>(path, MirrorJsonContext.Default.StakingRewardDataPage);
    }
}
