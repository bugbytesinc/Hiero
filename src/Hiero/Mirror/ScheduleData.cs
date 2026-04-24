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
/// Information about a scheduled transaction as reported by
/// the mirror node — the REST-side view of a schedule entity.
/// </summary>
/// <remarks>
/// Distinct from <see cref="ScheduleInfo"/>, which is the
/// consensus-side (gRPC) type returned by
/// <c>ConsensusClient.GetScheduleInfoAsync</c>. Field names
/// align where the concepts match; mirror-only fields include
/// <see cref="Created"/>, <see cref="Signatures"/>, and the
/// nullable <see cref="Deleted"/> / <see cref="Executed"/>
/// timestamps.
/// </remarks>
public class ScheduleData
{
    /// <summary>
    /// The key that can delete this schedule before execution.
    /// Null when the schedule has no admin key (cannot be
    /// canceled; only expiration or signature-completion can
    /// resolve it).
    /// </summary>
    [JsonPropertyName("admin_key")]
    public Endorsement? Administrator { get; set; }
    /// <summary>
    /// The consensus timestamp at which the schedule entity was
    /// created.
    /// </summary>
    [JsonPropertyName("consensus_timestamp")]
    public ConsensusTimeStamp Created { get; set; }
    /// <summary>
    /// The account that created the schedule entity.
    /// </summary>
    [JsonPropertyName("creator_account_id")]
    public EntityId Creator { get; set; } = default!;
    /// <summary>
    /// Flag indicating that the schedule has been deleted by
    /// its administrator before execution.
    /// </summary>
    [JsonPropertyName("deleted")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool Deleted { get; set; }
    /// <summary>
    /// The consensus timestamp at which the scheduled
    /// transaction executed. Null when the schedule has not
    /// executed (still awaiting signatures, awaiting its
    /// expiration-deferred execution, or already expired).
    /// </summary>
    [JsonPropertyName("executed_timestamp")]
    public ConsensusTimeStamp? Executed { get; set; }
    /// <summary>
    /// The consensus time after which the schedule is removed
    /// from the network if still unexecuted. Null when the
    /// network has not set an explicit expiration.
    /// </summary>
    [JsonPropertyName("expiration_time")]
    public ConsensusTimeStamp? Expiration { get; set; }
    /// <summary>
    /// Optional memo attached to the schedule at creation.
    /// </summary>
    [JsonPropertyName("memo")]
    public string? Memo { get; set; }
    /// <summary>
    /// The account paying for the execution of the scheduled
    /// transaction.
    /// </summary>
    [JsonPropertyName("payer_account_id")]
    public EntityId Payer { get; set; } = default!;
    /// <summary>
    /// The schedule entity's id.
    /// </summary>
    [JsonPropertyName("schedule_id")]
    public EntityId Schedule { get; set; } = default!;
    /// <summary>
    /// Signatures that have been recorded against the schedule
    /// so far. Null when the mirror node elides the field.
    /// </summary>
    [JsonPropertyName("signatures")]
    public ScheduleSignatureData[]? Signatures { get; set; }
    /// <summary>
    /// Body bytes of the scheduled transaction, serialized into
    /// the protobuf <c>SchedulableTransactionBody</c> message.
    /// </summary>
    [JsonPropertyName("transaction_body")]
    [JsonConverter(typeof(Base64StringToBytesConverter))]
    public ReadOnlyMemory<byte> TransactionBody { get; set; }
    /// <summary>
    /// When <c>true</c>, the network delays execution until the
    /// expiration time even if enough signatures are present
    /// earlier.
    /// </summary>
    [JsonPropertyName("wait_for_expiry")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool DelayExecution { get; set; }
}
/// <summary>
/// Extension methods for querying schedule data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ScheduleDataExtensions
{
    /// <summary>
    /// Enumerates schedule entities across the network. Use
    /// <see cref="AccountFilter"/> to narrow to a specific
    /// creator or payer, <see cref="ScheduleFilter"/> for a
    /// specific schedule id (or range), or
    /// <see cref="PageLimit"/> / <see cref="OrderBy"/> for
    /// paging.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="filters">
    /// Additional query filters. The endpoint supports
    /// <see cref="AccountFilter"/>, <see cref="ScheduleFilter"/>,
    /// <see cref="PageLimit"/>, and <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of schedule records.
    /// </returns>
    public static IAsyncEnumerable<ScheduleData> GetSchedulesAsync(this MirrorRestClient client, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath("schedules", [new PageLimit(100), .. filters]);
        return client.GetPagedItemsAsync<ScheduleDataPage, ScheduleData>(path, MirrorJsonContext.Default.ScheduleDataPage);
    }
    /// <summary>
    /// Retrieves a single schedule entity by id from
    /// <c>/api/v1/schedules/{id}</c>.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="schedule">
    /// The schedule entity id to look up.
    /// </param>
    /// <returns>
    /// The schedule record, or null if not found.
    /// </returns>
    public static Task<ScheduleData?> GetScheduleAsync(this MirrorRestClient client, EntityId schedule)
    {
        return client.GetSingleItemAsync($"schedules/{MirrorFormat(schedule)}", MirrorJsonContext.Default.ScheduleData);
    }
}
