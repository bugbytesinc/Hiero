// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// A proof that a specific item is contained within a specific
/// block: the item itself, the merkle sibling hashes forming the
/// path to the block's root, and the block's aggregate signature.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed record BlockContentsProof
{
    /// <summary>
    /// The block item the proof covers.
    /// </summary>
    public BlockItem Item { get; }
    /// <summary>
    /// The sibling hashes forming the merkle path from the item
    /// to the block root, in leaf-to-root order.
    /// </summary>
    public IReadOnlyList<MerkleSiblingHash> SiblingHashes { get; }
    /// <summary>
    /// The block's aggregate signature over its root hash.
    /// </summary>
    public ReadOnlyMemory<byte> BlockSignature { get; }
    /// <summary>
    /// Internal constructor from the underlying protobuf.
    /// </summary>
    internal BlockContentsProof(Org.Hiero.Block.Api.BlockContentsProof protobuf)
    {
        Item = new BlockItem(protobuf.BlockItem);
        var hashes = new MerkleSiblingHash[protobuf.SiblingHashes.Count];
        for (var i = 0; i < hashes.Length; i++)
        {
            hashes[i] = new MerkleSiblingHash(protobuf.SiblingHashes[i]);
        }
        SiblingHashes = hashes;
        BlockSignature = protobuf.BlockSignature.Memory;
    }
}
