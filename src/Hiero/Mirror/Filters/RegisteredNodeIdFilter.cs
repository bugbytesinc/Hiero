// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>registerednode.id</c> query parameter of
/// the <c>/api/v1/network/registered-nodes</c> endpoint (HIP-1137).
/// Construct via one of the static factories — the ctor is private so
/// the operator is always explicit at the call site.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed class RegisteredNodeIdFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "registerednode.id";
    /// <summary>
    /// The value of the query parameter sent to the mirror node —
    /// already includes the operator prefix where applicable.
    /// </summary>
    public string Value { get; }

    private RegisteredNodeIdFilter(string value) => Value = value;

    /// <summary>
    /// Records whose registered node id equals the given value.
    /// </summary>
    public static RegisteredNodeIdFilter Is(ulong registeredNodeId) => new(registeredNodeId.ToString());
    /// <summary>
    /// Records whose registered node id is strictly greater than the
    /// given value (<c>gt:</c>).
    /// </summary>
    public static RegisteredNodeIdFilter After(ulong registeredNodeId) => new($"gt:{registeredNodeId}");
    /// <summary>
    /// Records whose registered node id is at or greater than the given
    /// value (<c>gte:</c>).
    /// </summary>
    public static RegisteredNodeIdFilter OnOrAfter(ulong registeredNodeId) => new($"gte:{registeredNodeId}");
    /// <summary>
    /// Records whose registered node id is strictly less than the given
    /// value (<c>lt:</c>).
    /// </summary>
    public static RegisteredNodeIdFilter Before(ulong registeredNodeId) => new($"lt:{registeredNodeId}");
    /// <summary>
    /// Records whose registered node id is at or less than the given
    /// value (<c>lte:</c>).
    /// </summary>
    public static RegisteredNodeIdFilter OnOrBefore(ulong registeredNodeId) => new($"lte:{registeredNodeId}");
}
