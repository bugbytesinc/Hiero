using Hiero.Converters;
using Hiero.Mirror.Filters;
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
    /// Address Auto-Renew Period in seconds.
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
    /// Consensus Timestamp when this account was created
    /// </summary>
    [JsonPropertyName("created_timestamp")]
    public ConsensusTimeStamp Created { get; set; }
    /// <summary>
    /// Flag indicating that the account has been deleted.
    /// </summary>
    [JsonPropertyName("deleted")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool Deleted { get; set; }
    /// <summary>
    /// The account's public address encoded
    /// for use with the contract EVM.
    /// </summary>
    [JsonPropertyName("evm_address")]
    public EvmAddress EvmAddress { get; set; } = default!;
    /// <summary>
    /// Timestamp at which the network will try to 
    /// renew the account rent or delete the account
    /// if there are no funds to extends its lifetime.
    /// </summary>
    [JsonPropertyName("expiration_timestamp")]
    public ConsensusTimeStamp Expiration { get; set; }
    /// <summary>
    /// The HAPI ID of the file that created the contract.
    /// </summary>
    [JsonPropertyName("file_id")]
    public EntityId File { get; set; } = default!;
    /// <summary>
    /// The number of auto-associations for this account.
    /// </summary>
    [JsonPropertyName("max_automatic_token_associations")]
    [JsonConverter(typeof(IntMirrorConverter))]
    public int Associations { get; set; }
    /// <summary>
    /// The account's memo.
    /// </summary>
    [JsonPropertyName("memo")]
    public string Memo { get; set; } = default!;
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
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ContractDataExtensions
{
    /// <summary>
    /// Retrieves the hedera details for a contract.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="contract">
    /// The entityId of the contract to retrieve.
    /// </param>
    /// <param name="filters">
    /// Additional filters that may be applied.
    /// </param>
    /// <returns>
    /// The contract information satisfying any additional filters, or null if not found.
    /// </returns>
    public static Task<ContractData?> GetContractDataAsync(this MirrorRestClient client, EntityId contract, params IMirrorQueryFilter[] filters)
    {
        var path = GenerateInitialPath($"contracts/{MirrorFormat(contract)}", filters);
        return client.GetSingleItemAsync<ContractData>(path);
    }
}