using Hiero.Converters;
using System;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;
/// <summary>
/// Represents the log results from an EVM contract call.
/// </summary>
public class ContractLogData
{
    /// <summary>
    /// Payer of the contract that generated the event,
    /// this is not necessarily the contract that externally called.
    /// </summary>
    [JsonPropertyName("address")]
    public EvmAddress ContractAddresss { get; set; } = default!;
    /// <summary>
    /// Bloom filter for record
    /// </summary>
    [JsonPropertyName("bloom")]
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> Bloom { get; set; }
    /// <summary>
    /// ID of the contract that was called.
    /// </summary>
    [JsonPropertyName("contract_id")]
    public EntityId Contract { get; set; } = default!;
    /// <summary>
    /// Non Indexed Input associated with the log Event
    /// </summary>
    [JsonPropertyName("data")]
    [JsonConverter(typeof(HexStringToBytesConverter))]
    public ReadOnlyMemory<byte> Data { get; set; }
    /// <summary>
    /// The Block SegmentIndex for this log record
    /// </summary>
    [JsonPropertyName("index")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long BlockIndex { get; set; }
    /// <summary>
    /// The indexed topic values returned from the contract call event
    /// </summary>
    [JsonPropertyName("topics")]
    [JsonConverter(typeof(HexStringArraytoBytesArrayConverter))]
    public ReadOnlyMemory<byte>[] Topics { get; set; } = default!;
}
