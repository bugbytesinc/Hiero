// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// Represents the current and next exchange
/// rate as reported by the hedera network.
/// </summary>
public class ExchangeRateData
{
    /// <summary>
    /// The current exchange rate that 
    /// the hedera network uses to
    /// convert fees into hBar equivalent.
    /// </summary>
    [JsonPropertyName("current_rate")]
    public RateData CurrentRate { get; set; } = default!;
    /// <summary>
    /// The next exchange rate that will
    /// be used by the hedera network to
    /// convert fees into hBar equivalent.
    /// </summary>
    [JsonPropertyName("next_rate")]
    public RateData NextRate { get; set; } = default!;
    /// <summary>
    /// Timestamp at which this information
    /// was generated.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public ConsensusTimeStamp Timestamp { get; set; }
}
/// <summary>
/// Extension methods for querying exchange rate data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ExchangeRateDataExtensions
{
    /// <summary>
    /// Retrieves the current and next exchange rate from
    /// the mirror node.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="consensus">
    /// Optional consensus timestamp to retrieve the exchange rate
    /// at or before the given time.  If not specified, returns the current rate.
    /// </param>
    /// <returns>
    /// Exchange rate information for the current and next
    /// rate utilized by the network for determining
    /// transaction and gas fees.
    /// </returns>
    public static Task<ExchangeRateData?> GetExchangeRateAsync(this MirrorRestClient client, ConsensusTimeStamp? consensus = null)
    {
        if (consensus == null)
        {
            return client.GetSingleItemAsync("network/exchangerate", MirrorJsonContext.Default.ExchangeRateData);
        }
        else
        {
            var path = GenerateInitialPath($"network/exchangerate", [TimestampFilter.OnOrBefore(consensus.Value)]);
            return client.GetSingleItemAsync(path, MirrorJsonContext.Default.ExchangeRateData);
        }
    }
}