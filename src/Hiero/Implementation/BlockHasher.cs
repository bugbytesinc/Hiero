// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using System.Security.Cryptography;
using ProtoBlockStream = Com.Hedera.Hapi.Block.Stream;

namespace Hiero.Implementation;

/// <summary>
/// The SHA-384 hash algebra of the block stream's merkle trees,
/// mirroring the block node reference implementation
/// (org.hiero.block.common.hasher.HashingUtilities): domain-separated
/// node hashing with a leaf prefix of 0x00 and a two-children prefix
/// of 0x02.
/// </summary>
internal static class BlockHashing
{
    public const int HashSize = 48;
    private const byte LeafPrefix = 0x00;
    private const byte TwoChildrenPrefix = 0x02;
    /// <summary>
    /// The hash of an empty subtree: SHA-384 of a single zero byte.
    /// </summary>
    public static readonly byte[] EmptyTreeHash = SHA384.HashData([0x00]);

    public static byte[] HashLeaf(ReadOnlySpan<byte> leafData)
    {
        var buffer = new byte[leafData.Length + 1];
        buffer[0] = LeafPrefix;
        leafData.CopyTo(buffer.AsSpan(1));
        return SHA384.HashData(buffer);
    }

    public static byte[] HashInternalNode(ReadOnlySpan<byte> leftHash, ReadOnlySpan<byte> rightHash)
    {
        var buffer = new byte[1 + leftHash.Length + rightHash.Length];
        buffer[0] = TwoChildrenPrefix;
        leftHash.CopyTo(buffer.AsSpan(1));
        rightHash.CopyTo(buffer.AsSpan(1 + leftHash.Length));
        return SHA384.HashData(buffer);
    }
}

/// <summary>
/// Incremental merkle tree accumulator mirroring the reference
/// implementation's streaming hasher: leaf hashes are merged pairwise
/// as they arrive (binary-counter forest), and the final root folds
/// any remaining unbalanced subtrees right-to-left.  An empty tree's
/// root is <see cref="BlockHashing.EmptyTreeHash"/>.
/// </summary>
internal sealed class StreamingTreeHasher
{
    private readonly List<byte[]> _stack = [];
    private long _leafCount;

    public void AddLeaf(byte[] leafHash)
    {
        _stack.Add(leafHash);
        for (var n = _leafCount; (n & 1L) == 1L; n >>= 1)
        {
            var right = _stack[^1];
            var left = _stack[^2];
            _stack.RemoveRange(_stack.Count - 2, 2);
            _stack.Add(BlockHashing.HashInternalNode(left, right));
        }
        _leafCount++;
    }

    public byte[] RootHash()
    {
        if (_stack.Count == 0)
        {
            return BlockHashing.EmptyTreeHash;
        }
        var root = _stack[^1];
        for (var i = _stack.Count - 2; i >= 0; i--)
        {
            root = BlockHashing.HashInternalNode(_stack[i], root);
        }
        return root;
    }
}

/// <summary>
/// Why a block's root hash could not be computed.
/// </summary>
internal enum BlockHashFailure
{
    /// <summary>
    /// The root hash was computed.
    /// </summary>
    None,
    /// <summary>
    /// The block lacks something the computation requires (a header,
    /// a footer, or full-length footer hashes).
    /// </summary>
    Malformed,
    /// <summary>
    /// The block is well formed but holds content this SDK cannot hash
    /// with certainty (filtered or redacted items, item types it does
    /// not know, or an encoding the reference refuses), so any root it
    /// produced could disagree with the network's.
    /// </summary>
    Unverifiable,
}

/// <summary>
/// The outcome of a block root hash computation: the root, or the
/// reason there is none.
/// </summary>
internal readonly record struct BlockRootHashResult(byte[]? RootHash, BlockHashFailure Failure, string? Reason)
{
    public static BlockRootHashResult Computed(byte[] rootHash) => new(rootHash, BlockHashFailure.None, null);
    public static BlockRootHashResult Malformed(string reason) => new(null, BlockHashFailure.Malformed, reason);
    public static BlockRootHashResult Unverifiable(string reason) => new(null, BlockHashFailure.Unverifiable, reason);
}

/// <summary>
/// Computes a block's merkle root hash from its items and footer,
/// mirroring the block node reference implementation (v0.44:
/// block-verification BlockHasher + HashingUtilities.computeFinalBlockHash)
/// and the consensus node's BlockRootTree (v0.77 onward).  Each leaf
/// is the SHA-384 of the serialized whole BlockItem message with the
/// leaf prefix, partitioned into the five assigned item subtrees and
/// eight extension subtrees.  BlockFooter and BlockProof items are not
/// hashed.  Items the reference refuses to hash — filtered and redacted
/// items, unknown item types outside a hashable category, and malformed
/// encodings — make the block unverifiable rather than silently
/// producing a root the network would not.
/// </summary>
internal sealed class BlockHasher
{
    /// <summary>
    /// The first BlockItem field number governed by the block stream
    /// forward-compatibility numbering rule; unknown fields below it
    /// are reserved for item types requiring specific handling.
    /// </summary>
    private const int FirstForwardCompatibleFieldNumber = 20;
    /// <summary>
    /// For an unknown item numbered at or above
    /// <see cref="FirstForwardCompatibleFieldNumber"/>, its hashing
    /// category is the field number modulo this value.
    /// </summary>
    private const int CategoryModulus = 20;
    private const int ExtensionCount = 8;
    private readonly StreamingTreeHasher _consensusHeaders = new();
    private readonly StreamingTreeHasher _inputs = new();
    private readonly StreamingTreeHasher _outputs = new();
    private readonly StreamingTreeHasher _stateChanges = new();
    private readonly StreamingTreeHasher _traceData = new();
    private readonly StreamingTreeHasher[] _extensions = [new(), new(), new(), new(), new(), new(), new(), new()];
    private int _headerCount;
    private int _footerCount;
    private int _recordFileCount;

