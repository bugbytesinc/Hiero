// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Hiero;
/// <summary>
/// Represents a consensus node's network address, consisting of a node
/// identifier and a gRPC endpoint URI for accessing the Hiero Network.
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
    /// The URL and port of the public Hiero Network
    /// Consensus Node access point.
    /// </summary>
    public Uri Uri { get; private init; }
    /// <summary>
    /// The Network gRPC Consensus Node's Address, may
    /// only be [shard.realm.num] form.
    /// </summary>
    public EntityId Node { get; private init; }
    /// <summary>
    /// The expected SHA-384 hash of the node's TLS certificate, as published by
    /// the network's address book (the mirror node's <c>node_cert_hash</c> field
    /// or the classic address book's <c>NodeAddress.cert_hash</c>).
    /// </summary>
    /// <remarks>
    /// Hiero consensus nodes present self-signed certificates whose subject
    /// alternative name does not match their real address, so standard TLS chain
    /// and hostname validation can never succeed against a TLS (port 50212)
    /// endpoint.  When this hash is supplied and the <see cref="Uri"/> scheme is
    /// <c>https</c>, the client's default channel factory pins the connection to
    /// this hash instead of using chain/hostname validation.  When empty, the
    /// default factory falls back to standard TLS validation (appropriate for
    /// proxy-fronted DNS endpoints that present a chain-valid certificate).
    /// This value participates in equality (by content): two endpoints that pin
    /// to different certificate hashes — or one that pins and one that does not —
    /// are distinct endpoints and receive distinct, independently validated gRPC
    /// channels.
    /// </remarks>
    public ReadOnlyMemory<byte> CertificateHash { get; private init; }
    /// <summary>
    /// Public Constructor, a <code>ConsensusNodeEndpoint</code> is immutable after creation.
    /// </summary>
    /// <param name="node">
    /// Main Network Consensus Node's Address.
    /// </param>
    /// <param name="uri">
    /// The URL and port of the public Hiero Network Consensus Node's gRPC access point.
    /// A consensus node may actually have multiple gRPC endpoints mapped to the same
    /// wallet address (shard.realm.num).
    /// </param>
    public ConsensusNodeEndpoint(EntityId node, Uri uri) : this(node, uri, default)
    {
    }
    /// <summary>
    /// Public Constructor accepting the node's expected TLS certificate hash, a
    /// <code>ConsensusNodeEndpoint</code> is immutable after creation.
    /// </summary>
    /// <param name="node">
    /// Main Network Consensus Node's Address.
    /// </param>
    /// <param name="uri">
    /// The URL and port of the public Hiero Network Consensus Node's gRPC access point.
    /// A consensus node may actually have multiple gRPC endpoints mapped to the same
    /// wallet address (shard.realm.num).
    /// </param>
    /// <param name="certificateHash">
    /// The expected SHA-384 hash of the node's TLS certificate (see
    /// <see cref="CertificateHash"/>).  Supply this for <c>https</c> endpoints
    /// dialed directly by node address so the default channel factory can pin
    /// the TLS connection; leave empty for endpoints presenting a chain-valid
    /// certificate.
    /// </param>
    public ConsensusNodeEndpoint(EntityId node, Uri uri, ReadOnlyMemory<byte> certificateHash)
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
        CertificateHash = certificateHash;
    }
    /// <summary>
    /// Determines equality with another <see cref="ConsensusNodeEndpoint"/>.
    /// Two endpoints are equal when they share the same <see cref="Node"/>,
    /// <see cref="Uri"/>, and <see cref="CertificateHash"/> content.  The hash is
    /// compared by value (not by backing-array reference) so endpoints built from
    /// equal hash bytes are equal, and so distinct pinning targets remain distinct
    /// channel cache keys.
    /// </summary>
    /// <param name="other">The endpoint to compare against.</param>
    /// <returns>True when the node address, URI, and certificate hash all match.</returns>
    public bool Equals(ConsensusNodeEndpoint? other)
    {
        return other is not null
            && Node == other.Node
            && Uri == other.Uri
            && CertificateHash.Span.SequenceEqual(other.CertificateHash.Span);
    }
    /// <summary>
    /// Returns a hash code consistent with <see cref="Equals(ConsensusNodeEndpoint)"/>,
    /// derived from <see cref="Node"/>, <see cref="Uri"/>, and the
    /// <see cref="CertificateHash"/> content.
    /// </summary>
    /// <returns>A hash code for this endpoint.</returns>
    public override int GetHashCode()
    {
        var code = new HashCode();
        code.Add(Node);
        code.Add(Uri);
        code.AddBytes(CertificateHash.Span);
        return code.ToHashCode();
    }
    /// <summary>
    /// Implicit operator for converting a ConsensusNodeEndpoint to an EntityId.
    /// </summary>
    /// <param name="endpoint">
    /// The ConsensusNodeEndpoint object containing the shard, realm and node
    /// number information to convert into an address object.
    /// </param>
    public static implicit operator EntityId(ConsensusNodeEndpoint endpoint)
    {
        return endpoint.Node;
    }
    /// <summary>
    /// Returns a string representation of the Consensus Node ConsensusNodeEndpoint.
    /// </summary>
    /// <returns>String Representation of the Consensus Node ConsensusNodeEndpoint</returns>
    public override string ToString()
    {
        return $"{Node}@{Uri}";
    }
}