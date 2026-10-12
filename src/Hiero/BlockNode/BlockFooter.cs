// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// The footer of a block stream block, carrying the merkle roots
/// that chain blocks together.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed record BlockFooter
{
    /// <summary>
    /// The root hash of the previous block; chaining these hashes
    /// links every block back to genesis.
    /// </summary>
    public ReadOnlyMemory<byte> PreviousBlockRootHash { get; }
    /// <summary>
    /// The root hash of the merkle tree of all previous block hashes.
    /// </summary>
    public ReadOnlyMemory<byte> RootHashOfAllBlockHashesTree { get; }
    /// <summary>
    /// The network state root hash as of the start of this block
    /// (the post-block state root lands in the next block's footer).
    /// </summary>
    public ReadOnlyMemory<byte> StartOfBlockStateRootHash { get; }
    /// <summary>
    /// Internal constructor from the underlying protobuf.
    /// </summary>
    internal BlockFooter(Com.Hedera.Hapi.Block.Stream.Output.BlockFooter protobuf)
    {
        PreviousBlockRootHash = protobuf.PreviousBlockRootHash.Memory;
        RootHashOfAllBlockHashesTree = protobuf.RootHashOfAllBlockHashesTree.Memory;
        StartOfBlockStateRootHash = protobuf.StartOfBlockStateRootHash.Memory;
    }
}
