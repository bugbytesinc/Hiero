using Hiero.Converters;
using Hiero.Implementation;
using System;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;
/// <summary>
/// Call data to be sent to the mirror node for simulation
/// </summary>
public class EvmCallData
{
    /// <summary>
    /// Typically "latest", but can be specific historical blocks 
    /// when a hexadecimal or decimal block number is provided.
    /// </summary>
    [JsonPropertyName("block")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Block { get; set; }
    /// <summary>
    /// The ABI Encoded Call Input to send to the EVM
    /// </summary>
    [JsonPropertyName("data")]
    [JsonConverter(typeof(HexStringToBytesConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ReadOnlyMemory<byte>? Data { get; set; }
    /// <summary>
    /// If set to true, gas estimation is included in
    /// the results, only valid when block is "latest".
    /// </summary>
    [JsonPropertyName("estimate")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool EstimateGas { get; set; }
    /// <summary>
    /// The message sender EVM formatted address.
    /// </summary>
    [JsonPropertyName("from")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EvmAddress? From { get; set; }
    /// <summary>
    /// The amount of gas allocated for the call.
    /// </summary>
    [JsonPropertyName("gas")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Gas { get; set; }
    /// <summary>
    /// The gas price set for the call.
    /// </summary>
    [JsonPropertyName("gasPrice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? GasPrice { get; set; }
    /// <summary>
    /// The address of the contract that is
    /// being called (or simulated hBar sent to)
    /// </summary>
    [JsonPropertyName("to")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EvmAddress? To { get; set; }
    /// <summary>
    /// The amount of hbar to simulate sending to
    /// the contract or remote address.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ulong? Value { get; set; } = null;
    /// <summary>
    /// Constructor, leaving all parameters as their
    /// original default values.
    /// </summary>
    public EvmCallData()
    {
    }
    /// <summary>
    /// Helper Constructor for contract calls, sets up
    /// the data property based upon the contract address,
    /// method name and parameters.
    /// </summary>
    /// <param name="contract">Contract address to call.</param>
    /// <param name="method">Contract method to invoke.</param>
    /// <param name="args">Optional additional method arguments</param>
    public EvmCallData(EvmAddress contract, string method, params object[] args)
    {
        To = contract;
        Data = Abi.EncodeFunctionWithArguments(method, args);
    }
}
