using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;
/// <summary>
/// Represents Hedera hBar rate exchange with USD
/// </summary>
public class RateData
{
    /// <summary>
    /// The value of USD in cents of this 
    /// exchange rate fraction.
    /// </summary>
    [JsonPropertyName("cent_equivalent")]
    [JsonConverter(typeof(IntMirrorConverter))]
    public int CentEquivalent { get; set; }
    /// <summary>
    /// The whole value of hBars for
    /// this exchange rate fraction.
    /// </summary>
    [JsonPropertyName("hbar_equivalent")]
    [JsonConverter(typeof(IntMirrorConverter))]
    public int HbarEquivalent { get; set; }
    /// <summary>
    /// Time at which this exchange rate is
    /// no longer valid.
    /// </summary>
    [JsonPropertyName("expiration_time")]
    [JsonConverter(typeof(ConsensusTimeStampForExchangeRateConverter))]
    public ConsensusTimeStamp Expiration { get; set; }

}