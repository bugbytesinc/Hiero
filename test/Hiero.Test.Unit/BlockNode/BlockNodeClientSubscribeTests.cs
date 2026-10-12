// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Grpc.Core;
using Hiero.BlockNode;
using Org.Hiero.Block.Api;
using System.Threading.Channels;

namespace Hiero.Test.Unit.BlockNode;

public class BlockNodeClientSubscribeTests
{
    private const string ServerStatusPath = "/org.hiero.block.api.BlockNodeService/serverStatus";
    private const string SubscribePath = "/org.hiero.block.api.BlockStreamSubscribeService/subscribeBlockStream";
    private const string ServerStatusDetailPath = "/org.hiero.block.api.BlockNodeService/serverStatusDetail";

    private static BlockNodeClient CreateClient(FakeBlockNodeHandler handler, Action<IBlockNodeContext>? configure = null)
    {
        return new BlockNodeClient(_ => handler.CreateChannel(), ctx =>
        {
            ctx.Uri = new Uri("http://fake-block-node:40840");
            ctx.RetryDelay = TimeSpan.FromMilliseconds(1);
            configure?.Invoke(ctx);
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

    private static async Task<(List<Block> Blocks, Task Subscription)> RunAsync(BlockNodeClient client, SubscribeBlockStreamParams subscribeParams)
    {
        var channel = Channel.CreateUnbounded<Block>();
        subscribeParams.BlockWriter = channel.Writer;
        var subscription = client.SubscribeBlockStreamAsync(subscribeParams);
        var blocks = new List<Block>();
        await foreach (var block in channel.Reader.ReadAllAsync())
        {
            blocks.Add(block);
        }
        return (blocks, subscription);
    }

    [Test]
    public async Task Delivers_Assembled_Blocks_And_Completes_On_Success()
    {
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler);
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(10),
            .. BlockStreamAssemblerTests.WholeBlock(11),
            BlockStreamAssemblerTests.Terminal(SubscribeStreamResponse.Types.Code.Success)
        ]);
        await using var client = CreateClient(handler);
        var (blocks, subscription) = await RunAsync(client, new SubscribeBlockStreamParams
        {
            StartingBlockNumber = 10,
            EndingBlockNumber = 11
        });
        await subscription;
        await Assert.That(blocks.Select(b => b.Number).ToArray()).IsEquivalentTo([10ul, 11ul]);
        await Assert.That(blocks[0].Proof).IsNotNull();
    }

