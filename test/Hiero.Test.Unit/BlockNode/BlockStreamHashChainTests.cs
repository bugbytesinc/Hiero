// SPDX-License-Identifier: Apache-2.0
using Grpc.Core;
using Hiero.BlockNode;
using Hiero.Implementation;
using Org.Hiero.Block.Api;
using System.Threading.Channels;
using ProtoBlock = Com.Hedera.Hapi.Block.Stream;

namespace Hiero.Test.Unit.BlockNode;

public class BlockStreamHashChainTests
{
    private const string ServerStatusPath = "/org.hiero.block.api.BlockNodeService/serverStatus";
    private const string SubscribePath = "/org.hiero.block.api.BlockStreamSubscribeService/subscribeBlockStream";
    private const string GetBlockPath = "/org.hiero.block.api.BlockAccessService/getBlock";

    /// <summary>
    /// Builds a complete block whose footer chains from the given
    /// previous root, returning the block's items and its computed root.
    /// </summary>
    internal static (ProtoBlock.BlockItem[] Items, byte[] Root) ChainedBlock(ulong number, byte[] previousRoot)
    {
        var items = new[]
        {
            BlockDomainTests.CreateHeaderItem(number, seconds: (long)number),
            BlockDomainTests.CreateTransactionItem([(byte)number, 2, 3]),
            BlockDomainTests.CreateFooterItem(previousRoot),
            BlockDomainTests.CreateProofItem(number)
        };
        var proto = new ProtoBlock.Block();
        proto.Items.AddRange(items);
        return (items, BlockHasher.ComputeBlockRootHash(proto).RootHash!);
    }

    /// <summary>
    /// A well-formed block holding a filtered item, which the reference
    /// verifier refuses to hash: structurally valid, but unverifiable.
    /// </summary>
    internal static ProtoBlock.BlockItem[] UnverifiableBlock(ulong number)
    {
        return
        [
            BlockDomainTests.CreateHeaderItem(number, seconds: (long)number),
            new ProtoBlock.BlockItem { FilteredSingleItem = new ProtoBlock.FilteredSingleItem() },
            BlockDomainTests.CreateFooterItem(BlockDomainTests.RandomHash()),
            BlockDomainTests.CreateProofItem(number)
        ];
    }

    internal static SubscribeStreamResponse[] ChainedBlockResponses(ProtoBlock.BlockItem[] items, ulong number)
    {
        return [BlockStreamAssemblerTests.Items(items), BlockStreamAssemblerTests.End(number)];
    }

    [Test]
    public async Task Chained_Blocks_Verify_And_Carry_Root()
    {
        var (items10, root10) = ChainedBlock(10, BlockDomainTests.RandomHash());
        var (items11, root11) = ChainedBlock(11, root10);
        var assembler = new BlockStreamAssembler(BlockVerificationLevel.HashChain, BlockStreamGapPolicy.Resubscribe, null);
        var steps = new[]
        {
            assembler.Process(BlockStreamAssemblerTests.Items(items10)),
            assembler.Process(BlockStreamAssemblerTests.End(10)),
            assembler.Process(BlockStreamAssemblerTests.Items(items11)),
            assembler.Process(BlockStreamAssemblerTests.End(11)),
        };
        var delivered = steps.Where(s => s.Delivered is not null).Select(s => s.Delivered!).ToArray();
        await Assert.That(delivered.Length).IsEqualTo(2);
        await Assert.That(delivered[0].VerifiedRootHash.ToArray()).IsEquivalentTo(root10);
        await Assert.That(delivered[1].VerifiedRootHash.ToArray()).IsEquivalentTo(root11);
        await Assert.That(assembler.LastComputedRootHash).IsEquivalentTo(root11);
    }

    [Test]
    public async Task Broken_Chain_Produces_HashMismatch_Fault()
    {
        var (items10, _) = ChainedBlock(10, BlockDomainTests.RandomHash());
        // Block 11 chains from a WRONG previous root.
        var (items11, _) = ChainedBlock(11, BlockDomainTests.RandomHash());
        var assembler = new BlockStreamAssembler(BlockVerificationLevel.HashChain, BlockStreamGapPolicy.Resubscribe, null);
        assembler.Process(BlockStreamAssemblerTests.Items(items10));
        assembler.Process(BlockStreamAssemblerTests.End(10));
        assembler.Process(BlockStreamAssemblerTests.Items(items11));
        var step = assembler.Process(BlockStreamAssemblerTests.End(11));
        await Assert.That(step.Fault).IsNotNull();
        await Assert.That(step.Fault!.Kind).IsEqualTo(BlockStreamFaultKind.HashMismatch);
    }

