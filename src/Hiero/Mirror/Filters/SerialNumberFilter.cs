// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>serialnumber</c> query parameter
/// used by the NFT-listing endpoints. Construct via one of the
/// static factories — the ctor is private so the operator is
/// always explicit at the call site.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <see cref="SequenceNumberFilter"/>, the mirror-node
/// schema for <c>serialnumber</c> accepts only five operators on
/// the wire — equality (default), <c>gt:</c>, <c>gte:</c>,
/// <c>lt:</c>, and <c>lte:</c>. The OpenAPI pattern
/// <c>^((eq|gt|gte|lt|lte):)?\d{1,19}?$</c> explicitly excludes
/// <c>ne:</c>, so no <c>NotIs</c> factory is exposed.
/// </para>
/// <para>
/// Some endpoints also require the filter be paired with a
/// <see cref="TokenFilter"/> on the same request — see the
/// per-endpoint documentation for constraints.
/// </para>
/// </remarks>
public sealed class SerialNumberFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "serialnumber";
    /// <summary>
    /// The value of the query parameter sent to the mirror node —
    /// already includes the operator prefix where applicable.
    /// </summary>
    public string Value { get; }

    private SerialNumberFilter(string value) => Value = value;

    /// <summary>
    /// Records whose serial number equals the given value.
    /// </summary>
    public static SerialNumberFilter Is(long serialNumber) => new(serialNumber.ToString());
    /// <summary>
    /// Records whose serial number is strictly greater than the
    /// given value (<c>gt:</c>).
    /// </summary>
    public static SerialNumberFilter After(long serialNumber) => new($"gt:{serialNumber}");
    /// <summary>
    /// Records whose serial number is at or greater than the given
    /// value (<c>gte:</c>).
    /// </summary>
    public static SerialNumberFilter OnOrAfter(long serialNumber) => new($"gte:{serialNumber}");
    /// <summary>
    /// Records whose serial number is strictly less than the given
    /// value (<c>lt:</c>).
    /// </summary>
    public static SerialNumberFilter Before(long serialNumber) => new($"lt:{serialNumber}");
    /// <summary>
    /// Records whose serial number is at or less than the given
    /// value (<c>lte:</c>).
    /// </summary>
    public static SerialNumberFilter OnOrBefore(long serialNumber) => new($"lte:{serialNumber}");
}
