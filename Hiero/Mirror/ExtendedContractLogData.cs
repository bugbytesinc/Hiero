using Hiero.Converters;
using Hiero.Mirror.Filters;
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
    /// The Block Hash of the TransactionId
    /// </summary>
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
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> TransactionHash { get; set; }
    /// <summary>
    /// The TransactionId Block SegmentIndex for this log record
    /// </summary>
    [JsonPropertyName("transaction_index")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long TransactionIndex { get; set; }
    /// <summary>
    /// Payer of the contract that was called 
    /// externally, may be different than the contract
    /// that emitted this event.
    /// </summary>
    [JsonPropertyName("root_contract_id")]
    public EntityId RootContract { get; set; } = default!;
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ExtendedContractLogDataExtensions
{
    /// <summary>
    /// Retrieves the log events for a contract
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="contract">
    /// The entityId of the contract
    /// </param>
    /// <param name="filters">
    /// Additional query filters if desired.
    /// </param>
    /// <returns></returns>
    public static IAsyncEnumerable<ExtendedContractLogData> GetLogEventsForContractAsync(this MirrorRestClient client, EntityId contract, params IMirrorQueryFilter[] filters)
    {
        var path = GenerateInitialPath($"contracts/{MirrorFormat(contract)}/results/logs", [new LimitFilter(100), .. filters]);
        return client.GetPagedItemsAsync<ExtendedContractLogDataPage, ExtendedContractLogData>(path);
    }
    /// <summary>
    /// Retrieves the log events for all contracts satisfying the 
    /// filters.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="filters">
    /// Additional filters
    /// </param>
    /// <returns></returns>
    public static IAsyncEnumerable<ExtendedContractLogData> GetLogEventsForAllContractsAsync(this MirrorRestClient client, params IMirrorQueryFilter[] filters)
    {
        var path = GenerateInitialPath($"contracts/results/logs", [new LimitFilter(100), .. filters]);
        return client.GetPagedItemsAsync<ExtendedContractLogDataPage, ExtendedContractLogData>(path);
    }
}