    [Test]
    public async Task Checkpoint_Seed_Enforces_Continuity()
    {
        var checkpointRoot = BlockDomainTests.RandomHash();
        var (goodItems, _) = ChainedBlock(50, checkpointRoot);
        var good = new BlockStreamAssembler(BlockVerificationLevel.HashChain, BlockStreamGapPolicy.Resubscribe, 49, checkpointRoot);
        good.Process(BlockStreamAssemblerTests.Items(goodItems));
        var goodStep = good.Process(BlockStreamAssemblerTests.End(50));
        await Assert.That(goodStep.Delivered).IsNotNull();

        var (badItems, _) = ChainedBlock(50, BlockDomainTests.RandomHash());
        var bad = new BlockStreamAssembler(BlockVerificationLevel.HashChain, BlockStreamGapPolicy.Resubscribe, 49, checkpointRoot);
        bad.Process(BlockStreamAssemblerTests.Items(badItems));
        var badStep = bad.Process(BlockStreamAssemblerTests.End(50));
        await Assert.That(badStep.Fault).IsNotNull();
        await Assert.That(badStep.Fault!.Kind).IsEqualTo(BlockStreamFaultKind.HashMismatch);
    }

    [Test]
    public async Task Deliver_Policy_Computes_Roots_But_Skips_Continuity()
    {
        var (items10, root10) = ChainedBlock(10, BlockDomainTests.RandomHash());
        // Block 12 (gap) with a non-chaining previous root.
        var (items12, root12) = ChainedBlock(12, BlockDomainTests.RandomHash());
        var assembler = new BlockStreamAssembler(BlockVerificationLevel.HashChain, BlockStreamGapPolicy.Deliver, null);
        assembler.Process(BlockStreamAssemblerTests.Items(items10));
        var step10 = assembler.Process(BlockStreamAssemblerTests.End(10));
        assembler.Process(BlockStreamAssemblerTests.Items(items12));
        var step12 = assembler.Process(BlockStreamAssemblerTests.End(12));
        await Assert.That(step10.Delivered!.VerifiedRootHash.ToArray()).IsEquivalentTo(root10);
        await Assert.That(step12.Delivered!.VerifiedRootHash.ToArray()).IsEquivalentTo(root12);
        await Assert.That(step12.Fault).IsNull();
    }

