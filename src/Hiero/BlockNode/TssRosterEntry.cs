// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// One consensus node's entry in the TSS signing roster.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed record TssRosterEntry
{
    /// <summary>
    /// The consensus node's identifier.
    /// </summary>
    public ulong NodeId { get; }
    /// <summary>
    /// The node's consensus weight (stake).
    /// </summary>
    public ulong Weight { get; }
    /// <summary>
    /// The node's Schnorr public key used to chain roster changes.
    /// </summary>
    public ReadOnlyMemory<byte> SchnorrPublicKey { get; }
    /// <summary>
    /// Internal constructor from the underlying protobuf.
    /// </summary>
    internal TssRosterEntry(Org.Hiero.Block.Api.RosterEntry protobuf)
    {
        NodeId = protobuf.NodeId;
        Weight = protobuf.Weight;
        SchnorrPublicKey = protobuf.SchnorrPublicKey.Memory;
    }
}
