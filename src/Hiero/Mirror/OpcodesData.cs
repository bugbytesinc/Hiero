// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// The opcode-level trace of a historical EVM transaction — re-executed
/// on the mirror node to produce a step-by-step record of every opcode
/// that ran, along with the final stack / memory / storage state as
/// controlled by the request's projection filters. Produced by
/// <see cref="OpcodesDataExtensions.GetContractOpcodesByTransactionHashAsync"/>
/// and <see cref="OpcodesDataExtensions.GetContractOpcodesByTransactionIdAsync"/>.
/// </summary>
public class OpcodesData
{
    /// <summary>
    /// The EVM address of the transaction recipient. The zero address
    /// is set for transactions without a recipient (e.g. contract create).
    /// </summary>
    [JsonPropertyName("address")]
    public EvmAddress Address { get; set; } = default!;
    /// <summary>
    /// The contract account id that was executed.
    /// </summary>
    [JsonPropertyName("contract_id")]
    public EntityId Contract { get; set; } = default!;
    /// <summary>
    /// Whether the transaction failed to complete processing.
    /// </summary>
    [JsonPropertyName("failed")]
    public bool Failed { get; set; }
    /// <summary>
    /// Total gas consumed by the transaction, in gas units.
    /// </summary>
    [JsonPropertyName("gas")]
    public long Gas { get; set; }
    /// <summary>
    /// The step-by-step opcode trace produced by the EVM logger.
    /// </summary>
    [JsonPropertyName("opcodes")]
    public OpcodeData[] Opcodes { get; set; } = default!;
    /// <summary>
    /// Bytes returned from the transaction's top-level call.
    /// </summary>
    [JsonPropertyName("return_value")]
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> ReturnValue { get; set; }
}
/// <summary>
/// A single opcode-execution entry in an <see cref="OpcodesData"/> trace.
/// </summary>
/// <remarks>
/// The <see cref="Memory"/>, <see cref="Stack"/>, and <see cref="Storage"/>
/// fields are populated only when their respective projection filters
/// (<see cref="OpcodeMemoryProjectionFilter"/>,
/// <see cref="OpcodeStackProjectionFilter"/>,
/// <see cref="OpcodeStorageProjectionFilter"/>) were set to include
/// them on the request. Otherwise they arrive null.
/// </remarks>
public class OpcodeData
{
    /// <summary>
    /// The call-stack depth at which this opcode executed.
    /// </summary>
    [JsonPropertyName("depth")]
    public int Depth { get; set; }
    /// <summary>
    /// Remaining gas available when this opcode began executing.
    /// </summary>
    [JsonPropertyName("gas")]
    public long Gas { get; set; }
    /// <summary>
    /// Gas cost charged for executing this specific opcode.
    /// </summary>
    [JsonPropertyName("gas_cost")]
    public long GasCost { get; set; }
    /// <summary>
    /// The EVM memory, one 32-byte word per array element.
    /// Null when memory was not requested on the tracing call.
    /// </summary>
    [JsonPropertyName("memory")]
    [JsonConverter(typeof(HexStringArraytoBytesArrayConverter))]
    public ReadOnlyMemory<byte>[]? Memory { get; set; }
    /// <summary>
    /// The mnemonic name of the opcode (e.g. <c>PUSH1</c>, <c>SLOAD</c>).
    /// </summary>
    [JsonPropertyName("op")]
    public string Op { get; set; } = default!;
    /// <summary>
    /// Program counter at which this opcode executed.
    /// </summary>
    [JsonPropertyName("pc")]
    public int Pc { get; set; }
    /// <summary>
    /// Revert reason bytes, when the opcode is a revert. Null otherwise.
    /// </summary>
    [JsonPropertyName("reason")]
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> Reason { get; set; }
    /// <summary>
    /// The EVM stack, one 32-byte word per array element. Null when
    /// stack information was explicitly excluded on the tracing call.
    /// </summary>
    [JsonPropertyName("stack")]
    [JsonConverter(typeof(HexStringArraytoBytesArrayConverter))]
    public ReadOnlyMemory<byte>[]? Stack { get; set; }
    /// <summary>
    /// Storage slots read or written by this opcode, keyed by slot
    /// hash (preserved as its on-the-wire hex-string form, since
    /// EVM storage keys are conventionally displayed as hex).
    /// Null when storage was not requested on the tracing call.
    /// </summary>
    [JsonPropertyName("storage")]
    [JsonConverter(typeof(HexStringDictionaryToBytesConverter))]
    public Dictionary<string, ReadOnlyMemory<byte>>? Storage { get; set; }
}
/// <summary>
/// Extension methods exposing the mirror node's opcode-trace endpoint.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class OpcodesDataExtensions
{
    /// <summary>
    /// Retrieves the opcode-level execution trace of a historical
    /// transaction, identified by its EVM transaction hash. The mirror
    /// node re-executes the transaction on the EVM to produce the
    /// trace — for busy transactions this can take several seconds,
    /// especially when memory or stack projections are requested.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="evmTransactionHash">
    /// The EVM transaction hash (not to be confused with the raw HAPI
    /// transaction hash).
    /// </param>
    /// <param name="filters">
    /// Optional projection toggles recognized by this endpoint —
    /// <see cref="OpcodeStackProjectionFilter"/>,
    /// <see cref="OpcodeMemoryProjectionFilter"/>, and
    /// <see cref="OpcodeStorageProjectionFilter"/>. Server defaults
    /// are include-stack and exclude-memory / exclude-storage.
    /// </param>
    /// <returns>
    /// The opcode trace, or null if the transaction was not found.
    /// </returns>
    public static Task<OpcodesData?> GetContractOpcodesByTransactionHashAsync(this MirrorRestClient client, EvmHash evmTransactionHash, params IMirrorProjection[] filters)
    {
        var path = GenerateInitialPath($"contracts/results/{evmTransactionHash}/opcodes", filters);
        return client.GetSingleItemAsync(path, MirrorJsonContext.Default.OpcodesData);
    }
    /// <summary>
    /// Retrieves the opcode-level execution trace of a historical
    /// transaction, identified by its HAPI transaction id. The mirror
    /// node re-executes the transaction on the EVM to produce the
    /// trace — for busy transactions this can take several seconds,
    /// especially when memory or stack projections are requested.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="transactionId">
    /// The HAPI transaction id, not the EVM transaction hash.
    /// </param>
    /// <param name="filters">
    /// Optional projection toggles recognized by this endpoint —
    /// <see cref="OpcodeStackProjectionFilter"/>,
    /// <see cref="OpcodeMemoryProjectionFilter"/>, and
    /// <see cref="OpcodeStorageProjectionFilter"/>. Server defaults
    /// are include-stack and exclude-memory / exclude-storage.
    /// </param>
    /// <returns>
    /// The opcode trace, or null if the transaction was not found.
    /// </returns>
    public static Task<OpcodesData?> GetContractOpcodesByTransactionIdAsync(this MirrorRestClient client, TransactionId transactionId, params IMirrorProjection[] filters)
    {
        var (txId, txFilters) = MirrorFormat(transactionId);
        var path = GenerateInitialPath($"contracts/results/{txId}/opcodes", [.. txFilters, .. filters]);
        return client.GetSingleItemAsync(path, MirrorJsonContext.Default.OpcodesData);
    }
}
