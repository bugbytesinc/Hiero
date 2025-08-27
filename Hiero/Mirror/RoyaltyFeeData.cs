﻿using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;

/// <summary>
/// Represents royalty fees charged for
/// transfering assets (nfts).
/// </summary>
public class RoyaltyFeeData
{
    /// <summary>
    /// Are collecor accounts exempt from paying fees.
    /// </summary>
    [JsonPropertyName("all_collectors_are_exempt")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool CollectorsExempt { get; set; }
    /// <summary>
    /// Amount of fracitonal fee to collect.
    /// </summary>
    [JsonPropertyName("amount")]
    public FractionData Amount { get; set; } = default!;
    /// <summary>
    /// The account receiving the fees.
    /// </summary>
    [JsonPropertyName("collector_account_id")]
    public EntityId Collector { get; set; } = default!;
    /// <summary>
    /// The fallback fee to pay if no value was
    /// exchanged for NFT
    /// </summary>
    [JsonPropertyName("fallback_fee")]
    public FallbackFeeData FallbackFee { get; set; } = default!;
}
