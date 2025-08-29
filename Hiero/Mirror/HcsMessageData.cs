using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// Represents an HCS Message retrieved from the mirror node.
/// </summary>
public class HcsMessageData
{
    /// <summary>
    /// HCS Message Chunk Information.
    /// </summary>
    [JsonPropertyName("chunk_info")]
    public ChunkInfo? ChunkInfo { get; set; }
    /// <summary>
    /// HCS Message Consensus Timestamp.
    /// </summary>
    [JsonPropertyName("consensus_timestamp")]
    public ConsensusTimeStamp TimeStamp { get; set; }
    /// <summary>
    /// Message Payload.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = default!;
    /// <summary>
    /// The payer account submitting the message.
    /// </summary>
    [JsonPropertyName("payer_account_id")]
    public EntityId Payer { get; set; } = default!;
    /// <summary>
    /// The running hash of the message (for validation purposes)
    /// </summary>
    [JsonPropertyName("running_hash")]
    public string Hash { get; set; } = default!;
    /// <summary>
    /// The version of the running hash (for validation purposes).
    /// </summary>
    [JsonPropertyName("running_hash_version")]
    [JsonConverter(typeof(IntMirrorConverter))]
    public int HashVersion { get; set; }
    /// <summary>
    /// Sequence number of this HCS message.
    /// </summary>
    [JsonPropertyName("sequence_number")]
    [JsonConverter(typeof(UnsignedLongMirrorConverter))]
    public ulong SequenceNumber { get; set; }
    /// <summary>
    /// The HCS message stream topic ID for this message.
    /// </summary>
    [JsonPropertyName("topic_id")]
    public EntityId TopicId { get; set; } = default!;
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class HcsMessageDataExtensions
{
    /// <summary>
    /// Retrieves an HCS message with the given token and sequence number.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="topic">
    /// The HCS message topic to retrieve.
    /// </param>
    /// <param name="sequenceNumber">
    /// The sequence number of message within the token stream to retrieve.
    /// </param>
    /// <returns>
    /// The HCS Message information or null if not found.
    /// </returns>    
    public static Task<HcsMessageData?> GetHcsMessageAsync(this MirrorRestClient client, EntityId topic, ulong sequenceNumber)
    {
        return client.GetSingleItemAsync<HcsMessageData>($"topics/{topic}/messages/{sequenceNumber}");
    }
    /// <summary>
    /// Retrieves a list of HCS message.  Messages may be filtered by a starting 
    /// sequence number or consensus timestamp.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="topic">
    /// The topic id of the HCS stream.
    /// </param>
    /// <param name="filters">
    /// Additional query filters if desired.
    /// </param>
    /// <returns>
    /// An enumerable of HCS Messages meeting the given criteria, may be empty if 
    /// none are found.
    /// </returns>
    public static IAsyncEnumerable<HcsMessageData> GetHcsMessagesAsync(this MirrorRestClient client, EntityId topic, params IMirrorQueryFilter[] filters)
    {
        var path = GenerateInitialPath($"topics/{topic}/messages", [new LimitFilter(100), .. filters]);
        return client.GetPagedItemsAsync<HcsMessageDataPage, HcsMessageData>(path);
    }
}