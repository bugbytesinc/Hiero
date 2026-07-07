// SPDX-License-Identifier: Apache-2.0
namespace Hiero.Mirror;
/// <summary>
/// Selects which transport(s) a consensus-node discovery query should return
/// endpoints for.
/// </summary>
/// <remarks>
/// Consensus nodes typically expose both a plaintext gRPC endpoint (port 50211)
/// and a TLS gRPC endpoint (port 50212).  TLS endpoints present self-signed
/// certificates that require hash pinning against the address book's published
/// certificate hash; discovery attaches that hash to the returned endpoints so
/// the client's default channel factory can validate them.
/// </remarks>
public enum ConsensusNodeTransport
{
    /// <summary>
    /// Return only plaintext (unencrypted, port 50211) endpoints.  This is the
    /// default and preserves the historical behavior of node discovery.  No
    /// certificate hash is attached because none is needed.
    /// </summary>
    Plaintext = 0,
    /// <summary>
    /// Return only TLS (encrypted, port 50212) endpoints, each carrying the
    /// node's expected certificate hash for pinning.  Nodes whose address-book
    /// entry has no usable certificate hash are excluded, since a TLS endpoint
    /// cannot be validated without one.
    /// </summary>
    Tls = 1,
    /// <summary>
    /// Return both plaintext and TLS endpoints for every responsive node.  A node
    /// reachable on both ports appears as two separate endpoints, leaving the
    /// caller to choose.  This is the slowest option because it probes both ports
    /// per node.
    /// </summary>
    All = 2,
}
