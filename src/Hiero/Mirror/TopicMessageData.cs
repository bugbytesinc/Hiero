// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// Represents a topic message retrieved from the mirror node.
/// </summary>
public class TopicMessageData
{
    /// <summary>
    /// Chunk metadata for this message, when part of a segmented submit.
    /// </summary>
    [JsonPropertyName("chunk_info")]
    public ChunkData? Chunk { get; set; }
    /// <summary>
    /// Topic message consensus timestamp.
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
    /// Sequence number of this topic message.
    /// </summary>
    [JsonPropertyName("sequence_number")]
    [JsonConverter(typeof(UnsignedLongMirrorConverter))]
    public ulong SequenceNumber { get; set; }
    /// <summary>
    /// The topic ID for this message.
    /// </summary>
    [JsonPropertyName("topic_id")]
    public EntityId TopicId { get; set; } = default!;
}
/// <summary>
/// Extension methods for querying topic message data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TopicMessageDataExtensions
{
    /// <summary>
    /// Retrieves a topic message with the given topic and sequence number.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="topic">
    /// The topic to retrieve the message from.
    /// </param>
    /// <param name="sequenceNumber">
    /// The sequence number of the message within the topic stream to retrieve.
    /// </param>
    /// <returns>
    /// The topic message information or null if not found.
    /// </returns>
    public static Task<TopicMessageData?> GetTopicMessageAsync(this MirrorRestClient client, EntityId topic, ulong sequenceNumber)
    {
        return client.GetSingleItemAsync<TopicMessageData>($"topics/{topic}/messages/{sequenceNumber}", MirrorJsonContext.Default.TopicMessageData);
    }
    /// <summary>
    /// Retrieves a list of topic messages.  Messages may be filtered by a starting
    /// sequence number or consensus timestamp.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="topic">
    /// The topic id of the message stream.
    /// </param>
    /// <param name="filters">
    /// Additional query filters if desired.
    /// </param>
    /// <returns>
    /// An enumerable of topic messages meeting the given criteria, may be empty if
    /// none are found.
    /// </returns>
    public static IAsyncEnumerable<TopicMessageData> GetTopicMessagesAsync(this MirrorRestClient client, EntityId topic, params IMirrorQueryFilter[] filters)
    {
        var path = GenerateInitialPath($"topics/{topic}/messages", [new LimitFilter(100), .. filters]);
        return client.GetPagedItemsAsync<TopicMessageDataPage, TopicMessageData>(path, MirrorJsonContext.Default.TopicMessageDataPage);
    }
}