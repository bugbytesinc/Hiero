// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// Topic information retrieved from a mirror node.
/// </summary>
public class TopicData
{
    /// <summary>
    /// The ID of the topic
    /// </summary>
    [JsonPropertyName("topic_id")]
    public EntityId Topic { get; set; } = default!;
    /// <summary>
    /// The public administrator endorsements required for controlling this
    /// topic, or <c>null</c> when the topic has no admin key and can no
    /// longer be updated or deleted.
    /// </summary>
    [JsonPropertyName("admin_key")]
    public Endorsement? Administrator { get; set; }
    /// <summary>
    /// The ID of the associated auto renew account
    /// </summary>
    [JsonPropertyName("auto_renew_account")]
    public EntityId? AutoRenewAccount { get; set; }
    /// <summary>
    /// Topics Auto-Renew Period in seconds.
    /// </summary>
    [JsonPropertyName("auto_renew_period")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long AutoRenewPeriod { get; set; }
    /// <summary>
    /// Consensus Timestamp when this topic was created
    /// </summary>
    [JsonPropertyName("created_timestamp")]
    public ConsensusTimeStamp Created { get; set; }
    /// <summary>
    /// Flag indicating that the topic has been deleted.
    /// </summary>
    [JsonPropertyName("deleted")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool Deleted { get; set; }
    /// <summary>
    /// The topic's memo.
    /// </summary>
    [JsonPropertyName("memo")]
    public string Memo { get; set; } = default!;
    /// <summary>
    /// The custom fees (HIP-991) charged for each message submitted to
    /// the topic. A topic carries fixed fees only, so the fractional and
    /// royalty fee lists are always empty.
    /// </summary>
    [JsonPropertyName("custom_fees")]
    public CustomFeeData? Royalties { get; set; }
    /// <summary>
    /// The endorsement required to change the topic's custom fees
    /// (HIP-991); null when the fees cannot be changed.
    /// </summary>
    [JsonPropertyName("fee_schedule_key")]
    public Endorsement? RoyaltiesEndorsement { get; set; }
    /// <summary>
    /// Endorsements whose signers may submit messages to the topic
    /// without paying its custom fees (HIP-991).
    /// </summary>
    [JsonPropertyName("fee_exempt_key_list")]
    public Endorsement[] RoyaltyExemptEndorsements { get; set; } = [];
    /// <summary>
    /// The public submit endorsements required for submitting messages to
    /// this topic, or <c>null</c> when anyone may submit.
    /// </summary>
    [JsonPropertyName("submit_key")]
    public Endorsement? Submit { get; set; }
    /// <summary>
    /// The consensus timestamp range this topic covers.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public TimestampRangeData TimestampRange { get; set; } = default!;
}
/// <summary>
/// Extension methods for querying topic data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TopicDataExtensions
{
    /// <summary>
    /// Retrieves the topic entity information from
    /// <c>/api/v1/topics/{id}</c>, including the admin and submit
    /// keys, memo, and lifecycle timestamps.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="topic">
    /// The ID of the topic to retrieve.
    /// </param>
    /// <param name="cancellationToken">
    /// Token that cancels the request.
    /// </param>
    /// <param name="filters">
    /// Reserved for forward compatibility. The endpoint currently
    /// accepts no query parameters per the OpenAPI spec; any filters
    /// supplied here are included in the URL but ignored by the server.
    /// </param>
    /// <returns>
    /// The information for the specified topic, or null if not found.
    /// </returns>
    public static Task<TopicData?> GetTopicAsync(this MirrorRestClient client, EntityId topic, CancellationToken cancellationToken, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"topics/{topic.ToMirrorString()}", filters);
        return client.GetSingleItemAsync(path, MirrorJsonContext.Default.TopicData, cancellationToken);
    }
    /// <inheritdoc cref="GetTopicAsync(MirrorRestClient, EntityId, CancellationToken, IMirrorQueryParameter[])"/>
    public static Task<TopicData?> GetTopicAsync(this MirrorRestClient client, EntityId topic, params IMirrorQueryParameter[] filters)
    {
        return client.GetTopicAsync(topic, CancellationToken.None, filters);
    }
}
