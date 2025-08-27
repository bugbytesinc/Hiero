using System.Text.Json.Serialization;

namespace Hiero.Mirror;

public class CustomFeeData
{
    /// <summary>
    /// The consensus time that this royalty/custom
    /// fee schedule was created.
    /// </summary>
    [JsonPropertyName("created_timestamp")]
    public ConsensusTimeStamp Created { get; set; }
    /// <summary>
    /// Associated Fixed Fees
    /// </summary>
    [JsonPropertyName("fixed_fees")]
    public FixedFeeData[] FixedFees { get; set; } = default!;
    /// <summary>
    /// Associated Fractional Fees
    /// </summary>
    [JsonPropertyName("fractional_fees")]
    public FractionalFeeData[] FractionalFees { get; set; } = default!;
    /// <summary>
    /// Associated Royalty Fees
    /// </summary>
    [JsonPropertyName("royalty_fees")]
    public FractionalFeeData[] RoyaltyFees { get; set; } = default!;
}
