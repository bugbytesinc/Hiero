// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;
/// <summary>
/// A single HBAR-to-USD exchange rate, expressed as the ratio of
/// <see cref="HbarEquivalent"/> hbar to <see cref="CentEquivalent"/>
/// US cents.
/// </summary>
public class RateData
{
    /// <summary>
    /// The US-cent side of the exchange-rate ratio — the number of
    /// cents equivalent to <see cref="HbarEquivalent"/> hbar.
    /// </summary>
    [JsonPropertyName("cent_equivalent")]
    [JsonConverter(typeof(IntMirrorConverter))]
    public int CentEquivalent { get; set; }
    /// <summary>
    /// The hbar side of the exchange-rate ratio — the number of
    /// whole hbar equivalent to <see cref="CentEquivalent"/> cents.
    /// </summary>
    [JsonPropertyName("hbar_equivalent")]
    [JsonConverter(typeof(IntMirrorConverter))]
    public int HbarEquivalent { get; set; }
    /// <summary>
    /// The consensus time at which this exchange rate expires and
    /// the next rate takes effect.
    /// </summary>
    [JsonPropertyName("expiration_time")]
    [JsonConverter(typeof(ConsensusTimeStampForExchangeRateConverter))]
    public ConsensusTimeStamp Expiration { get; set; }

}