    [Test]
    public async Task Engine_Throws_VerificationFailed_Even_Under_Resubscribe()
    {
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(ServerStatusPath, StatusCode.OK, new ServerStatusResponse { FirstAvailableBlock = 0, LastAvailableBlock = 1000 });
        var (items10, _) = ChainedBlock(10, BlockDomainTests.RandomHash());
        var (badItems11, _) = ChainedBlock(11, BlockDomainTests.RandomHash());
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. ChainedBlockResponses(items10, 10),
            .. ChainedBlockResponses(badItems11, 11)
        ]);
        await using var client = new BlockNodeClient(_ => handler.CreateChannel(), ctx =>
        {
            ctx.Uri = new Uri("http://fake-block-node:40840");
            ctx.RetryDelay = TimeSpan.FromMilliseconds(1);
        });
        var channel = Channel.CreateUnbounded<Block>();
        var exception = await Assert.ThrowsAsync<BlockNodeException>(async () => await client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
        {
            StartingBlockNumber = 10,
            BlockWriter = channel.Writer,
            VerificationLevel = BlockVerificationLevel.HashChain,
            GapPolicy = BlockStreamGapPolicy.Resubscribe
        }));
        await Assert.That(exception!.Code).IsEqualTo(BlockNodeExceptionCode.VerificationFailed);
        // Only one subscription attempt: hash mismatches are not retried.
        await Assert.That(handler.CallCount(SubscribePath)).IsEqualTo(1);
    }

    [Test]
    public async Task Checkpoint_Params_Are_Validated()
    {
        var handler = new FakeBlockNodeHandler();
        await using var client = new BlockNodeClient(_ => handler.CreateChannel(), ctx => ctx.Uri = new Uri("http://fake-block-node:40840"));
        var channel = Channel.CreateUnbounded<Block>();
        // Both StartingBlockNumber and ResumeFromCheckpoint.
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
        {
            StartingBlockNumber = 5,
            ResumeFromCheckpoint = new BlockStreamCheckpoint(4, new byte[48]),
            VerificationLevel = BlockVerificationLevel.HashChain,
            BlockWriter = channel.Writer
        }));
        // Checkpoint without HashChain.
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
        {
            ResumeFromCheckpoint = new BlockStreamCheckpoint(4, new byte[48]),
            BlockWriter = channel.Writer
        }));
        // Wrong hash length.
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
        {
            ResumeFromCheckpoint = new BlockStreamCheckpoint(4, new byte[16]),
            VerificationLevel = BlockVerificationLevel.HashChain,
            BlockWriter = channel.Writer
        }));
    }

    [Test]
    public async Task Checkpoint_Resume_Starts_After_Checkpoint_And_Verifies()
    {
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(ServerStatusPath, StatusCode.OK, new ServerStatusResponse { FirstAvailableBlock = 0, LastAvailableBlock = 1000 });
        var checkpointRoot = BlockDomainTests.RandomHash();
        var (items100, _) = ChainedBlock(100, checkpointRoot);
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. ChainedBlockResponses(items100, 100),
            BlockStreamAssemblerTests.Terminal(SubscribeStreamResponse.Types.Code.Success)
        ]);
        await using var client = new BlockNodeClient(_ => handler.CreateChannel(), ctx =>
        {
            ctx.Uri = new Uri("http://fake-block-node:40840");
            ctx.RetryDelay = TimeSpan.FromMilliseconds(1);
        });
        var channel = Channel.CreateUnbounded<Block>();
        var subscription = client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
        {
            ResumeFromCheckpoint = new BlockStreamCheckpoint(99, checkpointRoot),
            EndingBlockNumber = 100,
            VerificationLevel = BlockVerificationLevel.HashChain,
            BlockWriter = channel.Writer
        });
        var blocks = new List<Block>();
        await foreach (var block in channel.Reader.ReadAllAsync())
        {
            blocks.Add(block);
        }
        await subscription;
        await Assert.That(blocks.Count).IsEqualTo(1);
        await Assert.That(blocks[0].Number).IsEqualTo(100ul);
        await Assert.That(blocks[0].VerifiedRootHash.IsEmpty).IsFalse();
        var request = SubscribeStreamRequest.Parser.ParseFrom(handler.Requests(SubscribePath)[0]);
        await Assert.That(request.StartBlockNumber).IsEqualTo(100ul);
    }

    [Test]
    public async Task Unary_Verify_Sets_Root_When_Anchor_Chains()
    {
        var handler = new FakeBlockNodeHandler();
        var (items20, root20) = ChainedBlock(20, BlockDomainTests.RandomHash());
        var (items21, _) = ChainedBlock(21, root20);
        var block20 = new ProtoBlock.Block(); block20.Items.AddRange(items20);
        var block21 = new ProtoBlock.Block(); block21.Items.AddRange(items21);
        handler.Enqueue(GetBlockPath, StatusCode.OK, new BlockResponse { Status = BlockResponse.Types.Code.Success, Block = block20 });
        handler.Enqueue(GetBlockPath, StatusCode.OK, new BlockResponse { Status = BlockResponse.Types.Code.Success, Block = block21 });
        await using var client = new BlockNodeClient(_ => handler.CreateChannel(), ctx => ctx.Uri = new Uri("http://fake-block-node:40840"));
        var block = await client.GetVerifiedBlockAsync(20);
        await Assert.That(block).IsNotNull();
        await Assert.That(block!.VerifiedRootHash.ToArray()).IsEquivalentTo(root20);
    }

    [Test]
    public async Task Unary_Verify_Returns_Null_When_The_Block_Is_Not_Found()
    {
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(GetBlockPath, StatusCode.OK, new BlockResponse { Status = BlockResponse.Types.Code.NotFound });
        await using var client = new BlockNodeClient(_ => handler.CreateChannel(), ctx => ctx.Uri = new Uri("http://fake-block-node:40840"));
        // Absence is data, not a verification failure — and no anchor
        // request should be made for a block that does not exist.
        await Assert.That(await client.GetVerifiedBlockAsync(20)).IsNull();
        await Assert.That(handler.CallCount(GetBlockPath)).IsEqualTo(1);
    }

    [Test]
    public async Task Unary_Verify_Throws_On_Mismatched_Anchor()
    {
        var handler = new FakeBlockNodeHandler();
        var (items20, _) = ChainedBlock(20, BlockDomainTests.RandomHash());
        var (items21, _) = ChainedBlock(21, BlockDomainTests.RandomHash());  // wrong chain
        var block20 = new ProtoBlock.Block(); block20.Items.AddRange(items20);
        var block21 = new ProtoBlock.Block(); block21.Items.AddRange(items21);
        handler.Enqueue(GetBlockPath, StatusCode.OK, new BlockResponse { Status = BlockResponse.Types.Code.Success, Block = block20 });
        handler.Enqueue(GetBlockPath, StatusCode.OK, new BlockResponse { Status = BlockResponse.Types.Code.Success, Block = block21 });
        await using var client = new BlockNodeClient(_ => handler.CreateChannel(), ctx => ctx.Uri = new Uri("http://fake-block-node:40840"));
        var exception = await Assert.ThrowsAsync<BlockNodeException>(async () => await client.GetVerifiedBlockAsync(20));
        await Assert.That(exception!.Code).IsEqualTo(BlockNodeExceptionCode.VerificationFailed);
    }

    [Test]
    public async Task Unary_Verify_Throws_When_Anchor_Unavailable()
    {
        var handler = new FakeBlockNodeHandler();
        var (items20, _) = ChainedBlock(20, BlockDomainTests.RandomHash());
        var block20 = new ProtoBlock.Block(); block20.Items.AddRange(items20);
        handler.Enqueue(GetBlockPath, StatusCode.OK, new BlockResponse { Status = BlockResponse.Types.Code.Success, Block = block20 });
        handler.Enqueue(GetBlockPath, StatusCode.OK, new BlockResponse { Status = BlockResponse.Types.Code.NotAvailable });
        await using var client = new BlockNodeClient(_ => handler.CreateChannel(), ctx => ctx.Uri = new Uri("http://fake-block-node:40840"));
        var exception = await Assert.ThrowsAsync<BlockNodeException>(async () => await client.GetVerifiedBlockAsync(20));
        await Assert.That(exception!.Code).IsEqualTo(BlockNodeExceptionCode.VerificationFailed);
        await Assert.That(exception!.Message).Contains("anchor");
    }

    [Test]
    [Arguments(BlockStreamGapPolicy.Resubscribe)]
    [Arguments(BlockStreamGapPolicy.Deliver)]
    [Arguments(BlockStreamGapPolicy.Throw)]
    public async Task Unverifiable_Block_Faults_Under_Every_Gap_Policy(BlockStreamGapPolicy gapPolicy)
    {
        // Unlike a malformed block, which Deliver mode drops silently,
        // an unverifiable block is never skipped: delivering past it
        // would leave a hole in the chain the caller asked to verify.
        var assembler = new BlockStreamAssembler(BlockVerificationLevel.HashChain, gapPolicy, null);
        assembler.Process(BlockStreamAssemblerTests.Items(UnverifiableBlock(10)));
        var step = assembler.Process(BlockStreamAssemblerTests.End(10));
        await Assert.That(step.Delivered).IsNull();
        await Assert.That(step.Fault).IsNotNull();
        await Assert.That(step.Fault!.Kind).IsEqualTo(BlockStreamFaultKind.Unverifiable);
    }

    [Test]
    public async Task Unverifiable_Block_Is_Delivered_Below_HashChain()
    {
        var assembler = new BlockStreamAssembler(BlockVerificationLevel.Structural, BlockStreamGapPolicy.Resubscribe, null);
        assembler.Process(BlockStreamAssemblerTests.Items(UnverifiableBlock(10)));
        var step = assembler.Process(BlockStreamAssemblerTests.End(10));
        await Assert.That(step.Fault).IsNull();
        await Assert.That(step.Delivered).IsNotNull();
        await Assert.That(step.Delivered!.VerifiedRootHash.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Engine_Throws_Unverifiable_Without_Resubscribing()
    {
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(ServerStatusPath, StatusCode.OK, new ServerStatusResponse { FirstAvailableBlock = 0, LastAvailableBlock = 1000 });
        handler.Enqueue(SubscribePath, StatusCode.OK, [.. ChainedBlockResponses(UnverifiableBlock(10), 10)]);
        await using var client = new BlockNodeClient(_ => handler.CreateChannel(), ctx =>
        {
            ctx.Uri = new Uri("http://fake-block-node:40840");
            ctx.RetryDelay = TimeSpan.FromMilliseconds(1);
        });
        var channel = Channel.CreateUnbounded<Block>();
        var exception = await Assert.ThrowsAsync<BlockNodeException>(async () => await client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
        {
            StartingBlockNumber = 10,
            BlockWriter = channel.Writer,
            VerificationLevel = BlockVerificationLevel.HashChain,
            GapPolicy = BlockStreamGapPolicy.Deliver
        }));
        await Assert.That(exception!.Code).IsEqualTo(BlockNodeExceptionCode.Unverifiable);
        await Assert.That(exception.Message).Contains("FilteredSingleItem");
        await Assert.That(handler.CallCount(SubscribePath)).IsEqualTo(1);
    }

    [Test]
    public async Task Unary_Verify_Throws_Unverifiable_For_Content_It_Cannot_Hash()
    {
        var handler = new FakeBlockNodeHandler();
        var block20 = new ProtoBlock.Block(); block20.Items.AddRange(UnverifiableBlock(20));
        handler.Enqueue(GetBlockPath, StatusCode.OK, new BlockResponse { Status = BlockResponse.Types.Code.Success, Block = block20 });
        await using var client = new BlockNodeClient(_ => handler.CreateChannel(), ctx => ctx.Uri = new Uri("http://fake-block-node:40840"));
        var exception = await Assert.ThrowsAsync<BlockNodeException>(async () => await client.GetVerifiedBlockAsync(20));
        await Assert.That(exception!.Code).IsEqualTo(BlockNodeExceptionCode.Unverifiable);
        // No anchor is fetched for a block that cannot be hashed.
        await Assert.That(handler.CallCount(GetBlockPath)).IsEqualTo(1);
    }

    [Test]
    public async Task Unary_Verify_Accepts_A_Mainnet_Wrapped_Record_Block()
    {
        // A real mainnet block (header HAPI 0.77.5) through the public
        // path, anchored by a synthetic successor that chains from the
        // root mainnet's own next block recorded.
        var directory = Path.Combine(AppContext.BaseDirectory, "BlockNode", "Fixtures");
        var mainnet = ProtoBlock.Block.Parser.ParseFrom(await System.IO.File.ReadAllBytesAsync(Path.Combine(directory, "mainnet-block-100759413.bin")));
        var networkRoot = Convert.FromHexString((await System.IO.File.ReadAllTextAsync(Path.Combine(directory, "mainnet-block-100759413.root.hex"))).Trim());
        var (anchorItems, _) = ChainedBlock(100759414, networkRoot);
        var anchor = new ProtoBlock.Block(); anchor.Items.AddRange(anchorItems);
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(GetBlockPath, StatusCode.OK, new BlockResponse { Status = BlockResponse.Types.Code.Success, Block = mainnet });
        handler.Enqueue(GetBlockPath, StatusCode.OK, new BlockResponse { Status = BlockResponse.Types.Code.Success, Block = anchor });
        await using var client = new BlockNodeClient(_ => handler.CreateChannel(), ctx => ctx.Uri = new Uri("http://fake-block-node:40840"));
        var block = await client.GetVerifiedBlockAsync(100759413);
        await Assert.That(block).IsNotNull();
        await Assert.That(block!.VerifiedRootHash.ToArray()).IsEquivalentTo(networkRoot);
    }
}
