// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>block.number</c> query parameter of the
/// <c>/api/v1/blocks</c> and contract-results endpoints. Construct via
/// one of the static factories — the ctor is private so the operator
/// is always explicit at the call site.
/// </summary>
/// <remarks>
/// Mirror REST accepts the six comparison forms — equality (default),
/// <c>gt:</c>, <c>gte:</c>, <c>lt:</c>, <c>lte:</c>, and <c>ne:</c> —
/// on the <c>block.number</c> query parameter. Each factory builds the
/// corresponding wire value. Block numbers are non-negative on the
/// wire (OpenAPI <c>minimum: 0</c>), hence the <c>ulong</c> argument.
/// </remarks>
public sealed class BlockNumberFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "block.number";
    /// <summary>
    /// The value of the query parameter sent to the mirror node —
    /// already includes the operator prefix where applicable.
    /// </summary>
    public string Value { get; }

    private BlockNumberFilter(string value) => Value = value;

    /// <summary>
    /// Records whose block number equals the given value.
    /// </summary>
    public static BlockNumberFilter Is(ulong blockNumber) => new(blockNumber.ToString());
    /// <summary>
    /// Records whose block number is strictly greater than the
    /// given value (<c>gt:</c>).
    /// </summary>
    public static BlockNumberFilter After(ulong blockNumber) => new($"gt:{blockNumber}");
    /// <summary>
    /// Records whose block number is at or greater than the
    /// given value (<c>gte:</c>).
    /// </summary>
    public static BlockNumberFilter OnOrAfter(ulong blockNumber) => new($"gte:{blockNumber}");
    /// <summary>
    /// Records whose block number is strictly less than the
    /// given value (<c>lt:</c>).
    /// </summary>
    public static BlockNumberFilter Before(ulong blockNumber) => new($"lt:{blockNumber}");
    /// <summary>
    /// Records whose block number is at or less than the given
    /// value (<c>lte:</c>).
    /// </summary>
    public static BlockNumberFilter OnOrBefore(ulong blockNumber) => new($"lte:{blockNumber}");
    /// <summary>
    /// Records whose block number is not equal to the given
    /// value (<c>ne:</c>).
    /// </summary>
    public static BlockNumberFilter NotIs(ulong blockNumber) => new($"ne:{blockNumber}");
}
