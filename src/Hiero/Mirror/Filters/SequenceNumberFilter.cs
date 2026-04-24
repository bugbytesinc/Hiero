// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the sequence number of an HCS topic message.
/// Construct via one of the static factories — the ctor is private
/// so the operator is always explicit in the call site.
/// </summary>
/// <remarks>
/// Mirror REST accepts the six comparison forms — equality
/// (default), <c>gt:</c>, <c>gte:</c>, <c>lt:</c>, <c>lte:</c>, and
/// <c>ne:</c> — on the <c>sequencenumber</c> query parameter. Each
/// factory builds the corresponding wire value.
/// </remarks>
public sealed class SequenceNumberFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "sequencenumber";
    /// <summary>
    /// The value of the query parameter sent to the mirror node —
    /// already includes the operator prefix where applicable.
    /// </summary>
    public string Value { get; }

    private SequenceNumberFilter(string value) => Value = value;

    /// <summary>
    /// Messages whose sequence number equals the given value.
    /// </summary>
    public static SequenceNumberFilter Is(ulong sequenceNumber) => new(sequenceNumber.ToString());
    /// <summary>
    /// Messages whose sequence number is strictly greater than the
    /// given value (<c>gt:</c>).
    /// </summary>
    public static SequenceNumberFilter After(ulong sequenceNumber) => new($"gt:{sequenceNumber}");
    /// <summary>
    /// Messages whose sequence number is at or greater than the
    /// given value (<c>gte:</c>).
    /// </summary>
    public static SequenceNumberFilter OnOrAfter(ulong sequenceNumber) => new($"gte:{sequenceNumber}");
    /// <summary>
    /// Messages whose sequence number is strictly less than the
    /// given value (<c>lt:</c>).
    /// </summary>
    public static SequenceNumberFilter Before(ulong sequenceNumber) => new($"lt:{sequenceNumber}");
    /// <summary>
    /// Messages whose sequence number is at or less than the given
    /// value (<c>lte:</c>).
    /// </summary>
    public static SequenceNumberFilter OnOrBefore(ulong sequenceNumber) => new($"lte:{sequenceNumber}");
    /// <summary>
    /// Messages whose sequence number is not equal to the given
    /// value (<c>ne:</c>).
    /// </summary>
    public static SequenceNumberFilter NotIs(ulong sequenceNumber) => new($"ne:{sequenceNumber}");
}
