using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror
{
    /// <summary>
    /// Represents a staking award attachted
    /// to a transaction.
    /// </summary>
    public class StakingRewardData
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
