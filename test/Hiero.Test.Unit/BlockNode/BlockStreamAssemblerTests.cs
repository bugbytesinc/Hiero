// SPDX-License-Identifier: Apache-2.0
using Hiero.BlockNode;
using Hiero.Implementation;
using Org.Hiero.Block.Api;
using ProtoBlock = Com.Hedera.Hapi.Block.Stream;

namespace Hiero.Test.Unit.BlockNode;

public class BlockStreamAssemblerTests
{
    internal static SubscribeStreamResponse Items(params ProtoBlock.BlockItem[] items)
    {
        var set = new BlockItemSet();
        set.BlockItems.AddRange(items);
        return new SubscribeStreamResponse { BlockItems = set };
    }

    internal static SubscribeStreamResponse End(ulong number)
    {
        return new SubscribeStreamResponse { EndOfBlock = new BlockEnd { BlockNumber = number } };
    }

    internal static SubscribeStreamResponse Terminal(SubscribeStreamResponse.Types.Code code)
    {
        return new SubscribeStreamResponse { Status = code };
    }

    internal static SubscribeStreamResponse[] WholeBlock(ulong number)
    {
        return
        [
            Items(BlockDomainTests.CreateHeaderItem(number), BlockDomainTests.CreateTransactionItem(BlockDomainTests.RandomHash())),
            Items(BlockDomainTests.CreateFooterItem(BlockDomainTests.RandomHash()), BlockDomainTests.CreateProofItem(number)),
            End(number)
        ];
    }

    private static BlockStreamAssembler CreateAssembler(BlockVerificationLevel level = BlockVerificationLevel.Structural, BlockStreamGapPolicy policy = BlockStreamGapPolicy.Resubscribe, ulong? lastDelivered = null)
    {
        return new BlockStreamAssembler(level, policy, lastDelivered);
    }

    private static List<BlockStreamStep> Feed(BlockStreamAssembler assembler, IEnumerable<SubscribeStreamResponse> responses)
    {
        return [.. responses.Select(assembler.Process)];
    }

    [Test]
    public async Task Assembles_Block_Across_Multiple_Frames()
    {
        var assembler = CreateAssembler();
        var steps = Feed(assembler, WholeBlock(42));
        await Assert.That(steps[0].Delivered).IsNull();
        await Assert.That(steps[1].Delivered).IsNull();
        await Assert.That(steps[2].Delivered).IsNotNull();
        await Assert.That(steps[2].Delivered!.Number).IsEqualTo(42ul);
        await Assert.That(steps[2].Delivered!.Items.Count).IsEqualTo(4);
        await Assert.That(assembler.LastDeliveredBlockNumber).IsEqualTo(42ul);
    }

    [Test]
    public async Task Contiguous_Blocks_Deliver_In_Order()
    {
        var assembler = CreateAssembler();
        var delivered = Feed(assembler, [.. WholeBlock(7), .. WholeBlock(8), .. WholeBlock(9)])
            .Where(s => s.Delivered is not null).Select(s => s.Delivered!.Number).ToArray();
        await Assert.That(delivered).IsEquivalentTo([7ul, 8ul, 9ul]);
    }

    [Test]
    public async Task Resend_Supersedes_Partial_Block()
    {
        var assembler = CreateAssembler();
        var partial = Items(BlockDomainTests.CreateHeaderItem(5), BlockDomainTests.CreateTransactionItem([1]), BlockDomainTests.CreateTransactionItem([2]));
        var steps = Feed(assembler, [partial, .. WholeBlock(5)]);
        var delivered = steps.Where(s => s.Delivered is not null).Select(s => s.Delivered!).ToArray();
        await Assert.That(delivered.Length).IsEqualTo(1);
        await Assert.That(delivered[0].Number).IsEqualTo(5ul);
        // The resent copy (4 items), not the partial (3 items) + resend.
        await Assert.That(delivered[0].Items.Count).IsEqualTo(4);
    }

    [Test]
    public async Task Duplicate_Completed_Block_Is_Dropped()
    {
        var assembler = CreateAssembler();
        var steps = Feed(assembler, [.. WholeBlock(5), .. WholeBlock(5)]);
        var delivered = steps.Where(s => s.Delivered is not null).ToArray();
        await Assert.That(delivered.Length).IsEqualTo(1);
        await Assert.That(steps.All(s => s.Fault is null)).IsTrue();
    }

    [Test]
    public async Task Gap_Produces_Gap_Fault()
    {
        var assembler = CreateAssembler();
        var steps = Feed(assembler, [.. WholeBlock(5), .. WholeBlock(7)]);
        var fault = steps.Single(s => s.Fault is not null).Fault!;
        await Assert.That(fault.Kind).IsEqualTo(BlockStreamFaultKind.Gap);
        await Assert.That(fault.ExpectedBlockNumber).IsEqualTo(6ul);
        await Assert.That(fault.ReceivedBlockNumber).IsEqualTo(7ul);
        await Assert.That(assembler.LastDeliveredBlockNumber).IsEqualTo(5ul);
    }

