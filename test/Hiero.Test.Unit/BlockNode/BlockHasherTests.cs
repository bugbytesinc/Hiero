// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Hiero.Implementation;
using System.Security.Cryptography;
using ProtoBlock = Com.Hedera.Hapi.Block.Stream;

namespace Hiero.Test.Unit.BlockNode;

public class BlockHasherTests
{
    private static byte[] Prefixed(byte prefix, params byte[][] parts)
    {
        var buffer = new List<byte> { prefix };
        foreach (var part in parts)
        {
            buffer.AddRange(part);
        }
        return SHA384.HashData(buffer.ToArray());
    }

    /// <summary>
    /// The straightforward recursive merkle root the streaming hasher
    /// must agree with: pair leaves left-to-right; an unpaired tail
    /// subtree combines into the result without padding.
    /// </summary>
    private static byte[] NaiveRoot(byte[][] leafHashes)
    {
        if (leafHashes.Length == 0)
        {
            return BlockHashing.EmptyTreeHash;
        }
        var level = leafHashes.ToList();
        while (level.Count > 1)
        {
            var next = new List<byte[]>();
            for (var i = 0; i + 1 < level.Count; i += 2)
            {
                next.Add(BlockHashing.HashInternalNode(level[i], level[i + 1]));
            }
            if (level.Count % 2 == 1)
            {
                next.Add(level[^1]);
            }
            level = next;
        }
        return level[0];
    }

    /// <summary>
    /// The block root as the consensus node's v0.77
    /// CachedReservedHalfBlockRootTreeHasher writes it out — the
    /// unrolled 16-branch tree — independently of the streaming hasher.
    /// Slots 0-7 are the assigned branches (previous block root, root
    /// of all previous block hashes, state root, consensus headers,
    /// inputs, outputs, state changes, trace data); 8-15 are the
    /// extensions.  A future item's category is its slot index.
    /// </summary>
    private static byte[] ReferenceRoot(byte[][] slots, Proto.Timestamp timestamp)
    {
        static byte[] N(byte[] left, byte[] right) => BlockHashing.HashInternalNode(left, right);
        var assigned = N(N(N(slots[0], slots[1]), N(slots[2], slots[3])), N(N(slots[4], slots[5]), N(slots[6], slots[7])));
        var reserved = N(N(N(slots[8], slots[9]), N(slots[10], slots[11])), N(N(slots[12], slots[13]), N(slots[14], slots[15])));
        return N(BlockHashing.HashLeaf(timestamp.ToByteArray()), N(assigned, reserved));
    }

    private static byte[][] EmptySlots(ProtoBlock.Output.BlockFooter footer)
    {
        var slots = Enumerable.Repeat(BlockHashing.EmptyTreeHash, 16).ToArray();
        slots[0] = footer.PreviousBlockRootHash.ToByteArray();
        slots[1] = footer.RootHashOfAllBlockHashesTree.ToByteArray();
        slots[2] = footer.StartOfBlockStateRootHash.ToByteArray();
        return slots;
    }

    /// <summary>
    /// A block item carrying only fields this SDK build does not know,
    /// as a newer producer would serialize it.
    /// </summary>
    private static ProtoBlock.BlockItem UnknownItem(params int[] fieldNumbers)
    {
        return ProtoBlock.BlockItem.Parser.ParseFrom(UnknownFieldBytes(fieldNumbers));
    }

    private static byte[] UnknownFieldBytes(params int[] fieldNumbers)
    {
        using var stream = new MemoryStream();
        var output = new CodedOutputStream(stream);
        foreach (var fieldNumber in fieldNumbers)
        {
            output.WriteTag(fieldNumber, WireFormat.WireType.LengthDelimited);
            output.WriteBytes(ByteString.CopyFrom((byte)fieldNumber, 1, 2));
        }
        output.Flush();
        return stream.ToArray();
    }

    private static ProtoBlock.Output.BlockFooter Footer()
    {
        return BlockDomainTests.CreateFooterItem(BlockDomainTests.RandomHash()).BlockFooter;
    }

