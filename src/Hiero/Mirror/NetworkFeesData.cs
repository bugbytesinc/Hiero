// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// A single network fee entry: gas price paired with a transaction type.
/// </summary>
public class NetworkFeeData
{
    /// <summary>
    /// The basic charge in gas for the paired transaction type.
    /// </summary>
    [JsonPropertyName("gas")]
    [JsonConverter(typeof(UnsignedLongMirrorConverter))]
    public ulong GasPrice { get; set; }
    /// <summary>
    /// The name of the transaction type
    /// </summary>
    [JsonPropertyName("transaction_type")]
    public string TransactionType { get; set; } = default!;
}
/// <summary>
/// Snapshot of the network fee schedule at a given timestamp.
/// </summary>
public class NetworkFeesData
{
    /// <summary>
    /// The list of fees and basic gas charged per transaction type.
    /// </summary>
    [JsonPropertyName("fees")]
    public NetworkFeeData[] Fees { get; set; } = default!;
    /// <summary>
    /// Timestamp at which this information
    /// was generated.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public ConsensusTimeStamp Timestamp { get; set; }
}
/// <summary>
/// Extension methods for querying network fee data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class NetworkFeesExtensions
{
    /// <summary>
    /// Retrieves the network fee data for the given timestamp.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="consensus">
    /// Timestamp to attempt to retrieve the network fee data.
    /// </param>
    /// <returns>
    /// Network fee data for the timestamp, or null if not found.
    /// </returns>
    public static Task<NetworkFeesData?> GetNetworkFeesAsync(this MirrorRestClient client, ConsensusTimeStamp consensus)
    {
        var path = GenerateInitialPath($"network/fees", [new TimestampOnOrBeforeFilter(consensus)]);
        return client.GetSingleItemAsync<NetworkFeesData>(path, MirrorJsonContext.Default.NetworkFeesData);
    }
    /// <summary>
    /// Retrieves the latest network fee data from the ledger.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <returns>
    /// The current Network fee data or null if not found.
    /// </returns>
    public static Task<NetworkFeesData?> GetLatestNetworkFeesAsync(this MirrorRestClient client)
    {
        return client.GetSingleItemAsync<NetworkFeesData>("network/fees", MirrorJsonContext.Default.NetworkFeesData);
    }
}
