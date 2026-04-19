// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8618
using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;

/// <summary>
/// Chunk metadata for a segmented HCS submit message, when the payload
/// was split across multiple chunk transactions.
/// </summary>
public class ChunkData
{
    /// <summary>
    /// Corresponding initial transaction id.
    /// </summary>
    [JsonPropertyName("initial_transaction_id")]
    [JsonConverter(typeof(TransactionIdStructuredConverter))]
    public TransactionId InitialTransactionId { get; set; }
    /// <summary>
    /// Chunk number.
    /// </summary>
    [JsonPropertyName("number")]
    public int Number { get; set; }
    /// <summary>
    /// Total number of chunks.
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }
}
