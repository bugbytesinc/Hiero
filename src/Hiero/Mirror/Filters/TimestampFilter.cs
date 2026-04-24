// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the consensus timestamp of a mirror node
/// record. Construct via one of the static factories — the ctor is
/// private so the operator is always explicit in the call site.
/// </summary>
/// <remarks>
/// Mirror REST accepts six comparison forms on the <c>timestamp</c>
/// query parameter: equality (default), <c>gt:</c>, <c>gte:</c>,
/// <c>lt:</c>, <c>lte:</c>, and <c>ne:</c>. Each factory builds the
/// corresponding wire value.
/// </remarks>
public sealed class TimestampFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "timestamp";
    /// <summary>
    /// The value of the query parameter sent to the mirror node —
    /// already includes the operator prefix where applicable.
    /// </summary>
    public string Value { get; }

    private TimestampFilter(string value) => Value = value;

    /// <summary>
    /// Records whose consensus timestamp equals the given instant.
    /// </summary>
    public static TimestampFilter Is(ConsensusTimeStamp timestamp) => new(timestamp.ToString()!);
    /// <summary>
    /// Records whose consensus timestamp is strictly after the given
    /// instant (<c>gt:</c>).
    /// </summary>
    public static TimestampFilter After(ConsensusTimeStamp timestamp) => new($"gt:{timestamp}");
    /// <summary>
    /// Records whose consensus timestamp is at or after the given
    /// instant (<c>gte:</c>).
    /// </summary>
    public static TimestampFilter OnOrAfter(ConsensusTimeStamp timestamp) => new($"gte:{timestamp}");
    /// <summary>
    /// Records whose consensus timestamp is strictly before the given
    /// instant (<c>lt:</c>).
    /// </summary>
    public static TimestampFilter Before(ConsensusTimeStamp timestamp) => new($"lt:{timestamp}");
    /// <summary>
    /// Records whose consensus timestamp is at or before the given
    /// instant (<c>lte:</c>).
    /// </summary>
    public static TimestampFilter OnOrBefore(ConsensusTimeStamp timestamp) => new($"lte:{timestamp}");
    /// <summary>
    /// Records whose consensus timestamp is not equal to the given
    /// instant (<c>ne:</c>).
    /// </summary>
    public static TimestampFilter NotIs(ConsensusTimeStamp timestamp) => new($"ne:{timestamp}");
}
