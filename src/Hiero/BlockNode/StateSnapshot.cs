// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// A reference to a network state snapshot held by a block node.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed record StateSnapshot
{
    /// <summary>
    /// The number of the last block reflected in the snapshot.
    /// </summary>
    public ulong LastBlockNumber { get; }
    /// <summary>
    /// An out-of-band reference (a URL or node-defined locator
    /// string) from which the snapshot content can be retrieved;
    /// the snapshot bytes themselves are not delivered over gRPC.
    /// </summary>
    public string SnapshotReference { get; }
    /// <summary>
    /// Internal constructor from the underlying protobuf.
    /// </summary>
    internal StateSnapshot(Org.Hiero.Block.Api.StateSnapshotResponse protobuf)
    {
        LastBlockNumber = protobuf.LastBlockNumber;
        SnapshotReference = protobuf.SnapshotReference;
    }
}
