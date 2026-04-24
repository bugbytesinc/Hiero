// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// Network-wide HBAR supply totals as reported by the mirror node.
/// Values are tinybars.
/// </summary>
/// <remarks>
/// On the public Hedera mainnet and testnet the total HBAR supply is
/// fixed (50 billion HBAR = 5×10¹⁸ tinybars), so the interesting field
/// for most callers is <see cref="ReleasedSupply"/> — the portion of
/// that total already released into circulation. On a custom Hiero
/// network the totals can change over time; querying at a specific
/// <c>timestamp</c> returns the supply state as of that consensus
/// instant.
/// </remarks>
public class NetworkSupplyData
{
    /// <summary>
    /// The portion of the network's total supply that is already
    /// released into circulation, in tinybars.
    /// </summary>
    [JsonPropertyName("released_supply")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long ReleasedSupply { get; set; }
    /// <summary>
    /// The consensus timestamp at which these supply values were
    /// valid.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public ConsensusTimeStamp Timestamp { get; set; }
    /// <summary>
    /// The network's total supply of HBAR, in tinybars.
    /// </summary>
    [JsonPropertyName("total_supply")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long TotalSupply { get; set; }
}
/// <summary>
/// Extension methods for querying network supply data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class NetworkSupplyDataExtensions
{
    /// <summary>
    /// Retrieves the network's HBAR supply totals at or before the
    /// given consensus timestamp. When <paramref name="consensus"/>
    /// is null, returns the most recent values.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="consensus">
    /// Optional consensus timestamp — returns the supply state as of
    /// <c>timestamp=lte:{consensus}</c>. Leave null for the latest
    /// values.
    /// </param>
    /// <returns>
    /// The network supply data, or null if not found.
    /// </returns>
    public static Task<NetworkSupplyData?> GetNetworkSupplyAsync(this MirrorRestClient client, ConsensusTimeStamp? consensus = null)
    {
        if (consensus == null)
        {
            return client.GetSingleItemAsync("network/supply", MirrorJsonContext.Default.NetworkSupplyData);
        }
        var path = GenerateInitialPath("network/supply", [TimestampFilter.OnOrBefore(consensus.Value)]);
        return client.GetSingleItemAsync(path, MirrorJsonContext.Default.NetworkSupplyData);
    }
}
