using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using System;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// Block information retrieved from a mirror node.
/// </summary>
public class BlockData
{
    /// <summary>
    /// Number of transactions in this block.
    /// </summary>
    [JsonPropertyName("count")]
    [JsonConverter(typeof(IntMirrorConverter))]
    public int Count { get; set; }
    /// <summary>
    /// The Hedera API version number this block
    /// was created with.
    /// </summary>
    [JsonPropertyName("hapi_version")]
    public string Version { get; set; } = default!;
    /// <summary>
    /// The Hash of the block.
    /// </summary>
    [JsonPropertyName("hash")]
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> Hash { get; set; }
    /// <summary>
    /// The filename of this block exported by 
    /// the gossip node network.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;
    /// <summary>
    /// The number identifier of this block.
    /// </summary>
    [JsonPropertyName("number")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Number { get; set; }
    /// <summary>
    /// The Hash of the previous block.
    /// </summary>
    [JsonPropertyName("previous_hash")]
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> PreviousHash { get; set; }
    /// <summary>
    /// The size of this block.
    /// </summary>
    [JsonPropertyName("size")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Size { get; set; }
    /// <summary>
    /// The consensus timestamp range this
    /// block covers.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public TimestampRangeData TimestampRange { get; set; } = default!;
    /// <summary>
    /// The amount of gas that was used.
    /// </summary>
    [JsonPropertyName("gas_used")]
    [JsonConverter(typeof(UnsignedLongMirrorConverter))]
    public ulong GasUsed { get; set; }
    /// <summary>
    /// The bloom filter for this block.
    /// </summary>
    [JsonPropertyName("logs_bloom")]
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> LogsBloom { get; set; }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class BlockDataExtensions
{
    /// <summary>
    /// Retrieves block information given the block number ID
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="blockNumber">
    /// Block Number
    /// </param>
    /// <returns>
    /// Information for the block, or null if not found.
    /// </returns>
    public static Task<BlockData?> GetBlockAsync(this MirrorRestClient client, long blockNumber)
    {
        return client.GetSingleItemAsync<BlockData>($"blocks/{blockNumber}");
    }
    /// <summary>
    /// Retrieves block information given the block blockhash.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="blockhash">
    /// The EVM Block Hash for the block to search for.
    /// </param>
    /// <returns>
    /// Information for the block, or null if not found.
    /// </returns>
    public static Task<BlockData?> GetBlockAsync(this MirrorRestClient client, ReadOnlyMemory<byte> blockhash)
    {
        return client.GetSingleItemAsync<BlockData>($"blocks/0x{Hex.FromBytes(blockhash)}");
    }
    /// <summary>
    /// Retrieves the latest block known to the remote mirror node.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <returns>
    /// Block information for the latest known block, or null if there 
    /// was an error.
    /// </returns>
    public static async Task<BlockData?> GetLatestBlockAsync(this MirrorRestClient client)
    {
        var list = await client.GetSingleItemAsync<BlockDataPage>("blocks?limit=1&order=desc").ConfigureAwait(false);
        return list?.Blocks?.FirstOrDefault();
    }
    /// <summary>
    /// Retrieves the latest known block before the given consensus timestamp.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="consensus">The consensus timestamp</param>
    /// <returns>Block info for the latest block before the given timestamp</returns>
    public static async Task<BlockData?> GetLatestBlockBeforeConsensusAsync(this MirrorRestClient client, ConsensusTimeStamp consensus)
    {
        var path = GenerateInitialPath($"blocks", [new LimitFilter(1), OrderByFilter.Descending, new TimestampOnOrBeforeFilter(consensus)]);
        var list = await client.GetSingleItemAsync<BlockDataPage>("blocks?limit=1&order=desc").ConfigureAwait(false);
        return list?.Blocks?.FirstOrDefault();
    }
}