    private static ProtoBlock.Block BlockWith(params ProtoBlock.BlockItem[] items)
    {
        var block = new ProtoBlock.Block();
        block.Items.Add(BlockDomainTests.CreateHeaderItem(7));
        block.Items.AddRange(items);
        block.Items.Add(BlockDomainTests.CreateFooterItem(BlockDomainTests.RandomHash()));
        block.Items.Add(BlockDomainTests.CreateProofItem(7));
        return block;
    }

    [Test]
    public async Task Hash_Algebra_Matches_Reference_Definitions()
    {
        var data = new byte[] { 1, 2, 3 };
        var left = SHA384.HashData(new byte[] { 9 });
        var right = SHA384.HashData(new byte[] { 8 });
        await Assert.That(BlockHashing.EmptyTreeHash).IsEquivalentTo(SHA384.HashData(new byte[] { 0x00 }));
        await Assert.That(BlockHashing.HashLeaf(data)).IsEquivalentTo(Prefixed(0x00, data));
        await Assert.That(BlockHashing.HashInternalNode(left, right)).IsEquivalentTo(Prefixed(0x02, left, right));
    }

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(5)]
    [Arguments(8)]
    [Arguments(13)]
    [Arguments(100)]
    public async Task Streaming_Hasher_Agrees_With_Naive_Merkle_Root(int leafCount)
    {
        var leaves = new byte[leafCount][];
        var hasher = new StreamingTreeHasher();
        for (var i = 0; i < leafCount; i++)
        {
            leaves[i] = BlockHashing.HashLeaf([(byte)i, (byte)(i * 7)]);
            hasher.AddLeaf(leaves[i]);
        }
        await Assert.That(hasher.RootHash()).IsEquivalentTo(NaiveRoot(leaves));
    }

    [Test]
    public async Task Root_Matches_The_Sixteen_Branch_Reference_Tree()
    {
        var footer = Footer();
        var timestamp = new Proto.Timestamp { Seconds = 42, Nanos = 7 };
        var transaction = BlockDomainTests.CreateTransactionItem([1, 2, 3]);
        var stateChanges = new ProtoBlock.BlockItem { StateChanges = new ProtoBlock.Output.StateChanges() };
        var hasher = new BlockHasher();
        hasher.AddItem(transaction);
        hasher.AddItem(stateChanges);
        var slots = EmptySlots(footer);
        slots[4] = BlockHashing.HashLeaf(transaction.ToByteArray());
        slots[6] = BlockHashing.HashLeaf(stateChanges.ToByteArray());
        await Assert.That(hasher.ComputeRootHash(footer, timestamp)).IsEquivalentTo(ReferenceRoot(slots, timestamp));
    }

    [Test]
    public async Task Root_Hash_Treats_An_Absent_State_Root_As_An_Empty_Subtree()
    {
        var footerWithEmptyTree = new ProtoBlock.Output.BlockFooter
        {
            PreviousBlockRootHash = ByteString.CopyFrom(new byte[48]),
            RootHashOfAllBlockHashesTree = ByteString.CopyFrom(new byte[48]),
            StartOfBlockStateRootHash = ByteString.CopyFrom(BlockHashing.EmptyTreeHash)
        };
        var footerWithAbsent = new ProtoBlock.Output.BlockFooter
        {
            PreviousBlockRootHash = ByteString.CopyFrom(new byte[48]),
            RootHashOfAllBlockHashesTree = ByteString.CopyFrom(new byte[48])
            // StartOfBlockStateRootHash intentionally absent (empty).
        };
        var timestamp = new Proto.Timestamp { Seconds = 12, Nanos = 34 };
        var explicitEmpty = new BlockHasher().ComputeRootHash(footerWithEmptyTree, timestamp);
        var absent = new BlockHasher().ComputeRootHash(footerWithAbsent, timestamp);
        await Assert.That(absent).IsEquivalentTo(explicitEmpty);
    }

    [Test]
    public async Task Item_Routing_Changes_Only_The_Assigned_Subtree()
    {
        // Two hashers fed identical items except one extra state-change:
        // the roots must differ (the item was hashed), while adding
        // items that are not hashed (footer, proof) must not change it.
        var footer = Footer();
        var timestamp = new Proto.Timestamp { Seconds = 1 };
        var baseline = new BlockHasher();
        baseline.AddItem(BlockDomainTests.CreateTransactionItem([1, 2]));
        var withState = new BlockHasher();
        withState.AddItem(BlockDomainTests.CreateTransactionItem([1, 2]));
        withState.AddItem(new ProtoBlock.BlockItem { StateChanges = new ProtoBlock.Output.StateChanges() });
        var withIgnored = new BlockHasher();
        withIgnored.AddItem(BlockDomainTests.CreateTransactionItem([1, 2]));
        withIgnored.AddItem(new ProtoBlock.BlockItem { BlockFooter = footer });
        withIgnored.AddItem(BlockDomainTests.CreateProofItem(1));

        var baselineRoot = baseline.ComputeRootHash(footer, timestamp);
        await Assert.That(withState.ComputeRootHash(footer, timestamp)).IsNotEquivalentTo(baselineRoot);
        await Assert.That(withIgnored.ComputeRootHash(footer, timestamp)).IsEquivalentTo(baselineRoot);
        await Assert.That(withIgnored.UnverifiableReason).IsNull();
    }

    /// <summary>
    /// The forward-compatibility numbering rule: an unknown item numbered
    /// 20 or above hashes into the subtree its category (field number
    /// modulo 20) names — 3-7 the five item subtrees, 8-15 the eight
    /// extensions.
    /// </summary>
    [Test]
    [Arguments(23)]
    [Arguments(24)]
    [Arguments(25)]
    [Arguments(26)]
    [Arguments(27)]
    [Arguments(28)]
    [Arguments(31)]
    [Arguments(35)]
    [Arguments(43)]
    public async Task Unknown_Item_In_A_Hashed_Category_Is_Hashed_Into_Its_Subtree(int fieldNumber)
    {
        var footer = Footer();
        var timestamp = new Proto.Timestamp { Seconds = 9 };
        var item = UnknownItem(fieldNumber);
        var hasher = new BlockHasher();
        hasher.AddItem(item);
        var slots = EmptySlots(footer);
        slots[fieldNumber % 20] = BlockHashing.HashLeaf(item.ToByteArray());
        await Assert.That(hasher.UnverifiableReason).IsNull();
        await Assert.That(hasher.ComputeRootHash(footer, timestamp)).IsEquivalentTo(ReferenceRoot(slots, timestamp));
    }

    [Test]
    [Arguments(20)]
    [Arguments(39)]
    [Arguments(40)]
    public async Task Unknown_Item_In_An_Unhashed_Category_Is_Ignored(int fieldNumber)
    {
        var footer = Footer();
        var timestamp = new Proto.Timestamp { Seconds = 9 };
        var baseline = new BlockHasher();
        var withUnknown = new BlockHasher();
        withUnknown.AddItem(UnknownItem(fieldNumber));
        await Assert.That(withUnknown.UnverifiableReason).IsNull();
        await Assert.That(withUnknown.ComputeRootHash(footer, timestamp)).IsEquivalentTo(baseline.ComputeRootHash(footer, timestamp));
    }

    [Test]
    [Arguments("unknown field below 20")]
    [Arguments("category 1")]
    [Arguments("category 2")]
    [Arguments("category 16")]
    [Arguments("category 18")]
    [Arguments("two unknown fields")]
    [Arguments("empty item")]
    [Arguments("known item with an unknown field")]
    [Arguments("filtered item")]
    [Arguments("redacted item")]
    [Arguments("second footer")]
    [Arguments("second record file")]
    [Arguments("second header")]
    public async Task Content_The_Reference_Refuses_Makes_The_Block_Unverifiable(string content)
    {
        ProtoBlock.BlockItem[] items = content switch
        {
            "unknown field below 20" => [UnknownItem(13)],
            "category 1" => [UnknownItem(21)],
            "category 2" => [UnknownItem(22)],
            "category 16" => [UnknownItem(36)],
            "category 18" => [UnknownItem(38)],
            "two unknown fields" => [UnknownItem(23, 24)],
            "empty item" => [new ProtoBlock.BlockItem()],
            "known item with an unknown field" => [ProtoBlock.BlockItem.Parser.ParseFrom([.. BlockDomainTests.CreateTransactionItem([1]).ToByteArray(), .. UnknownFieldBytes(30)])],
            "filtered item" => [new ProtoBlock.BlockItem { FilteredSingleItem = new ProtoBlock.FilteredSingleItem() }],
            "redacted item" => [new ProtoBlock.BlockItem { RedactedItem = new ProtoBlock.RedactedItem() }],
            "second footer" => [BlockDomainTests.CreateFooterItem(BlockDomainTests.RandomHash())],
            "second record file" => [new ProtoBlock.BlockItem { RecordFile = new ProtoBlock.RecordFileItem() }, new ProtoBlock.BlockItem { RecordFile = new ProtoBlock.RecordFileItem() }],
            "second header" => [BlockDomainTests.CreateHeaderItem(7)],
            _ => throw new ArgumentOutOfRangeException(nameof(content))
        };
        var result = BlockHasher.ComputeBlockRootHash(BlockWith(items));
        await Assert.That(result.Failure).IsEqualTo(BlockHashFailure.Unverifiable);
        await Assert.That(result.RootHash).IsNull();
        await Assert.That(result.Reason).IsNotNull();
    }

    [Test]
    public async Task A_Block_With_Only_Hashable_Content_Computes()
    {
        var result = BlockHasher.ComputeBlockRootHash(BlockWith(BlockDomainTests.CreateTransactionItem([1]), UnknownItem(20)));
        await Assert.That(result.Failure).IsEqualTo(BlockHashFailure.None);
        await Assert.That(result.RootHash!.Length).IsEqualTo(48);
    }

    [Test]
    public async Task Compute_Reports_Malformed_Without_Header_Or_Footer()
    {
        var headerless = new ProtoBlock.Block();
        headerless.Items.Add(BlockDomainTests.CreateFooterItem(BlockDomainTests.RandomHash()));
        await Assert.That(BlockHasher.ComputeBlockRootHash(headerless).Failure).IsEqualTo(BlockHashFailure.Malformed);
        var footerless = new ProtoBlock.Block();
        footerless.Items.Add(BlockDomainTests.CreateHeaderItem(5));
        await Assert.That(BlockHasher.ComputeBlockRootHash(footerless).Failure).IsEqualTo(BlockHashFailure.Malformed);
    }

    [Test]
    public async Task Compute_Reports_Malformed_When_Footer_Hashes_Are_Not_Full_Length()
    {
        var block = new ProtoBlock.Block();
        block.Items.Add(BlockDomainTests.CreateHeaderItem(5));
        block.Items.Add(BlockDomainTests.CreateFooterItem([1, 2, 3]));
        var result = BlockHasher.ComputeBlockRootHash(block);
        await Assert.That(result.Failure).IsEqualTo(BlockHashFailure.Malformed);
        await Assert.That(result.RootHash).IsNull();
    }

    /// <summary>
    /// Offline conformance: wrapped record blocks captured from three
    /// mainnet block nodes on 2026-10-05, spanning header HAPI versions
    /// 0.3.0, 0.11.0 and 0.77.5 — all hashed with the 16-branch tree.
    /// Each stored expected root is the following block's footer
    /// previous_block_root_hash — network ground truth.
    /// </summary>
    [Test]
    [Arguments(1441576UL)]
    [Arguments(18897916UL)]
    [Arguments(100759413UL)]
    public async Task Computed_Root_Matches_Mainnet_Ground_Truth_Fixture(ulong blockNumber)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "BlockNode", "Fixtures");
        var block = ProtoBlock.Block.Parser.ParseFrom(await System.IO.File.ReadAllBytesAsync(Path.Combine(directory, $"mainnet-block-{blockNumber}.bin")));
        var expected = (await System.IO.File.ReadAllTextAsync(Path.Combine(directory, $"mainnet-block-{blockNumber}.root.hex"))).Trim();
        var result = BlockHasher.ComputeBlockRootHash(block);
        await Assert.That(result.Failure).IsEqualTo(BlockHashFailure.None);
        await Assert.That(Convert.ToHexString(result.RootHash!)).IsEqualTo(expected);
    }
}
