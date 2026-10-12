// SPDX-License-Identifier: Apache-2.0
using Hiero.BlockNode;
using Hiero.Implementation;
using Hiero.Test.Integration.Fixtures;
using System.Threading.Channels;

namespace Hiero.Test.Integration.BlockNode;

[Category("Block Node")]
public class BlockHashChainConformanceTests
{
    /// <summary>
    /// The live conformance oracle for the hash-chain implementation:
    /// the computed SHA-384 merkle root of block N must equal block
    /// N+1's footer previous_block_root_hash.  If this passes over a
    /// window of real blocks, the hasher matches the network's block
    /// hashing, independent of the (unpublished) specification text.
    /// </summary>
    [Test]
    public async Task Computed_Roots_Chain_Across_Live_Blocks()
    {
        if (TestNetwork.BlockNodeUri is null)
        {
            Skip.Test("Block Node Endpoint [BlockNodeEndpoint] is not configured.");
        }
        await using var client = TestNetwork.CreateBlockNodeClient();
        var status = await client.GetServerStatusAsync();
        if (status.LastAvailableBlockNumber < status.FirstAvailableBlockNumber + 25)
        {
            Skip.Test("The block node does not hold enough history for this test.");
        }
        var start = status.LastAvailableBlockNumber - 21;
        var channel = Channel.CreateUnbounded<Block>();
        var subscription = client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
        {
            StartingBlockNumber = start,
            EndingBlockNumber = start + 20,
            BlockWriter = channel.Writer
        });
        var blocks = new List<Block>();
        await foreach (var block in channel.Reader.ReadAllAsync())
        {
            blocks.Add(block);
        }
        await subscription;
        await Assert.That(blocks.Count).IsEqualTo(21);
        for (var i = 0; i < blocks.Count - 1; i++)
        {
            var computed = BlockHasher.ComputeBlockRootHash(blocks[i].ToProtobuf()).RootHash;
            var expected = blocks[i + 1].Footer!.PreviousBlockRootHash.ToArray();
            await Assert.That(computed).IsNotNull();
            await Assert.That(Convert.ToHexString(computed!)).IsEqualTo(Convert.ToHexString(expected));
        }
    }

    /// <summary>
    /// End-to-end: a HashChain subscription verifies live blocks and a
    /// checkpoint resume continues the chain cryptographically.
    /// </summary>
    [Test]
    public async Task HashChain_Subscription_And_Checkpoint_Resume_Verify_Live_Blocks()
    {
        if (TestNetwork.BlockNodeUri is null)
        {
            Skip.Test("Block Node Endpoint [BlockNodeEndpoint] is not configured.");
        }
        await using var client = TestNetwork.CreateBlockNodeClient();
        var status = await client.GetServerStatusAsync();
        if (status.LastAvailableBlockNumber < status.FirstAvailableBlockNumber + 12)
        {
            Skip.Test("The block node does not hold enough history for this test.");
        }
        var start = status.LastAvailableBlockNumber - 10;
        var channel = Channel.CreateUnbounded<Block>();
        var subscription = client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
        {
            StartingBlockNumber = start,
            EndingBlockNumber = start + 4,
            VerificationLevel = BlockVerificationLevel.HashChain,
            BlockWriter = channel.Writer
        });
        var blocks = new List<Block>();
        await foreach (var block in channel.Reader.ReadAllAsync())
        {
            blocks.Add(block);
        }
        await subscription;
        await Assert.That(blocks.Count).IsEqualTo(5);
        foreach (var block in blocks)
        {
            await Assert.That(block.VerifiedRootHash.IsEmpty).IsFalse();
        }
        // Resume from the last verified block as a checkpoint: the next
        // block must chain from its computed root.
        var checkpoint = new BlockStreamCheckpoint(blocks[^1].Number, blocks[^1].VerifiedRootHash);
        var resumed = Channel.CreateUnbounded<Block>();
        var resumedSubscription = client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
        {
            ResumeFromCheckpoint = checkpoint,
            EndingBlockNumber = checkpoint.BlockNumber + 2,
            VerificationLevel = BlockVerificationLevel.HashChain,
            BlockWriter = resumed.Writer
        });
        var resumedBlocks = new List<Block>();
        await foreach (var block in resumed.Reader.ReadAllAsync())
        {
            resumedBlocks.Add(block);
        }
        await resumedSubscription;
        await Assert.That(resumedBlocks.Count).IsEqualTo(2);
        await Assert.That(resumedBlocks[0].Number).IsEqualTo(checkpoint.BlockNumber + 1);
        await Assert.That(resumedBlocks[0].VerifiedRootHash.IsEmpty).IsFalse();
    }

    /// <summary>
    /// Opt-in unary verification against the live node.
    /// </summary>
    [Test]
    public async Task Unary_Get_With_Verify_Populates_Root()
    {
        if (TestNetwork.BlockNodeUri is null)
        {
            Skip.Test("Block Node Endpoint [BlockNodeEndpoint] is not configured.");
        }
        await using var client = TestNetwork.CreateBlockNodeClient();
        var latest = await client.GetLatestBlockAsync();
        var block = await client.GetVerifiedBlockAsync(latest.Number - 2);
        await Assert.That(block).IsNotNull();
        await Assert.That(block!.VerifiedRootHash.IsEmpty).IsFalse();
    }

    /// <summary>
    /// Every block's footer must also chain to its predecessor's
    /// computed root within the same window (the internal linkage the
    /// HashChain verification level enforces during streaming).
    /// </summary>
    [Test]
    public async Task Footer_Previous_Hash_Matches_Predecessor_Computed_Root()
    {
        if (TestNetwork.BlockNodeUri is null)
        {
            Skip.Test("Block Node Endpoint [BlockNodeEndpoint] is not configured.");
        }
        await using var client = TestNetwork.CreateBlockNodeClient();
        var latest = await client.GetLatestBlockAsync();
        var previous = await client.GetBlockAsync(latest.Number - 1);
        await Assert.That(previous).IsNotNull();
        var computed = BlockHasher.ComputeBlockRootHash(previous!.ToProtobuf()).RootHash;
        await Assert.That(computed).IsNotNull();
        await Assert.That(Convert.ToHexString(computed!)).IsEqualTo(Convert.ToHexString(latest.Footer!.PreviousBlockRootHash.ToArray()));
    }
}
