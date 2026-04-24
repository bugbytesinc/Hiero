// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>node.id</c> query parameter of the
/// <c>/api/v1/network/nodes</c> endpoint. Construct via one of the
/// static factories — the ctor is private so the operator is always
/// explicit at the call site.
/// </summary>
/// <remarks>
/// <para>
/// The <c>node.id</c> value is the node's 0-based index (see
/// <see cref="ConsensusNodeData.NodeId"/>), <b>not</b> the
/// <c>node_account_id</c> (e.g., <c>0.0.3</c>).
/// </para>
/// <para>
/// Unlike the other comparison-operator filters in this namespace,
/// <c>node.id</c> accepts only five operators on the wire —
/// equality (default), <c>gt:</c>, <c>gte:</c>, <c>lt:</c>, and
/// <c>lte:</c>. The mirror-node schema explicitly excludes <c>ne:</c>,
/// so no <c>NotIs</c> factory is exposed.
/// </para>
/// </remarks>
public sealed class NodeFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "node.id";
    /// <summary>
    /// The value of the query parameter sent to the mirror node —
    /// already includes the operator prefix where applicable.
    /// </summary>
    public string Value { get; }

    private NodeFilter(string value) => Value = value;

    /// <summary>
    /// Records whose node id equals the given value.
    /// </summary>
    public static NodeFilter Is(ulong nodeId) => new(nodeId.ToString());
    /// <summary>
    /// Records whose node id is strictly greater than the given
    /// value (<c>gt:</c>).
    /// </summary>
    public static NodeFilter After(ulong nodeId) => new($"gt:{nodeId}");
    /// <summary>
    /// Records whose node id is at or greater than the given value
    /// (<c>gte:</c>).
    /// </summary>
    public static NodeFilter OnOrAfter(ulong nodeId) => new($"gte:{nodeId}");
    /// <summary>
    /// Records whose node id is strictly less than the given value
    /// (<c>lt:</c>).
    /// </summary>
    public static NodeFilter Before(ulong nodeId) => new($"lt:{nodeId}");
    /// <summary>
    /// Records whose node id is at or less than the given value
    /// (<c>lte:</c>).
    /// </summary>
    public static NodeFilter OnOrBefore(ulong nodeId) => new($"lte:{nodeId}");
}
