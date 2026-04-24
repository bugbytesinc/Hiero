// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>schedule.id</c> query parameter.
/// Construct via one of the static factories — the ctor is
/// private so the operator is always explicit at the call site.
/// </summary>
/// <remarks>
/// Mirror REST's <c>EntityIdQuery</c> schema accepts the six
/// comparison forms — equality (default), <c>gt:</c>,
/// <c>gte:</c>, <c>lt:</c>, <c>lte:</c>, and <c>ne:</c> — on
/// the <c>schedule.id</c> query parameter. Each factory builds
/// the corresponding wire value.
/// </remarks>
public sealed class ScheduleFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "schedule.id";
    /// <summary>
    /// The value of the query parameter sent to the mirror node —
    /// already includes the operator prefix where applicable.
    /// </summary>
    public string Value { get; }

    private ScheduleFilter(string value) => Value = value;

    /// <summary>
    /// Records whose <c>schedule.id</c> equals the given entity.
    /// </summary>
    /// <param name="schedule">The schedule entity to filter by.</param>
    public static ScheduleFilter Is(EntityId schedule) => new(schedule.ToString());
    /// <summary>
    /// Records whose <c>schedule.id</c> is strictly greater than
    /// the given entity (<c>gt:</c>).
    /// </summary>
    /// <param name="schedule">The schedule entity to filter by.</param>
    public static ScheduleFilter After(EntityId schedule) => new($"gt:{schedule}");
    /// <summary>
    /// Records whose <c>schedule.id</c> is at or greater than the
    /// given entity (<c>gte:</c>).
    /// </summary>
    /// <param name="schedule">The schedule entity to filter by.</param>
    public static ScheduleFilter OnOrAfter(EntityId schedule) => new($"gte:{schedule}");
    /// <summary>
    /// Records whose <c>schedule.id</c> is strictly less than the
    /// given entity (<c>lt:</c>).
    /// </summary>
    /// <param name="schedule">The schedule entity to filter by.</param>
    public static ScheduleFilter Before(EntityId schedule) => new($"lt:{schedule}");
    /// <summary>
    /// Records whose <c>schedule.id</c> is at or less than the
    /// given entity (<c>lte:</c>).
    /// </summary>
    /// <param name="schedule">The schedule entity to filter by.</param>
    public static ScheduleFilter OnOrBefore(EntityId schedule) => new($"lte:{schedule}");
    /// <summary>
    /// Records whose <c>schedule.id</c> is not equal to the given
    /// entity (<c>ne:</c>).
    /// </summary>
    /// <param name="schedule">The schedule entity to filter by.</param>
    public static ScheduleFilter NotIs(EntityId schedule) => new($"ne:{schedule}");
}
