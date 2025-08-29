using Hiero.Converters;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Hiero;
/// <summary>
/// Class representing the Network Payer and gRPC ConsensusNodeEndpoint
/// address for gaining access to the Hedera Network.
/// </summary>
/// <remarks>
/// This class consists of both an <see cref="EntityId"/> representing 
/// the main node within the network and the gRPC URL for the
/// network node where the public network endpoint is located.
/// This class is immutable once created.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
[JsonConverter(typeof(ConsensusNodeEndpointConverter))]
public sealed record ConsensusNodeEndpoint
{
    /// <summary>
    /// The URL and port of the public Hedera Network 
    /// Consensus Node access point.
    /// </summary>
    public Uri Uri { get; private init; }
    /// <summary>
    /// The Network gRPC Consensus Node's Address Address, may
    /// only be [shard.realm.num] form.
    /// </summary>
    public EntityId Node { get; private init; }
    /// <summary>
    /// Public Constructor, a <code>ConsensusEndpoint</code> is immutable after creation.
    /// </summary>
    /// <param name="node">
    /// Main Network Consensus Nodes's Address Address.
    /// </param>
    /// <param name="uri">
    /// The URL and port of the public Hedera Network Consensus Node's gRPC access point.
    /// A consensus node may actually have multiple gRPC endpoints mapped to the same
    /// wallet addresss (shard.realm.num).
    /// </param>
    public ConsensusNodeEndpoint(EntityId node, Uri uri)
    {
        if (uri is null)
        {
            throw new ArgumentNullException(nameof(uri), "URL is required.");
        }
        if (node is null)
        {
            throw new ArgumentNullException(nameof(node), "Node wallet address is required.");
        }
        if (node == EntityId.None)
        {
            throw new ArgumentException("Node wallet address can not be None.", nameof(node));
        }
        if (!node.IsShardRealmNum)
        {
            throw new ArgumentOutOfRangeException(nameof(node), "Node wallet address must be in the form of [shard.realm.num].");
        }
        Uri = uri;
        Node = node;
    }
    /// <summary>
    /// Implicit operator for converting a Consensus ConsensusNodeEndpoint to an Payer Entity ID
    /// </summary>
    /// <param name="endpoint">
    /// The ConsensusNodeEndpoint object containing the realm, shard and node 
    /// number node information to convert into an node object.
    /// </param>
    public static implicit operator EntityId(ConsensusNodeEndpoint endpoint)
    {
        return endpoint.Node;
    }
    /// <summary>
    /// Returns a string representation of the Consensus Node ConsensusNodeEndpoint,
    /// </summary>
    /// <returns>String Represntation of the Consensus Node ConsensusNodeEndpoint</returns>
    public override string ToString()
    {
        return $"{Node}@{Uri}";
    }
}