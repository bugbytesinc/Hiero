using Hiero.Converters;
using Hiero.Mirror.Filters;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// Fee Info
/// </summary>
public class NetworkFee
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
/// Represents Network Fees
/// </summary>
public class NetworkFeeData
{
    /// <summary>
    /// The list of fees and basic gas charged.
    /// </summary>
    [JsonPropertyName("fees")]
    public NetworkFee[] Fees { get; set; } = default!;
    /// <summary>
    /// Timestamp at which this information
    /// was generated.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public ConsensusTimeStamp Timestamp { get; set; }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class NetworkFeeExtensions
{
    /// <summary>
    /// Retreives the network fee data for the given timestamp.
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
    public static Task<NetworkFeeData?> GetNetworkFees(this MirrorRestClient client, ConsensusTimeStamp consensus)
    {
        var path = GenerateInitialPath($"network/fees", [new TimestampOnOrBeforeFilter(consensus)]);
        return client.GetSingleItemAsync<NetworkFeeData>(path);
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
    public static Task<NetworkFeeData?> GetLatestNetworkFeesAsync(this MirrorRestClient client)
    {
        return client.GetSingleItemAsync<NetworkFeeData>("network/fees");
    }
}