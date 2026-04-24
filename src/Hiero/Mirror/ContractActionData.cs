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
/// A single entry in the call graph of a historical contract
/// transaction — one per nested EVM call (CALL, CREATE,
/// DELEGATECALL, precompile invocation, etc.). Retrieved via
/// <see cref="ContractActionDataExtensions.GetContractActionsByTransactionHashAsync"/>
/// and <see cref="ContractActionDataExtensions.GetContractActionsByTransactionIdAsync"/>.
/// </summary>
public class ContractActionData
{
    /// <summary>
    /// The nesting depth at which this call executed. Zero for the
    /// top-level call; each nested call increments by one.
    /// </summary>
    [JsonPropertyName("call_depth")]
    public int CallDepth { get; set; }
    /// <summary>
    /// The EVM operation used to initiate this call: one of
    /// <c>CALL</c>, <c>CALLCODE</c>, <c>CREATE</c>, <c>CREATE2</c>,
    /// <c>DELEGATECALL</c>, <c>STATICCALL</c>, or <c>UNKNOWN</c>.
    /// Kept as a raw string to match existing AOT-friendly precedent
    /// on returned-enum fields.
    /// </summary>
    [JsonPropertyName("call_operation_type")]
    public string CallOperationType { get; set; } = default!;
    /// <summary>
    /// The semantic category of the call: one of <c>NO_ACTION</c>,
    /// <c>CALL</c>, <c>CREATE</c>, <c>PRECOMPILE</c>, or
    /// <c>SYSTEM</c>. Kept as a raw string for AOT-friendliness.
    /// </summary>
    [JsonPropertyName("call_type")]
    public string CallType { get; set; } = default!;
    /// <summary>
    /// The HAPI account/contract id that initiated this call.
    /// </summary>
    [JsonPropertyName("caller")]
    public EntityId Caller { get; set; } = default!;
    /// <summary>
    /// The entity type of the caller: <c>ACCOUNT</c> or <c>CONTRACT</c>.
    /// </summary>
    [JsonPropertyName("caller_type")]
    public string CallerType { get; set; } = default!;
    /// <summary>
    /// The EVM address of the caller. Always populated by the server.
    /// </summary>
    [JsonPropertyName("from")]
    [JsonConverter(typeof(EvmAddressConverter))]
    public EvmAddress From { get; set; } = default!;
    /// <summary>
    /// Gas made available to this call (in gas units).
    /// </summary>
    [JsonPropertyName("gas")]
    public long Gas { get; set; }
    /// <summary>
    /// Gas actually consumed by this call (in gas units).
    /// </summary>
    [JsonPropertyName("gas_used")]
    public long GasUsed { get; set; }
    /// <summary>
    /// The position of this action within the ordered list of actions
    /// for the parent transaction. Used with
    /// <see cref="ContractActionIndexFilter"/> to page or seek.
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }
    /// <summary>
    /// The call's input data (EVM calldata or contract-creation
    /// bytecode). Null when the server did not record input data.
    /// </summary>
    [JsonPropertyName("input")]
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> Input { get; set; }
    /// <summary>
    /// The HAPI account/contract id of the recipient, when known.
    /// </summary>
    [JsonPropertyName("recipient")]
    public EntityId Recipient { get; set; } = default!;
    /// <summary>
    /// The entity type of the recipient: <c>ACCOUNT</c> or
    /// <c>CONTRACT</c>. Null when the recipient type was not
    /// determined (e.g., some failed CREATE actions).
    /// </summary>
    [JsonPropertyName("recipient_type")]
    public string? RecipientType { get; set; }
    /// <summary>
    /// Data returned from this call, when any. Null when no result
    /// data was recorded.
    /// </summary>
    [JsonPropertyName("result_data")]
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> ResultData { get; set; }
    /// <summary>
    /// The semantic kind of <see cref="ResultData"/>: one of
    /// <c>OUTPUT</c> (normal return), <c>REVERT_REASON</c>, or
    /// <c>ERROR</c>.
    /// </summary>
    [JsonPropertyName("result_data_type")]
    public string ResultDataType { get; set; } = default!;
    /// <summary>
    /// The consensus timestamp at which the parent transaction
    /// executed (shared across all actions in the same trace).
    /// </summary>
    [JsonPropertyName("timestamp")]
    public ConsensusTimeStamp Timestamp { get; set; }
    /// <summary>
    /// The EVM address of the recipient. Null when the server
    /// sent an explicit null — distinct from the literal zero
    /// address <c>0x0000…0000</c>, which carries meaning for
    /// contract-create actions and other edge cases.
    /// </summary>
    [JsonPropertyName("to")]
    [JsonConverter(typeof(NullableEvmAddressConverter))]
    public EvmAddress? To { get; set; }
    /// <summary>
    /// The HBAR value transferred with this call, in tinybars.
    /// </summary>
    [JsonPropertyName("value")]
    public long Value { get; set; }
}
/// <summary>
/// Extension methods exposing the mirror node's contract-action endpoint.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ContractActionDataExtensions
{
    /// <summary>
    /// Retrieves the ordered call graph of a historical contract
    /// transaction, identified by its EVM transaction hash.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="evmTransactionHash">
    /// The EVM transaction hash (not to be confused with the raw
    /// HAPI transaction hash).
    /// </param>
    /// <param name="filters">
    /// Optional filters — <see cref="ContractActionIndexFilter"/>
    /// for narrowing the action position, plus the standard
    /// <c>PageLimit</c> / <c>OrderBy</c> paging directives.
    /// </param>
    /// <returns>
    /// The sequence of contract actions for the transaction; empty if
    /// the transaction has no recorded actions or was not found.
    /// </returns>
    public static IAsyncEnumerable<ContractActionData> GetContractActionsByTransactionHashAsync(this MirrorRestClient client, EvmHash evmTransactionHash, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"contracts/results/{evmTransactionHash}/actions", [new PageLimit(100), .. filters]);
        return client.GetPagedItemsAsync<ContractActionDataPage, ContractActionData>(path, MirrorJsonContext.Default.ContractActionDataPage);
    }
    /// <summary>
    /// Retrieves the ordered call graph of a historical contract
    /// transaction, identified by its HAPI transaction id.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="transactionId">
    /// The HAPI transaction id, not the EVM transaction hash.
    /// </param>
    /// <param name="filters">
    /// Optional filters — <see cref="ContractActionIndexFilter"/>
    /// for narrowing the action position, plus the standard
    /// <c>PageLimit</c> / <c>OrderBy</c> paging directives.
    /// </param>
    /// <returns>
    /// The sequence of contract actions for the transaction; empty if
    /// the transaction has no recorded actions or was not found.
    /// </returns>
    public static IAsyncEnumerable<ContractActionData> GetContractActionsByTransactionIdAsync(this MirrorRestClient client, TransactionId transactionId, params IMirrorQueryParameter[] filters)
    {
        var (txId, txFilters) = MirrorFormat(transactionId);
        var path = GenerateInitialPath($"contracts/results/{txId}/actions", [new PageLimit(100), .. txFilters, .. filters]);
        return client.GetPagedItemsAsync<ContractActionDataPage, ContractActionData>(path, MirrorJsonContext.Default.ContractActionDataPage);
    }
}
