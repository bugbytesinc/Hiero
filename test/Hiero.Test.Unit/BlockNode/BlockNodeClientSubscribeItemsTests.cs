// SPDX-License-Identifier: Apache-2.0
using Grpc.Core;
using Hiero.BlockNode;
using Org.Hiero.Block.Api;
using System.Threading.Channels;

namespace Hiero.Test.Unit.BlockNode;

public class BlockNodeClientSubscribeItemsTests
{
    private const string ServerStatusPath = "/org.hiero.block.api.BlockNodeService/serverStatus";
    private const string SubscribePath = "/org.hiero.block.api.BlockStreamSubscribeService/subscribeBlockStream";

    private static BlockNodeClient CreateClient(FakeBlockNodeHandler handler)
    {
        return new BlockNodeClient(_ => handler.CreateChannel(), ctx =>
        {
            ctx.Uri = new Uri("http://fake-block-node:40840");
            ctx.RetryDelay = TimeSpan.FromMilliseconds(1);
        });
    }

    private static void EnqueueStatus(FakeBlockNodeHandler handler, ulong first = 0, ulong last = 1000)
    {
        handler.Enqueue(ServerStatusPath, StatusCode.OK, new ServerStatusResponse
        {
            FirstAvailableBlock = first,
            LastAvailableBlock = last
        });
    }

    private static async Task<(List<StreamedBlockItem> Items, Task Subscription)> RunAsync(BlockNodeClient client, SubscribeBlockItemStreamParams subscribeParams)
    {
        var channel = Channel.CreateUnbounded<StreamedBlockItem>();
        subscribeParams.ItemWriter = channel.Writer;
        var subscription = client.SubscribeBlockItemStreamAsync(subscribeParams);
        var items = new List<StreamedBlockItem>();
        await foreach (var item in channel.Reader.ReadAllAsync())
        {
            items.Add(item);
        }
        return (items, subscription);
    }

