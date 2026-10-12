// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// One sibling hash along a merkle path from an item of interest
/// to the root of a block's merkle tree.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed record MerkleSiblingHash
{
    /// <summary>
    /// When true, this sibling is the first hash of the pair
    /// (the proven node's hash is second) when computing the
    /// parent hash.
    /// </summary>
    public bool IsFirst { get; }
    /// <summary>
    /// The sibling's hash value.
    /// </summary>
    public ReadOnlyMemory<byte> Hash { get; }
    /// <summary>
    /// Internal constructor from the underlying protobuf.
    /// </summary>
    internal MerkleSiblingHash(Com.Hedera.Hapi.Block.Stream.MerkleSiblingHash protobuf)
    {
        IsFirst = protobuf.IsFirst;
        Hash = protobuf.SiblingHash.Memory;
    }
}
