using Hiero;
using Hiero.Converters;
using Hiero.Mirror;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using System.ComponentModel;
using System.Numerics;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror
{
    /// <summary>
    /// Represents the data for a slot on a contract.
    /// </summary>
    public class ContractStateData
    {
        /// <summary>
        /// The contract's EVM Address
        /// </summary>
        [JsonPropertyName("address")]
        public EvmAddress EvmAddress { get; set; } = default!;
        /// <summary>
        /// The contract's HAPI Address
        /// </summary>
        [JsonPropertyName("contract_id")]
        public EntityId HapiAddress { get; set; } = default!;
        /// <summary>
        /// Timestamp when data was retrieved.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public ConsensusTimeStamp StakePeriodStart { get; set; }
        /// <summary>
        /// Slot ID
        /// </summary>
        [JsonPropertyName("slot")]
        [JsonConverter(typeof(BigIntegerConverter))]
        public BigInteger Slot { get; set; }
        /// <summary>
        /// The slot data value
        /// </summary>
        [JsonPropertyName("value")]
        [JsonConverter(typeof(HexStringToBytesConverter))]
        public ReadOnlyMemory<byte> Value { get; set; }
    }
}
/// <summary>
/// Extension methods for querying contract state data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ContractStateDataExtensions
{
    /// <summary>
    /// Retrieves Contract State (slot) data.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="contract">
    /// The EntityId of the contract.
    /// </param>
    /// <param name="position">
    /// The position within the data (slot number).
    /// </param>
    /// <param name="filters">
    /// Additional filters to apply.
    /// </param>
    /// <returns>
    /// The contract data (slot information) fulfilling the search criteria, or null if not found.
    /// </returns>
    public static async Task<ContractStateData?> GetContractState(this MirrorRestClient client, EntityId contract, BigInteger position, IMirrorQueryFilter[] filters)
    {
        var path = GenerateInitialPath($"contracts/{MirrorFormat(contract)}/state", [new SlotIsFilter(position), .. filters]);
        var list = await client.GetSingleItemAsync<ContractStateDataPage>(path, MirrorJsonContext.Default.ContractStateDataPage).ConfigureAwait(false);
        return list?.States?.FirstOrDefault();
    }
}