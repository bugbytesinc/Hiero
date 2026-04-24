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
/// Contract information retrieved from a mirror node.
/// </summary>
public class ContractData
{
    /// <summary>
    /// The public key which controls access.
    /// </summary>
    [JsonPropertyName("admin_key")]
    public Endorsement? Endorsement { get; set; }
    /// <summary>
    /// The ID of the associated auto renew account
    /// </summary>
    [JsonPropertyName("auto_renew_account")]
    public EntityId? AutoRenewAccount { get; set; }
    /// <summary>
    /// Contract Auto-Renew Period in seconds.
    /// </summary>
    [JsonPropertyName("auto_renew_period")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long AutoRenewPeriod { get; set; }
    /// <summary>
    /// The HAPI ID of the Contract
    /// </summary>
    [JsonPropertyName("contract_id")]
    public EntityId HapiAddress { get; set; } = default!;
    /// <summary>
    /// Consensus Timestamp when this contract was created
    /// </summary>
    [JsonPropertyName("created_timestamp")]
    public ConsensusTimeStamp Created { get; set; }
    /// <summary>
    /// Flag indicating that the contract has been deleted.
    /// </summary>
    [JsonPropertyName("deleted")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool Deleted { get; set; }
    /// <summary>
    /// The contract's public address encoded
    /// for use with the contract EVM.
    /// </summary>
    [JsonPropertyName("evm_address")]
    public EvmAddress EvmAddress { get; set; } = default!;
    /// <summary>
    /// Timestamp at which the network will try to 
    /// renew the contract rent or delete the contract
    /// if there are no funds to extend its lifetime.
    /// </summary>
    [JsonPropertyName("expiration_timestamp")]
    public ConsensusTimeStamp Expiration { get; set; }
    /// <summary>
    /// The HAPI ID of the file that created the contract.
    /// </summary>
    [JsonPropertyName("file_id")]
    public EntityId File { get; set; } = default!;
    /// <summary>
    /// The number of auto-associations for this contract.
    /// </summary>
    [JsonPropertyName("max_automatic_token_associations")]
    [JsonConverter(typeof(IntMirrorConverter))]
    public int Associations { get; set; }
    /// <summary>
    /// The contract's memo.
    /// </summary>
    [JsonPropertyName("memo")]
    public string Memo { get; set; } = default!;
    /// <summary>
    /// The current contract nonce — the number of
    /// contracts this contract has deployed via
    /// <c>CREATE</c> / <c>CREATE2</c>.
    /// </summary>
    [JsonPropertyName("nonce")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Nonce { get; set; }
    /// <summary>
    /// The HAPI ID of the obtainer?
    /// </summary>
    [JsonPropertyName("obtainer_id")]
    public EntityId Obtainer { get; set; } = default!;
    /// <summary>
    /// Flag indicating permanent removal?
    /// </summary>
    [JsonPropertyName("permanent_removal")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool PermanentRemoval { get; set; }
    /// <summary>
    /// The ID of the proxy account
    /// </summary>
    [JsonPropertyName("proxy_account_id")]
    public EntityId? ProxyAccount { get; set; }
    /// <summary>
    /// The consensus timestamp range this
    /// data record covers.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public TimestampRangeData TimestampRange { get; set; } = default!;
    /// <summary>
    /// The contract bytecode in hex during deployment
    /// </summary>
    [JsonPropertyName("bytecode")]
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> Bytecode { get; set; }
    /// <summary>
    /// The contract bytecode in hex after deployment
    /// </summary>
    [JsonPropertyName("runtime_bytecode")]
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> RuntimeBytecode { get; set; }
}
/// <summary>
/// Extension methods for querying contract data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ContractDataExtensions
{
    /// <summary>
    /// Retrieves the full contract information from
    /// <c>/api/v1/contracts/{id}</c>, including both
    /// <see cref="ContractData.Bytecode"/> and
    /// <see cref="ContractData.RuntimeBytecode"/> (which the list
    /// endpoint omits). Use <see cref="TimestampFilter"/> to retrieve
    /// the contract's state at a historical consensus instant.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="contract">
    /// The entityId of the contract to retrieve.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters. The endpoint supports
    /// <see cref="TimestampFilter"/>.
    /// </param>
    /// <returns>
    /// The contract information, or null if not found.
    /// </returns>
    public static Task<ContractData?> GetContractAsync(this MirrorRestClient client, EntityId contract, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"contracts/{MirrorFormat(contract)}", filters);
        return client.GetSingleItemAsync(path, MirrorJsonContext.Default.ContractData);
    }
    /// <summary>
    /// Enumerates smart-contract entities across the network from
    /// <c>/api/v1/contracts</c>. Use <see cref="ContractFilter"/> to
    /// narrow by contract id (or id range). Newest-first by default;
    /// pass <see cref="OrderBy.Ascending"/> to reverse.
    /// </summary>
    /// <remarks>
    /// The list endpoint omits <see cref="ContractData.Bytecode"/>
    /// and <see cref="ContractData.RuntimeBytecode"/> — these
    /// come back empty here. Call
    /// <see cref="GetContractAsync(MirrorRestClient, EntityId, IMirrorQueryParameter[])"/>
    /// on a specific id to retrieve them.
    /// </remarks>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="filters">
    /// Additional query filters. The endpoint supports
    /// <see cref="ContractFilter"/>, <see cref="PageLimit"/>,
    /// and <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of contract records.
    /// </returns>
    public static IAsyncEnumerable<ContractData> GetContractsAsync(this MirrorRestClient client, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath("contracts", [new PageLimit(100), .. filters]);
        return client.GetPagedItemsAsync<ContractDataPage, ContractData>(path, MirrorJsonContext.Default.ContractDataPage);
    }
}