// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror
{
    /// <summary>
    /// Represents a staking reward transfer attached to a
    /// transaction — the per-transaction form carried in
    /// <see cref="TransactionData.StakingRewards"/>.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="StakingRewardData"/>, which
    /// is the top-level shape returned by the
    /// <c>/api/v1/accounts/{id}/rewards</c> endpoint and
    /// additionally carries the reward's consensus timestamp.
    /// </remarks>
    public class StakingRewardTransferData
    {
        /// <summary>
        /// The account receiving the staking reward
        /// </summary>
        [JsonPropertyName("account")]
        public EntityId Account { get; set; } = default!;
        /// <summary>
        /// The amount of the staking reward in tinybars
        /// </summary>
        [JsonPropertyName("amount")]
        [JsonConverter(typeof(LongMirrorConverter))]
        public long Amount { get; set; }
    }
}