    [Test]
    public async Task Deliver_Policy_Delivers_Across_Gaps_And_Duplicates()
    {
        var assembler = CreateAssembler(policy: BlockStreamGapPolicy.Deliver);
        var steps = Feed(assembler, [.. WholeBlock(5), .. WholeBlock(7), .. WholeBlock(5)]);
        var delivered = steps.Where(s => s.Delivered is not null).Select(s => s.Delivered!.Number).ToArray();
        await Assert.That(delivered).IsEquivalentTo([5ul, 7ul, 5ul]);
        await Assert.That(steps.All(s => s.Fault is null)).IsTrue();
    }

    [Test]
    public async Task Structural_Rejects_Block_Without_Header()
    {
        var assembler = CreateAssembler();
        var steps = Feed(assembler,
        [
            Items(BlockDomainTests.CreateTransactionItem([1]), BlockDomainTests.CreateFooterItem([2]), BlockDomainTests.CreateProofItem(3)),
            End(3)
        ]);
        var fault = steps[^1].Fault;
        await Assert.That(fault).IsNotNull();
        await Assert.That(fault!.Kind).IsEqualTo(BlockStreamFaultKind.Malformed);
        await Assert.That(fault.Message).Contains("header");
    }

    [Test]
    public async Task Structural_Rejects_Header_Number_Mismatch()
    {
        var assembler = CreateAssembler();
        var block = WholeBlock(10);
        var steps = Feed(assembler, [block[0], block[1], End(11)]);
        var fault = steps[^1].Fault;
        await Assert.That(fault).IsNotNull();
        await Assert.That(fault!.Kind).IsEqualTo(BlockStreamFaultKind.Malformed);
    }

    [Test]
    public async Task Structural_Rejects_Block_Without_Proof()
    {
        var assembler = CreateAssembler();
        var steps = Feed(assembler,
        [
            Items(BlockDomainTests.CreateHeaderItem(4), BlockDomainTests.CreateFooterItem([1])),
            End(4)
        ]);
        await Assert.That(steps[^1].Fault!.Message).Contains("proof");
    }

    [Test]
    public async Task Structural_Rejects_Block_Without_Footer()
    {
        var assembler = CreateAssembler();
        var steps = Feed(assembler,
        [
            Items(BlockDomainTests.CreateHeaderItem(4), BlockDomainTests.CreateProofItem(4)),
            End(4)
        ]);
        await Assert.That(steps[^1].Fault!.Message).Contains("footer");
    }

    [Test]
    public async Task Deliver_Policy_Drops_Malformed_Blocks_Without_Faulting()
    {
        // Under Deliver the caller tolerates missing block numbers, so a
        // block failing structural validation is dropped rather than
        // faulting the stream — the consumer sees the gap instead.
        var assembler = CreateAssembler(policy: BlockStreamGapPolicy.Deliver);
        var steps = Feed(assembler,
        [
            Items(BlockDomainTests.CreateTransactionItem([1]), BlockDomainTests.CreateProofItem(3)),
            End(3),
            .. WholeBlock(4)
        ]);
        await Assert.That(steps.All(s => s.Fault is null)).IsTrue();
        var delivered = steps.Where(s => s.Delivered is not null).Select(s => s.Delivered!.Number).ToArray();
        await Assert.That(delivered).IsEquivalentTo([4ul]);
    }

    [Test]
    public async Task None_Verification_Delivers_Headerless_Block()
    {
        var assembler = CreateAssembler(level: BlockVerificationLevel.None);
        var steps = Feed(assembler,
        [
            Items(BlockDomainTests.CreateTransactionItem([1])),
            End(9)
        ]);
        await Assert.That(steps[^1].Delivered).IsNotNull();
        await Assert.That(steps[^1].Delivered!.Number).IsEqualTo(0ul);
        await Assert.That(assembler.LastDeliveredBlockNumber).IsEqualTo(9ul);
    }

    [Test]
    public async Task Empty_Block_End_Is_Malformed()
    {
        var assembler = CreateAssembler();
        var step = assembler.Process(End(3));
        await Assert.That(step.Fault).IsNotNull();
        await Assert.That(step.Fault!.Kind).IsEqualTo(BlockStreamFaultKind.Malformed);
    }

    [Test]
    public async Task Terminal_Code_Passes_Through()
    {
        var assembler = CreateAssembler();
        var step = assembler.Process(Terminal(SubscribeStreamResponse.Types.Code.Success));
        await Assert.That(step.TerminalCode).IsEqualTo(SubscribeStreamResponse.Types.Code.Success);
    }

    [Test]
    public async Task Resumed_Assembler_Detects_Duplicates_Against_Prior_Progress()
    {
        var assembler = CreateAssembler(lastDelivered: 100);
        var steps = Feed(assembler, [.. WholeBlock(100), .. WholeBlock(101)]);
        var delivered = steps.Where(s => s.Delivered is not null).Select(s => s.Delivered!.Number).ToArray();
        await Assert.That(delivered).IsEquivalentTo([101ul]);
    }
}
