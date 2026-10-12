// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// The basic status of a block node: the span of blocks it can serve.
/// </summary>
/// <remarks>
/// Not every block node holds full history — check
/// <see cref="FirstAvailableBlockNumber"/> before requesting
/// historical blocks or subscribing from a historical starting point.
/// </remarks>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed record BlockNodeStatus
{
    /// <summary>
    /// The earliest block number this node can serve.
    /// </summary>
    public ulong FirstAvailableBlockNumber { get; }
    /// <summary>
    /// The latest block number this node can serve, or
    /// <see cref="ulong.MaxValue"/> when the node is newly installed or
    /// backfilling its history and its range is not contiguous — in
    /// which case <see cref="BlockNodeClient.GetServerStatusDetailAsync"/>
    /// reports the actual available ranges.
    /// </summary>
    public ulong LastAvailableBlockNumber { get; }
    /// <summary>
    /// True when the node retains only the latest state snapshot
    /// rather than historical state.
    /// </summary>
    public bool OnlyLatestState { get; }
    /// <summary>
    /// Internal constructor from the underlying protobuf.
    /// </summary>
    internal BlockNodeStatus(Org.Hiero.Block.Api.ServerStatusResponse protobuf)
    {
        FirstAvailableBlockNumber = protobuf.FirstAvailableBlock;
        LastAvailableBlockNumber = protobuf.LastAvailableBlock;
        OnlyLatestState = protobuf.OnlyLatestState;
    }
}