    /// <summary>
    /// The first reason this block cannot be verified, or
    /// <code>null</code> while every item added has been hashable.
    /// </summary>
    public string? UnverifiableReason { get; private set; }

    /// <summary>
    /// Routes one block item into its subtree (or ignores it, per the
    /// reference partitioning rules), recording the first reason the
    /// block cannot be verified.
    /// </summary>
    public void AddItem(ProtoBlockStream.BlockItem item)
    {
        var bytes = item.ToByteArray();
        var reason = item.ItemCase == ProtoBlockStream.BlockItem.ItemOneofCase.None
            ? AddFutureItem(bytes)
            : AddKnownItem(item.ItemCase, bytes);
        UnverifiableReason ??= reason;
    }

    private string? AddKnownItem(ProtoBlockStream.BlockItem.ItemOneofCase kind, byte[] bytes)
    {
        // A BlockItem is a oneof: a valid item carries exactly one field.
        // A known item alongside fields this build does not recognize
        // parses, but its meaning is not fully known (oneof case values
        // are the field numbers).
        foreach (var fieldNumber in ReadFieldNumbers(bytes))
        {
            if (fieldNumber != (int)kind)
            {
                return $"A {kind} item also carries field {fieldNumber}, which this SDK does not recognize.";
            }
        }
        switch (kind)
        {
            case ProtoBlockStream.BlockItem.ItemOneofCase.BlockHeader:
                if (++_headerCount > 1)
                {
                    return "The block contains more than one block header.";
                }
                _outputs.AddLeaf(BlockHashing.HashLeaf(bytes));
                return null;
            case ProtoBlockStream.BlockItem.ItemOneofCase.RoundHeader:
            case ProtoBlockStream.BlockItem.ItemOneofCase.EventHeader:
                _consensusHeaders.AddLeaf(BlockHashing.HashLeaf(bytes));
                return null;
            case ProtoBlockStream.BlockItem.ItemOneofCase.SignedTransaction:
                _inputs.AddLeaf(BlockHashing.HashLeaf(bytes));
                return null;
            case ProtoBlockStream.BlockItem.ItemOneofCase.TransactionResult:
            case ProtoBlockStream.BlockItem.ItemOneofCase.TransactionOutput:
                _outputs.AddLeaf(BlockHashing.HashLeaf(bytes));
                return null;
            case ProtoBlockStream.BlockItem.ItemOneofCase.RecordFile:
                if (++_recordFileCount > 1)
                {
                    return "The block contains more than one record file item.";
                }
                _outputs.AddLeaf(BlockHashing.HashLeaf(bytes));
                return null;
            case ProtoBlockStream.BlockItem.ItemOneofCase.StateChanges:
                _stateChanges.AddLeaf(BlockHashing.HashLeaf(bytes));
                return null;
            case ProtoBlockStream.BlockItem.ItemOneofCase.TraceData:
                _traceData.AddLeaf(BlockHashing.HashLeaf(bytes));
                return null;
            case ProtoBlockStream.BlockItem.ItemOneofCase.BlockFooter:
                return ++_footerCount > 1 ? "The block contains more than one block footer." : null;
            case ProtoBlockStream.BlockItem.ItemOneofCase.BlockProof:
                return null;
            case ProtoBlockStream.BlockItem.ItemOneofCase.FilteredSingleItem:
            case ProtoBlockStream.BlockItem.ItemOneofCase.RedactedItem:
                return $"The block contains a {kind} item; filtered and redacted blocks cannot be verified by recomputing their root.";
            default:
                return $"The block contains a {kind} item, which this SDK does not know how to hash.";
        }
    }

