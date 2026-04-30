// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using Hiero.Mirror.Paging;
using System.ComponentModel;
using System.Text.Json.Serialization;
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
    /// The 48-byte SHA-384 record-file hash of the block.
    /// </summary>
    /// <remarks>
    /// Hedera block hashes are SHA-384 outputs (48 bytes / 96 hex
    /// chars on the wire), not 32-byte EVM-style hashes — see the
    /// remarks on <see cref="EvmHash"/>.
    /// </remarks>
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
    /// The 48-byte SHA-384 record-file hash of the previous block.
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
/// <summary>
/// Extension methods for querying block data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class BlockDataExtensions
{
    /// <summary>
    /// Retrieves a single block by number from
    /// <c>/api/v1/blocks/{blockNumber}</c>.
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
        return client.GetSingleItemAsync($"blocks/{blockNumber}", MirrorJsonContext.Default.BlockData);
    }
    /// <summary>
    /// Retrieves a single block by hash from
    /// <c>/api/v1/blocks/{blockHash}</c>.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="blockhash">
    /// The block hash bytes — accepts the 48-byte Hedera SHA-384
    /// record-file hash returned by the mirror, or a 32-byte EVM
    /// block hash for callers that have one.
    /// </param>
    /// <returns>
    /// Information for the block, or null if not found.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="blockhash"/> is neither 32 nor 48 bytes long.
    /// </exception>
    public static Task<BlockData?> GetBlockAsync(this MirrorRestClient client, ReadOnlyMemory<byte> blockhash)
    {
        if (blockhash.Length != 32 && blockhash.Length != 48)
        {
            throw new ArgumentOutOfRangeException(nameof(blockhash), "Block hash must be 32 bytes (EVM) or 48 bytes (Hedera SHA-384).");
        }
        return client.GetSingleItemAsync($"blocks/0x{Hex.FromBytes(blockhash.Span)}", MirrorJsonContext.Default.BlockData);
    }
    /// <summary>
    /// Retrieves the most recent block observed by the mirror node via
    /// <c>/api/v1/blocks?limit=1&amp;order=desc</c> (there is no
    /// dedicated "latest block" endpoint).
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
        var list = await client.GetSingleItemAsync("blocks?limit=1&order=desc", MirrorJsonContext.Default.BlockDataPage).ConfigureAwait(false);
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
        var path = GenerateInitialPath($"blocks", [new PageLimit(1), OrderBy.Descending, TimestampFilter.OnOrBefore(consensus)]);
        var list = await client.GetSingleItemAsync(path, MirrorJsonContext.Default.BlockDataPage).ConfigureAwait(false);
        return list?.Blocks?.FirstOrDefault();
    }
    /// <summary>
    /// Enumerates blocks from the chain. Default ordering is
    /// newest-first; pass <see cref="OrderBy.Ascending"/> to
    /// reverse. Pair with <see cref="BlockNumberFilter"/> or
    /// <see cref="TimestampFilter"/> to bound a range.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="filters">
    /// Additional query filters. The endpoint supports
    /// <see cref="BlockNumberFilter"/>,
    /// <see cref="TimestampFilter"/>, <see cref="PageLimit"/>,
    /// and <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of block records.
    /// </returns>
    public static IAsyncEnumerable<BlockData> GetBlocksAsync(this MirrorRestClient client, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath("blocks", [new PageLimit(100), .. filters]);
        return client.GetPagedItemsAsync<BlockDataPage, BlockData>(path, MirrorJsonContext.Default.BlockDataPage);
    }
}