    [Test]
    public async Task Delivers_Items_Per_Frame_Before_Block_End()
    {
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler);
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            // Header + transaction arrive in one frame, footer + proof in a
            // second, BlockEnd only afterwards — items must carry the block
            // number from the header frame.
            BlockStreamAssemblerTests.Items(BlockDomainTests.CreateHeaderItem(20), BlockDomainTests.CreateTransactionItem([1])),
            BlockStreamAssemblerTests.Items(BlockDomainTests.CreateFooterItem([2]), BlockDomainTests.CreateProofItem(20)),
            BlockStreamAssemblerTests.End(20),
            BlockStreamAssemblerTests.Terminal(SubscribeStreamResponse.Types.Code.Success)
        ]);
        await using var client = CreateClient(handler);
        var (items, subscription) = await RunAsync(client, new SubscribeBlockItemStreamParams
        {
            StartingBlockNumber = 20,
            EndingBlockNumber = 20
        });
        await subscription;
        await Assert.That(items.Count).IsEqualTo(4);
        await Assert.That(items.All(i => i.BlockNumber == 20ul)).IsTrue();
        await Assert.That(items.Select(i => i.Item.Kind).ToArray()).IsEquivalentTo(
            [BlockItemKind.BlockHeader, BlockItemKind.SignedTransaction, BlockItemKind.BlockFooter, BlockItemKind.BlockProof]);
        await Assert.That(items[^1].IsBlockComplete).IsTrue();
        await Assert.That(items.Take(3).All(i => !i.IsBlockComplete)).IsTrue();
    }

    [Test]
    public async Task Resent_Block_Items_Are_Delivered_Again()
    {
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler);
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            // Partial block 30, then a resend of block 30 from the top.
            BlockStreamAssemblerTests.Items(BlockDomainTests.CreateHeaderItem(30), BlockDomainTests.CreateTransactionItem([1])),
            .. BlockStreamAssemblerTests.WholeBlock(30),
            BlockStreamAssemblerTests.Terminal(SubscribeStreamResponse.Types.Code.Success)
        ]);
        await using var client = CreateClient(handler);
        var (items, subscription) = await RunAsync(client, new SubscribeBlockItemStreamParams
        {
            StartingBlockNumber = 30
        });
        await subscription;
        // 2 items from the partial run + 4 from the resend; no dedup.
        await Assert.That(items.Count).IsEqualTo(6);
        await Assert.That(items.Count(i => i.Item.Kind == BlockItemKind.BlockHeader)).IsEqualTo(2);
    }

    [Test]
    public async Task Gap_With_Resubscribe_Resumes_From_Last_Completed()
    {
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler);
        EnqueueStatus(handler);
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(10),
            // Gap: header of block 12 arrives after block 10 completed.
            BlockStreamAssemblerTests.Items(BlockDomainTests.CreateHeaderItem(12))
        ]);
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(11),
            BlockStreamAssemblerTests.Terminal(SubscribeStreamResponse.Types.Code.Success)
        ]);
        await using var client = CreateClient(handler);
        var (items, subscription) = await RunAsync(client, new SubscribeBlockItemStreamParams
        {
            StartingBlockNumber = 10,
            EndingBlockNumber = 11
        });
        await subscription;
        var headers = items.Where(i => i.Item.Kind == BlockItemKind.BlockHeader).Select(i => i.BlockNumber).ToArray();
        await Assert.That(headers).IsEquivalentTo([10ul, 11ul]);
        var resumeRequest = SubscribeStreamRequest.Parser.ParseFrom(handler.Requests(SubscribePath)[1]);
        await Assert.That(resumeRequest.StartBlockNumber).IsEqualTo(11ul);
    }

    [Test]
    public async Task Gap_With_Throw_Policy_Throws()
    {
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler);
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(10),
            BlockStreamAssemblerTests.Items(BlockDomainTests.CreateHeaderItem(12))
        ]);
        await using var client = CreateClient(handler);
        var channel = Channel.CreateUnbounded<StreamedBlockItem>();
        var exception = await Assert.ThrowsAsync<BlockNodeException>(async () => await client.SubscribeBlockItemStreamAsync(new SubscribeBlockItemStreamParams
        {
            StartingBlockNumber = 10,
            ItemWriter = channel.Writer,
            GapPolicy = BlockStreamGapPolicy.Throw
        }));
        await Assert.That(exception!.Code).IsEqualTo(BlockNodeExceptionCode.StreamGapDetected);
    }

    [Test]
    public async Task Cancellation_While_The_Channel_Is_Full_Completes_Normally()
    {
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler);
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(10),
            BlockStreamAssemblerTests.Terminal(SubscribeStreamResponse.Types.Code.Success)
        ]);
        using var cts = new CancellationTokenSource();
        await using var client = CreateClient(handler);
        // Nobody reads: the first item fills the channel and the next waits for room.
        var channel = Channel.CreateBounded<StreamedBlockItem>(1);
        var subscription = client.SubscribeBlockItemStreamAsync(new SubscribeBlockItemStreamParams
        {
            StartingBlockNumber = 10,
            ItemWriter = channel.Writer,
            CancellationToken = cts.Token
        });
        while (channel.Reader.Count < 1)
        {
            await Task.Delay(5);
        }
        cts.Cancel();
        await subscription.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task A_Cancelled_Status_The_Caller_Did_Not_Ask_For_Resubscribes()
    {
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler);
        EnqueueStatus(handler);
        // A server or proxy ends the first stream with CANCELLED.
        handler.Enqueue(SubscribePath, StatusCode.Cancelled,
        [
            .. BlockStreamAssemblerTests.WholeBlock(10)
        ]);
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(11),
            BlockStreamAssemblerTests.Terminal(SubscribeStreamResponse.Types.Code.Success)
        ]);
        await using var client = CreateClient(handler);
        var (items, subscription) = await RunAsync(client, new SubscribeBlockItemStreamParams
        {
            StartingBlockNumber = 10,
            EndingBlockNumber = 11
        });
        await subscription;
        var headers = items.Where(i => i.Item.Kind == BlockItemKind.BlockHeader).Select(i => i.BlockNumber).ToArray();
        await Assert.That(headers).IsEquivalentTo([10ul, 11ul]);
    }

    [Test]
    public async Task Missing_Writer_Throws()
    {
        var handler = new FakeBlockNodeHandler();
        await using var client = CreateClient(handler);
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await client.SubscribeBlockItemStreamAsync(new SubscribeBlockItemStreamParams()));
    }

    [Test]
    public async Task StatusUri_Sends_The_Status_Preflight_To_The_Status_Endpoint()
    {
        // Mainnet's registered block nodes serve each API on its own
        // port; the subscribe port answers the status API with NotFound.
        var subscribeNode = new FakeBlockNodeHandler();
        var statusNode = new FakeBlockNodeHandler();
        subscribeNode.Enqueue(ServerStatusPath, StatusCode.NotFound);
        EnqueueStatus(statusNode, first: 0, last: 20);
        subscribeNode.Enqueue(SubscribePath, StatusCode.OK,
        [
            BlockStreamAssemblerTests.Items(BlockDomainTests.CreateHeaderItem(20), BlockDomainTests.CreateTransactionItem([1])),
            BlockStreamAssemblerTests.End(20),
            BlockStreamAssemblerTests.Terminal(SubscribeStreamResponse.Types.Code.Success)
        ]);
        await using var client = new BlockNodeClient(uri => uri.Port == 40982 ? statusNode.CreateChannel() : subscribeNode.CreateChannel(), ctx =>
        {
            ctx.Uri = new Uri("http://block-node:40980");
            ctx.StatusUri = new Uri("http://block-node:40982");
        });
        var (items, subscription) = await RunAsync(client, new SubscribeBlockItemStreamParams());
        await subscription;
        await Assert.That(items.Count).IsEqualTo(2);
        await Assert.That(statusNode.CallCount(ServerStatusPath)).IsEqualTo(1);
        await Assert.That(subscribeNode.CallCount(ServerStatusPath)).IsEqualTo(0);
        var request = SubscribeStreamRequest.Parser.ParseFrom(subscribeNode.Requests(SubscribePath)[0]);
        await Assert.That(request.StartBlockNumber).IsEqualTo(20ul);
    }
}
