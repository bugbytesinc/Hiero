#pragma warning disable CS8618 
using Hiero.Mirror.Implementation;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TransactionTimestampDataExtensions
{
    /// <summary>
    /// Retrieves the latest consensus timestamp known by the mirror node.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <returns>
    /// The latest consensus timestamp known by the mirror node.
    /// </returns>
    public static async Task<ConsensusTimeStamp> GetLatestConsensusTimestampAsync(this MirrorRestClient client)
    {
        var list = await client.GetSingleItemAsync<TransactionTimestampDataPage>("transactions?limit=1&order=desc");
        if (list?.Transactions?.Length > 0)
        {
            return list.Transactions[0].Consensus;
        }
        return ConsensusTimeStamp.MinValue;
    }
}