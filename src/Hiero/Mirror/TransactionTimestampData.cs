// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8618 
using Hiero.Mirror.Implementation;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;
/// <summary>
/// Helper Class for retrieving just the timestamp from the transaction list.
/// </summary>
internal class TransactionTimestampData
{
    /// <summary>
    /// The transaction’s consensus timestamp.
    /// </summary>
    [JsonPropertyName("consensus_timestamp")]
    public ConsensusTimeStamp Consensus { get; set; }
}
/// <summary>
/// Extension methods for retrieving the latest consensus timestamp from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TransactionTimestampDataExtensions
{
    /// <summary>
    /// Retrieves the most recent consensus timestamp observed by the
    /// mirror node via
    /// <c>/api/v1/transactions?limit=1&amp;order=desc</c>. There is no
    /// dedicated "now" endpoint on the mirror node; this is the
    /// canonical way to get a current-ish network clock.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <returns>
    /// The latest consensus timestamp known by the mirror node, or
    /// <see cref="ConsensusTimeStamp.MinValue"/> if the mirror has no
    /// transactions recorded yet (e.g., freshly-started local node).
    /// </returns>
    public static async Task<ConsensusTimeStamp> GetLatestConsensusTimestampAsync(this MirrorRestClient client)
    {
        var list = await client.GetSingleItemAsync("transactions?limit=1&order=desc", MirrorJsonContext.Default.TransactionTimestampDataPage);
        if (list?.Transactions?.Length > 0)
        {
            return list.Transactions[0].Consensus;
        }
        return ConsensusTimeStamp.MinValue;
    }
}