using Hiero.Converters;
using Hiero.Mirror.Filters;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// HCS Topics information retrieved from a mirror node.
/// </summary>
public class HcsTopicData
{
    /// <summary>
    /// The ID of the topic
    /// </summary>
    [JsonPropertyName("topic_id")]
    public EntityId Topic { get; set; } = default!;
    /// <summary>
    /// The public administrator endorsments required for controlling this topic.
    /// </summary>
    [JsonPropertyName("admin_key")]
    public Endorsement Administrator { get; set; } = default!;
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
    /// The public submit endorsments required for 
    /// submitting messages to this topic
    /// </summary>
    [JsonPropertyName("submit_key")]
    public Endorsement Submit { get; set; } = default!;
    /// <summary>
    /// The consensus timestamp range this topic covers.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public TimestampRangeData TimestampRange { get; set; } = default!;
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class HcsTopicDataExtensions
{
    /// <summary>
    /// Retrieves the information regarding a HCS topic.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="topic">
    /// The ID of the topic to retrieve.
    /// </param>
    /// <param name="filters">
    /// Additional query filters if desired.
    /// </param>
    /// <returns>
    /// The information for the specified topic, or null if not found.
    /// </returns>
    public static Task<HcsTopicData?> GetHcsTopicAsync(this MirrorRestClient client, EntityId topic, params IMirrorQueryFilter[] filters)
    {
        var path = GenerateInitialPath($"topics/{MirrorFormat(topic)}", filters);
        return client.GetSingleItemAsync<HcsTopicData>(path);
    }
}