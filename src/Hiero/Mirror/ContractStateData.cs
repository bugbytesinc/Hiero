// SPDX-License-Identifier: Apache-2.0
using Hiero;
using Hiero.Converters;
using Hiero.Mirror;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using Hiero.Mirror.Paging;
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
    /// Retrieves the value stored at a specific storage slot of a
    /// contract from <c>/api/v1/contracts/{id}/state</c>. The
    /// <paramref name="position"/> is converted internally via
    /// <see cref="SlotFilter"/>; use <see cref="TimestampFilter"/> to
    /// read the slot's value at a historical consensus instant.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="contract">
    /// The EntityId of the contract.
    /// </param>
    /// <param name="position">
    /// The storage slot position as a 32-byte big-endian integer.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters. The endpoint supports
    /// <see cref="TimestampFilter"/>, <see cref="PageLimit"/>, and
    /// <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// The slot record at the requested position, or null if the slot
    /// has not been written.
    /// </returns>
    public static async Task<ContractStateData?> GetContractStateAsync(this MirrorRestClient client, EntityId contract, BigInteger position, IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"contracts/{MirrorFormat(contract)}/state", [SlotFilter.Is(position), .. filters]);
        var list = await client.GetSingleItemAsync(path, MirrorJsonContext.Default.ContractStateDataPage).ConfigureAwait(false);
        return list?.States?.FirstOrDefault();
    }
}