    [Test]
    public async Task MaxCount_Stops_Delivery_Early()
    {
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler);
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(10),
            .. BlockStreamAssemblerTests.WholeBlock(11),
            .. BlockStreamAssemblerTests.WholeBlock(12),
            BlockStreamAssemblerTests.Terminal(SubscribeStreamResponse.Types.Code.Success)
        ]);
        await using var client = CreateClient(handler);
        var (blocks, subscription) = await RunAsync(client, new SubscribeBlockStreamParams
        {
            StartingBlockNumber = 10,
            MaxCount = 1
        });
        await subscription;
        await Assert.That(blocks.Count).IsEqualTo(1);
        await Assert.That(blocks[0].Number).IsEqualTo(10ul);
    }

    [Test]
    public async Task Gap_With_Resubscribe_Recovers_From_Last_Delivered()
    {
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler);
        EnqueueStatus(handler);
        // First subscription: block 10, then a gap (block 12).
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(10),
            .. BlockStreamAssemblerTests.WholeBlock(12)
        ]);
        // Recovery subscription: blocks 11 and 12, then bounded success.
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(11),
            .. BlockStreamAssemblerTests.WholeBlock(12),
            BlockStreamAssemblerTests.Terminal(SubscribeStreamResponse.Types.Code.Success)
        ]);
        await using var client = CreateClient(handler);
        var (blocks, subscription) = await RunAsync(client, new SubscribeBlockStreamParams
        {
            StartingBlockNumber = 10,
            EndingBlockNumber = 12
        });
        await subscription;
        await Assert.That(blocks.Select(b => b.Number).ToArray()).IsEquivalentTo([10ul, 11ul, 12ul]);
        var resumeRequest = SubscribeStreamRequest.Parser.ParseFrom(handler.Requests(SubscribePath)[1]);
        await Assert.That(resumeRequest.StartBlockNumber).IsEqualTo(11ul);
    }

    [Test]
    public async Task Gap_With_Throw_Policy_Throws_StreamGapDetected()
    {
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler);
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(10),
            .. BlockStreamAssemblerTests.WholeBlock(12)
        ]);
        await using var client = CreateClient(handler);
        var channel = Channel.CreateUnbounded<Block>();
        var exception = await Assert.ThrowsAsync<BlockNodeException>(async () => await client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
        {
            StartingBlockNumber = 10,
            BlockWriter = channel.Writer,
            GapPolicy = BlockStreamGapPolicy.Throw
        }));
        await Assert.That(exception!.Code).IsEqualTo(BlockNodeExceptionCode.StreamGapDetected);
        // Block 10 was delivered before the fault; the channel is then completed.
        var delivered = new List<Block>();
        await foreach (var block in channel.Reader.ReadAllAsync())
        {
            delivered.Add(block);
        }
        await Assert.That(delivered.Select(b => b.Number).ToArray()).IsEquivalentTo([10ul]);
    }

    [Test]
    public async Task Deliver_Policy_Delivers_Blocks_With_Gaps()
    {
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler);
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(10),
            .. BlockStreamAssemblerTests.WholeBlock(12),
            BlockStreamAssemblerTests.Terminal(SubscribeStreamResponse.Types.Code.Success)
        ]);
        await using var client = CreateClient(handler);
        var (blocks, subscription) = await RunAsync(client, new SubscribeBlockStreamParams
        {
            StartingBlockNumber = 10,
            GapPolicy = BlockStreamGapPolicy.Deliver
        });
        await subscription;
        await Assert.That(blocks.Select(b => b.Number).ToArray()).IsEquivalentTo([10ul, 12ul]);
    }

    [Test]
    public async Task Invalid_Start_Terminal_Code_Throws()
    {
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler);
        handler.Enqueue(SubscribePath, StatusCode.OK,
            BlockStreamAssemblerTests.Terminal(SubscribeStreamResponse.Types.Code.InvalidStartBlockNumber));
        await using var client = CreateClient(handler);
        var channel = Channel.CreateUnbounded<Block>();
        var exception = await Assert.ThrowsAsync<BlockNodeException>(async () => await client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
        {
            StartingBlockNumber = 10,
            BlockWriter = channel.Writer
        }));
        await Assert.That(exception!.Code).IsEqualTo(BlockNodeExceptionCode.InvalidStartBlockNumber);
    }

    [Test]
    public async Task Pruned_Start_Fails_Fast_With_Range_In_Message()
    {
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler, first: 500, last: 1000);
        await using var client = CreateClient(handler);
        var channel = Channel.CreateUnbounded<Block>();
        var exception = await Assert.ThrowsAsync<BlockNodeException>(async () => await client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
        {
            StartingBlockNumber = 5,
            BlockWriter = channel.Writer
        }));
        await Assert.That(exception!.Code).IsEqualTo(BlockNodeExceptionCode.NotAvailable);
        await Assert.That(exception!.Message).Contains("500");
        await Assert.That(handler.CallCount(SubscribePath)).IsEqualTo(0);
    }

    [Test]
    public async Task Null_Start_Tails_From_Latest_Available()
    {
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler, first: 0, last: 777);
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(777),
            BlockStreamAssemblerTests.Terminal(SubscribeStreamResponse.Types.Code.Success)
        ]);
        await using var client = CreateClient(handler);
        var (blocks, subscription) = await RunAsync(client, new SubscribeBlockStreamParams
        {
            MaxCount = 1
        });
        await subscription;
        await Assert.That(blocks[0].Number).IsEqualTo(777ul);
        var request = SubscribeStreamRequest.Parser.ParseFrom(handler.Requests(SubscribePath)[0]);
        await Assert.That(request.StartBlockNumber).IsEqualTo(777ul);
        await Assert.That(request.EndBlockNumber).IsEqualTo(ulong.MaxValue);
    }

    [Test]
    public async Task Stream_End_Without_Terminal_Resubscribes()
    {
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler);
        EnqueueStatus(handler);
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(10)
        ]);
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(11),
            BlockStreamAssemblerTests.Terminal(SubscribeStreamResponse.Types.Code.Success)
        ]);
        await using var client = CreateClient(handler);
        var (blocks, subscription) = await RunAsync(client, new SubscribeBlockStreamParams
        {
            StartingBlockNumber = 10,
            EndingBlockNumber = 11
        });
        await subscription;
        await Assert.That(blocks.Select(b => b.Number).ToArray()).IsEquivalentTo([10ul, 11ul]);
    }

    [Test]
    public async Task Inverted_Range_Throws()
    {
        var handler = new FakeBlockNodeHandler();
        await using var client = CreateClient(handler);
        var channel = Channel.CreateUnbounded<Block>();
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
        {
            StartingBlockNumber = 10,
            EndingBlockNumber = 5,
            BlockWriter = channel.Writer
        }));
    }

    [Test]
    public async Task Missing_Writer_Throws()
    {
        var handler = new FakeBlockNodeHandler();
        await using var client = CreateClient(handler);
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams()));
    }

    [Test]
    public async Task Cancellation_Completes_Normally()
    {
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler);
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(10)
        ]);
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(11)
        ]);
        using var cts = new CancellationTokenSource();
        await using var client = CreateClient(handler);
        var channel = Channel.CreateUnbounded<Block>();
        var subscription = client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
        {
            StartingBlockNumber = 10,
            BlockWriter = channel.Writer,
            CancellationToken = cts.Token
        });
        await channel.Reader.ReadAsync();
        cts.Cancel();
        await subscription;
        // Drain anything delivered before the cancellation landed; the
        // writer has been completed, so Completion then transitions.
        while (channel.Reader.TryRead(out _)) { }
        await channel.Reader.Completion;
    }

    [Test]
    public async Task Cancellation_While_The_Channel_Is_Full_Completes_Normally()
    {
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler);
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(10),
            .. BlockStreamAssemblerTests.WholeBlock(11),
            BlockStreamAssemblerTests.Terminal(SubscribeStreamResponse.Types.Code.Success)
        ]);
        using var cts = new CancellationTokenSource();
        await using var client = CreateClient(handler);
        // Nobody reads: block 10 fills the channel and block 11 waits for room.
        var channel = Channel.CreateBounded<Block>(1);
        var subscription = client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
        {
            StartingBlockNumber = 10,
            BlockWriter = channel.Writer,
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
        var (blocks, subscription) = await RunAsync(client, new SubscribeBlockStreamParams
        {
            StartingBlockNumber = 10,
            EndingBlockNumber = 11
        });
        await subscription;
        await Assert.That(blocks.Select(b => b.Number).ToArray()).IsEquivalentTo([10ul, 11ul]);
    }

    /// <summary>
    /// A block node publishing each API on its own port, as mainnet's
    /// registered block nodes do: the subscribe port answers the status
    /// API with NotFound.
    /// </summary>
    private static (FakeBlockNodeHandler SubscribeNode, FakeBlockNodeHandler StatusNode, BlockNodeClient Client) SplitPortNode(bool configureStatusUri)
    {
        var subscribeNode = new FakeBlockNodeHandler();
        var statusNode = new FakeBlockNodeHandler();
        subscribeNode.Enqueue(ServerStatusPath, StatusCode.NotFound);
        var client = new BlockNodeClient(uri => uri.Port == 40982 ? statusNode.CreateChannel() : subscribeNode.CreateChannel(), ctx =>
        {
            ctx.Uri = new Uri("http://block-node:40980");
            if (configureStatusUri)
            {
                ctx.StatusUri = new Uri("http://block-node:40982");
            }
            ctx.RetryDelay = TimeSpan.FromMilliseconds(1);
        });
        return (subscribeNode, statusNode, client);
    }

    [Test]
    public async Task StatusUri_Sends_The_Status_Preflight_To_The_Status_Endpoint()
    {
        var (subscribeNode, statusNode, client) = SplitPortNode(configureStatusUri: true);
        await using var _ = client;
        EnqueueStatus(statusNode, first: 0, last: 50);
        subscribeNode.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(50),
            BlockStreamAssemblerTests.Terminal(SubscribeStreamResponse.Types.Code.Success)
        ]);
        var (blocks, subscription) = await RunAsync(client, new SubscribeBlockStreamParams { MaxCount = 1 });
        await subscription;
        await Assert.That(blocks.Single().Number).IsEqualTo(50ul);
        await Assert.That(statusNode.CallCount(ServerStatusPath)).IsEqualTo(1);
        await Assert.That(statusNode.CallCount(SubscribePath)).IsEqualTo(0);
        await Assert.That(subscribeNode.CallCount(ServerStatusPath)).IsEqualTo(0);
        await Assert.That(subscribeNode.CallCount(SubscribePath)).IsEqualTo(1);
    }

    [Test]
    public async Task Split_Port_Node_Without_StatusUri_Says_What_To_Set()
    {
        var (_, statusNode, client) = SplitPortNode(configureStatusUri: false);
        await using var _ = client;
        var channel = Channel.CreateUnbounded<Block>();
        var exception = await Assert.ThrowsAsync<BlockNodeException>(async () => await client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
        {
            StartingBlockNumber = 10,
            BlockWriter = channel.Writer
        }));
        await Assert.That(exception!.Code).IsEqualTo(BlockNodeExceptionCode.NotImplemented);
        await Assert.That(exception.Message).Contains("StatusUri");
        await Assert.That(exception.Message).Contains("http://block-node:40980/");
        await Assert.That(statusNode.CallCount(ServerStatusPath)).IsEqualTo(0);
    }

    [Test]
    public async Task Unbounded_Latest_Available_Block_Tails_From_The_Status_Detail()
    {
        // A newly installed or backfilling node reports uint64_max as its
        // latest block; the real tip is the highest available range end.
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler, first: 0, last: ulong.MaxValue);
        handler.Enqueue(ServerStatusDetailPath, StatusCode.OK, new ServerStatusDetailResponse
        {
            AvailableRanges =
            {
                new Org.Hiero.Block.Api.BlockRange { RangeStart = 0, RangeEnd = 500 },
                new Org.Hiero.Block.Api.BlockRange { RangeStart = 900, RangeEnd = 1200 }
            }
        });
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(1200),
            BlockStreamAssemblerTests.Terminal(SubscribeStreamResponse.Types.Code.Success)
        ]);
        await using var client = CreateClient(handler);
        var (blocks, subscription) = await RunAsync(client, new SubscribeBlockStreamParams { MaxCount = 1 });
        await subscription;
        await Assert.That(blocks.Single().Number).IsEqualTo(1200ul);
        var request = SubscribeStreamRequest.Parser.ParseFrom(handler.Requests(SubscribePath)[0]);
        await Assert.That(request.StartBlockNumber).IsEqualTo(1200ul);
    }

    [Test]
    public async Task Unbounded_Latest_Available_Block_With_No_Ranges_Asks_For_A_Start()
    {
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler, first: 0, last: ulong.MaxValue);
        handler.Enqueue(ServerStatusDetailPath, StatusCode.OK, new ServerStatusDetailResponse());
        await using var client = CreateClient(handler);
        var channel = Channel.CreateUnbounded<Block>();
        var exception = await Assert.ThrowsAsync<BlockNodeException>(async () => await client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
        {
            BlockWriter = channel.Writer
        }));
        await Assert.That(exception!.Code).IsEqualTo(BlockNodeExceptionCode.NotAvailable);
        await Assert.That(handler.CallCount(SubscribePath)).IsEqualTo(0);
    }

    [Test]
    public async Task Unbounded_Latest_Available_Block_Is_Irrelevant_With_An_Explicit_Start()
    {
        var handler = new FakeBlockNodeHandler();
        EnqueueStatus(handler, first: 0, last: ulong.MaxValue);
        handler.Enqueue(SubscribePath, StatusCode.OK,
        [
            .. BlockStreamAssemblerTests.WholeBlock(10),
            BlockStreamAssemblerTests.Terminal(SubscribeStreamResponse.Types.Code.Success)
        ]);
        await using var client = CreateClient(handler);
        var (blocks, subscription) = await RunAsync(client, new SubscribeBlockStreamParams { StartingBlockNumber = 10, MaxCount = 1 });
        await subscription;
        await Assert.That(blocks.Single().Number).IsEqualTo(10ul);
        await Assert.That(handler.CallCount(ServerStatusDetailPath)).IsEqualTo(0);
    }
}