    /// <summary>
    /// Applies the block stream forward-compatibility numbering rule to
    /// an item type unknown to this build: a field numbered 20 or above
    /// is hashed into the subtree its category (field number modulo 20)
    /// names, or ignored when the category is not hashed; everything
    /// else is refused, because guessing could produce a root that
    /// disagrees with the network's.
    /// </summary>
    private string? AddFutureItem(byte[] bytes)
    {
        var fieldNumbers = ReadFieldNumbers(bytes);
        if (fieldNumbers.Count != 1)
        {
            return fieldNumbers.Count == 0
                ? "The block contains an empty item."
                : "The block contains an item carrying more than one unrecognized field.";
        }
        var fieldNumber = fieldNumbers[0];
        if (fieldNumber < FirstForwardCompatibleFieldNumber)
        {
            return $"The block contains an item of unknown type (field {fieldNumber}) reserved for types requiring specific handling.";
        }
        var category = fieldNumber % CategoryModulus;
        var subtree = category switch
        {
            3 => _consensusHeaders,
            4 => _inputs,
            5 => _outputs,
            6 => _stateChanges,
            7 => _traceData,
            >= 8 and <= 15 => _extensions[category - 8],
            _ => null
        };
        if (subtree is not null)
        {
            subtree.AddLeaf(BlockHashing.HashLeaf(bytes));
            return null;
        }
        // Categories 0 and 19 are outside the merkle tree; 1, 2 and
        // 16-18 require handling this build does not know.
        return category is 0 or 19
            ? null
            : $"The block contains an item of unknown type (field {fieldNumber}) in a category requiring specific handling.";
    }

    /// <summary>
    /// The top-level field numbers present in a serialized message.
    /// </summary>
    private static List<int> ReadFieldNumbers(byte[] bytes)
    {
        var fieldNumbers = new List<int>(1);
        var input = new CodedInputStream(bytes);
        uint tag;
        while ((tag = input.ReadTag()) != 0)
        {
            fieldNumbers.Add(WireFormat.GetTagFieldNumber(tag));
            input.SkipLastField();
        }
        return fieldNumbers;
    }

    /// <summary>
    /// Combines the footer's chaining hashes, the five item subtree
    /// roots and the eight extension subtree roots into the fixed
    /// 16-branch tree ("Merkle Mountain Top", HIP-1424), paired with
    /// the block timestamp leaf at the root.
    /// </summary>
    /// <param name="footer">
    /// The block's footer (previous block root hash, root of all
    /// previous block hashes, start-of-block state root).
    /// </param>
    /// <param name="blockTimestamp">
    /// The block header's timestamp.
    /// </param>
    public byte[] ComputeRootHash(ProtoBlockStream.Output.BlockFooter footer, Proto.Timestamp blockTimestamp)
    {
        // An absent state root hash is treated as an empty subtree,
        // matching the reference implementation.
        var stateRootHash = footer.StartOfBlockStateRootHash.Length == 0
            ? BlockHashing.EmptyTreeHash
            : footer.StartOfBlockStateRootHash.ToByteArray();
        var branches = new StreamingTreeHasher();
        branches.AddLeaf(footer.PreviousBlockRootHash.ToByteArray());
        branches.AddLeaf(footer.RootHashOfAllBlockHashesTree.ToByteArray());
        branches.AddLeaf(stateRootHash);
        branches.AddLeaf(_consensusHeaders.RootHash());
        branches.AddLeaf(_inputs.RootHash());
        branches.AddLeaf(_outputs.RootHash());
        branches.AddLeaf(_stateChanges.RootHash());
        branches.AddLeaf(_traceData.RootHash());
        for (var i = 0; i < ExtensionCount; i++)
        {
            branches.AddLeaf(_extensions[i].RootHash());
        }
        var timestampLeaf = BlockHashing.HashLeaf(blockTimestamp.ToByteArray());
        return BlockHashing.HashInternalNode(timestampLeaf, branches.RootHash());
    }

    /// <summary>
    /// Computes the root hash of a complete block (items must include a
    /// header and footer).
    /// </summary>
    /// <returns>
    /// The computed root hash, or the reason the block is malformed or
    /// cannot be verified.
    /// </returns>
    public static BlockRootHashResult ComputeBlockRootHash(ProtoBlockStream.Block block)
    {
        ProtoBlockStream.Output.BlockFooter? footer = null;
        Proto.Timestamp? timestamp = null;
        var hasher = new BlockHasher();
        foreach (var item in block.Items)
        {
            if (item.ItemCase == ProtoBlockStream.BlockItem.ItemOneofCase.BlockHeader)
            {
                timestamp ??= item.BlockHeader.BlockTimestamp;
            }
            else if (item.ItemCase == ProtoBlockStream.BlockItem.ItemOneofCase.BlockFooter)
            {
                footer ??= item.BlockFooter;
            }
            hasher.AddItem(item);
        }
        if (footer is null || timestamp is null)
        {
            return BlockRootHashResult.Malformed("The block lacks the header or footer required to compute its root hash.");
        }
        if (footer.PreviousBlockRootHash.Length != BlockHashing.HashSize || footer.RootHashOfAllBlockHashesTree.Length != BlockHashing.HashSize)
        {
            return BlockRootHashResult.Malformed("The block footer's previous block root hash and root of all previous block hashes must each be 48 bytes.");
        }
        if (hasher.UnverifiableReason is not null)
        {
            return BlockRootHashResult.Unverifiable(hasher.UnverifiableReason);
        }
        return BlockRootHashResult.Computed(hasher.ComputeRootHash(footer, timestamp));
    }
}
