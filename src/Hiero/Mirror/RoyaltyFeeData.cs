// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;

/// <summary>
/// A royalty fee charged on NFT transfers — a fraction of the value
/// exchanged for the asset, paid to a collector account.
/// </summary>
public class RoyaltyFeeData
{
    /// <summary>
    /// When <c>true</c>, fee-collector accounts are themselves
    /// exempt from paying this royalty when they transfer the asset.
    /// </summary>
    [JsonPropertyName("all_collectors_are_exempt")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool CollectorsExempt { get; set; }
    /// <summary>
    /// The fraction of the exchanged value collected as the royalty.
    /// </summary>
    [JsonPropertyName("amount")]
    public FractionData Amount { get; set; } = default!;
    /// <summary>
    /// The account receiving the royalty fee.
    /// </summary>
    [JsonPropertyName("collector_account_id")]
    public EntityId Collector { get; set; } = default!;
    /// <summary>
    /// The fixed fee charged in lieu of the royalty when no value
    /// was exchanged for the NFT (e.g. a free transfer).
    /// </summary>
    [JsonPropertyName("fallback_fee")]
    public FallbackFeeData FallbackFee { get; set; } = default!;
}
