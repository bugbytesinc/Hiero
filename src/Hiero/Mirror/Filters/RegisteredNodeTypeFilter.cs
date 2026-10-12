// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;
namespace Hiero.Mirror.Filters;
/// <summary>
/// Predicate filter on the <c>type</c> query parameter of the
/// <c>/api/v1/network/registered-nodes</c> endpoint (HIP-1137),
/// narrowing results to a single registered-node kind. Construct via
/// one of the static factories.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed class RegisteredNodeTypeFilter : IMirrorFilter
{
    /// <summary>
    /// The query parameter name recognized by the remote mirror node.
    /// </summary>
    public string Name => "type";
    /// <summary>
    /// The value of the query parameter sent to the mirror node.
    /// </summary>
    public string Value { get; }

    private RegisteredNodeTypeFilter(string value) => Value = value;

    /// <summary>
    /// Registered nodes serving the Block Node APIs.
    /// </summary>
    public static RegisteredNodeTypeFilter BlockNode => new("BLOCK_NODE");
    /// <summary>
    /// Registered mirror nodes.
    /// </summary>
    public static RegisteredNodeTypeFilter MirrorNode => new("MIRROR_NODE");
    /// <summary>
    /// Registered JSON-RPC relay nodes.
    /// </summary>
    public static RegisteredNodeTypeFilter RpcRelay => new("RPC_RELAY");
    /// <summary>
    /// Registered general-service nodes.
    /// </summary>
    public static RegisteredNodeTypeFilter GeneralService => new("GENERAL_SERVICE");
}
