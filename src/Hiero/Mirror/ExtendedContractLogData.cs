// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Paging;
using Hiero.Mirror.Implementation;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;

/// <summary>
/// Represents the log results from an EVM contract call
/// with extended information identifying which transaction
/// and block this log event belongs to.
/// </summary>
public class ExtendedContractLogData : ContractLogData
{
    /// <summary>
    /// The 48-byte SHA-384 record-file hash of the block this log
    /// event was emitted in.
    /// </summary>
    /// <remarks>
    /// Hedera block hashes are SHA-384 outputs (48 bytes / 96 hex
    /// chars on the wire), not 32-byte EVM-style hashes — see the
    /// remarks on <see cref="EvmHash"/>.
    /// </remarks>
    [JsonPropertyName("block_hash")]
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> BlockHash { get; set; }
    /// <summary>
    /// The Block Number containing this log entry
    /// </summary>
    [JsonPropertyName("block_number")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long BlockNumber { get; set; }
    /// <summary>
    /// The associated transaction’s consensus timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public ConsensusTimeStamp Consensus { get; set; }
    /// <summary>
    /// The Hash of the TransactionId
    /// </summary>
    [JsonPropertyName("transaction_hash")]
    public EvmHash TransactionHash { get; set; } = EvmHash.None;
    /// <summary>
    /// The transaction index within the block for this log record
    /// </summary>
    [JsonPropertyName("transaction_index")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long TransactionIndex { get; set; }
    /// <summary>
    /// The ID of the contract that was called
    /// externally, may be different than the contract
    /// that emitted this event.
    /// </summary>
    [JsonPropertyName("root_contract_id")]
    public EntityId RootContract { get; set; } = default!;
}
/// <summary>
/// Extension methods for querying contract log data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ExtendedContractLogDataExtensions
{
    /// <summary>
    /// Enumerates log events emitted by a specific contract. Narrow
    /// by <see cref="TimestampFilter"/>, event-log topic via
    /// <see cref="EvmTopicFilter"/>, originating transaction hash
    /// via <see cref="TransactionHashFilter"/>, or block-local
    /// position via <see cref="ContractLogIndexFilter"/>.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="contract">
    /// The entityId of the contract.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters. The endpoint supports
    /// <see cref="TimestampFilter"/>, <see cref="EvmTopicFilter"/>,
    /// <see cref="TransactionHashFilter"/>,
    /// <see cref="ContractLogIndexFilter"/>, <see cref="PageLimit"/>,
    /// and <see cref="OrderBy"/>.
    /// </param>
    /// <returns>An async enumerable of contract log events meeting the given criteria.</returns>
    /// <remarks>
    /// The mirror node requires a <see cref="TimestampFilter"/> to
    /// accompany any <see cref="ContractLogIndexFilter"/> (or any
    /// <see cref="EvmTopicFilter"/>); passing the index or topic
    /// filter alone will yield a server-side 400.
    /// </remarks>
    public static IAsyncEnumerable<ExtendedContractLogData> GetContractLogEventsAsync(this MirrorRestClient client, EntityId contract, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"contracts/{MirrorFormat(contract)}/results/logs", [new PageLimit(100), .. filters]);
        return client.GetPagedItemsAsync<ExtendedContractLogDataPage, ExtendedContractLogData>(path, MirrorJsonContext.Default.ExtendedContractLogDataPage);
    }
    /// <summary>
    /// Enumerates log events across every contract on the network.
    /// Same filter palette as
    /// <see cref="GetContractLogEventsAsync"/>, without the
    /// per-contract scoping.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters. The endpoint supports
    /// <see cref="TimestampFilter"/>, <see cref="EvmTopicFilter"/>,
    /// <see cref="TransactionHashFilter"/>,
    /// <see cref="ContractLogIndexFilter"/>, <see cref="PageLimit"/>,
    /// and <see cref="OrderBy"/>.
    /// </param>
    /// <returns>An async enumerable of contract log events meeting the given criteria.</returns>
    /// <remarks>
    /// The mirror node requires a <see cref="TimestampFilter"/> to
    /// accompany any <see cref="ContractLogIndexFilter"/> (or any
    /// <see cref="EvmTopicFilter"/>); passing the index or topic
    /// filter alone will yield a server-side 400.
    /// </remarks>
    public static IAsyncEnumerable<ExtendedContractLogData> GetAllContractLogEventsAsync(this MirrorRestClient client, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"contracts/results/logs", [new PageLimit(100), .. filters]);
        return client.GetPagedItemsAsync<ExtendedContractLogDataPage, ExtendedContractLogData>(path, MirrorJsonContext.Default.ExtendedContractLogDataPage);
    }
}