// SPDX-License-Identifier: Apache-2.0
using Grpc.Net.Client;
using Hiero.BlockNode;
using Hiero.Implementation;
using Org.Hiero.Block.Api;
using ProtoBlock = Com.Hedera.Hapi.Block.Stream;

namespace Hiero.Test.Unit.BlockNode;

public class BlockStreamPublisherTests
{
    /// <summary>
    /// Builds a publisher over the in-memory fake connection with a
    /// throwaway context whose channel factory is never invoked.
    /// </summary>
    private static BlockStreamPublisher CreatePublisher(FakePublishConnection connection)
    {
        var root = new BlockNodeContextStack((Func<Uri, GrpcChannel>)(_ => throw new InvalidOperationException("The channel factory should not be used in unit tests.")));
        var context = new BlockNodeContextStack(root);
        return new BlockStreamPublisher(connection, context);
    }

    private static Block CreateBlock(ulong number)
    {
        return Block.FromProtobuf(BlockDomainTests.CreateBlockProto(number));
    }

    private static PublishStreamResponse Ack(ulong number)
    {
        return new PublishStreamResponse { Acknowledgement = new PublishStreamResponse.Types.BlockAcknowledgement { BlockNumber = number } };
    }

    [Test]
    public async Task WriteBlock_Sends_ItemSet_And_BlockEnd()
    {
        var connection = new FakePublishConnection();
        await using var publisher = CreatePublisher(connection);
        await publisher.WriteBlockAsync(CreateBlock(42));
        var first = await connection.NextRequestAsync();
        await Assert.That(first.RequestCase).IsEqualTo(PublishStreamRequest.RequestOneofCase.BlockItems);
        // header + 2 transactions + footer + proof
        await Assert.That(first.BlockItems.BlockItems.Count).IsEqualTo(5);
        await Assert.That(first.BlockItems.BlockItems[0].ItemCase).IsEqualTo(ProtoBlock.BlockItem.ItemOneofCase.BlockHeader);
        var second = await connection.NextRequestAsync();
        await Assert.That(second.RequestCase).IsEqualTo(PublishStreamRequest.RequestOneofCase.EndOfBlock);
        await Assert.That(second.EndOfBlock.BlockNumber).IsEqualTo(42ul);
    }

    [Test]
    public async Task WriteBlockItems_And_CompleteBlock_Send_Frames()
    {
        var connection = new FakePublishConnection();
        await using var publisher = CreatePublisher(connection);
        var items = CreateBlock(9).Items;
        await publisher.WriteBlockItemsAsync([items[0], items[1]]);
        await publisher.WriteBlockItemsAsync([items[2], items[3]]);
        await publisher.CompleteBlockAsync(9);
        var first = await connection.NextRequestAsync();
        await Assert.That(first.BlockItems.BlockItems.Count).IsEqualTo(2);
        var second = await connection.NextRequestAsync();
        await Assert.That(second.BlockItems.BlockItems.Count).IsEqualTo(2);
        var third = await connection.NextRequestAsync();
        await Assert.That(third.EndOfBlock.BlockNumber).IsEqualTo(9ul);
    }

    [Test]
    public async Task Cumulative_Acknowledgement_Resolves_Waiters_And_Raises_Event()
    {
        var connection = new FakePublishConnection();
        await using var publisher = CreatePublisher(connection);
        await publisher.WriteBlockAsync(CreateBlock(5));
        var wait5 = publisher.WaitForAcknowledgementAsync(5);
        var wait7 = publisher.WaitForAcknowledgementAsync(7);
        await Assert.That(wait5.IsCompleted).IsFalse();
        connection.Send(Ack(7));
        await wait5;
        await wait7;
        await Assert.That(publisher.LastAcknowledgedBlockNumber).IsEqualTo(7ul);
        var evt = await publisher.Events.ReadAsync();
        await Assert.That(evt.Kind).IsEqualTo(PublishStreamEventKind.BlockAcknowledged);
        await Assert.That(evt.BlockNumber).IsEqualTo(7ul);
        // An already-covered wait resolves immediately.
        await publisher.WaitForAcknowledgementAsync(6);
    }

    [Test]
    public async Task Directives_Surface_As_Events_In_Order()
    {
        var connection = new FakePublishConnection();
        await using var publisher = CreatePublisher(connection);
        connection.Send(new PublishStreamResponse { SkipBlock = new PublishStreamResponse.Types.SkipBlock { BlockNumber = 11 } });
        connection.Send(new PublishStreamResponse { ResendBlock = new PublishStreamResponse.Types.ResendBlock { BlockNumber = 12 } });
        connection.Send(new PublishStreamResponse { NodeBehindPublisher = new PublishStreamResponse.Types.BehindPublisher { BlockNumber = 13 } });
        var first = await publisher.Events.ReadAsync();
        var second = await publisher.Events.ReadAsync();
        var third = await publisher.Events.ReadAsync();
        await Assert.That(first.Kind).IsEqualTo(PublishStreamEventKind.SkipBlock);
        await Assert.That(first.BlockNumber).IsEqualTo(11ul);
        await Assert.That(second.Kind).IsEqualTo(PublishStreamEventKind.ResendBlock);
        await Assert.That(third.Kind).IsEqualTo(PublishStreamEventKind.BehindPublisher);
    }

    [Test]
    public async Task Node_EndOfStream_Faults_Writes_And_Waiters()
    {
        var connection = new FakePublishConnection();
        await using var publisher = CreatePublisher(connection);
        await publisher.WriteBlockAsync(CreateBlock(5));
        var wait = publisher.WaitForAcknowledgementAsync(5);
        connection.Send(new PublishStreamResponse
        {
            EndStream = new PublishStreamResponse.Types.EndOfStream
            {
                Status = PublishStreamResponse.Types.EndOfStream.Types.Code.DuplicateBlock,
                BlockNumber = 4
            }
        });
        connection.CloseResponses();
        var ended = await publisher.Events.ReadAsync();
        await Assert.That(ended.Kind).IsEqualTo(PublishStreamEventKind.StreamEnded);
        await Assert.That(ended.EndReason).IsEqualTo(PublishStreamEndReason.DuplicateBlock);
        var waitFault = await Assert.ThrowsAsync<BlockNodeException>(async () => await wait);
        await Assert.That(waitFault!.Code).IsEqualTo(BlockNodeExceptionCode.DuplicateBlock);
        var writeFault = await Assert.ThrowsAsync<BlockNodeException>(async () => await publisher.WriteBlockAsync(CreateBlock(6)));
        await Assert.That(writeFault!.Code).IsEqualTo(BlockNodeExceptionCode.DuplicateBlock);
    }

    [Test]
    public async Task EndStream_Sends_Request_Completes_Events_And_Blocks_Further_Writes()
    {
        var connection = new FakePublishConnection();
        await using var publisher = CreatePublisher(connection);
        await publisher.WriteBlockAsync(CreateBlock(3));
        connection.Send(Ack(3));
        await publisher.WaitForAcknowledgementAsync(3);
        // EndStreamAsync awaits the pump, which only finishes once the node
        // closes its side — so start the end, then simulate the node close.
        var endTask = publisher.EndStreamAsync(PublishStreamEndCode.Reset, 3, 3);
        connection.CloseResponses();
        await endTask;
        await Assert.That(connection.RequestsCompleted).IsTrue();
        var requests = new List<PublishStreamRequest>();
        await foreach (var request in connection.Requests.ReadAllAsync())
        {
            requests.Add(request);
        }
        var end = requests.Single(r => r.RequestCase == PublishStreamRequest.RequestOneofCase.EndStream);
        await Assert.That(end.EndStream.EndCode).IsEqualTo(PublishStreamRequest.Types.EndStream.Types.Code.Reset);
        await Assert.That(end.EndStream.EarliestBlockNumber).IsEqualTo(3ul);
        // Events channel completes when the stream ends.
        var drained = new List<PublishStreamEvent>();
        await foreach (var evt in publisher.Events.ReadAllAsync())
        {
            drained.Add(evt);
        }
        var writeAfterEnd = await Assert.ThrowsAsync<BlockNodeException>(async () => await publisher.WriteBlockAsync(CreateBlock(4)));
        await Assert.That(writeAfterEnd!.Code).IsEqualTo(BlockNodeExceptionCode.InvalidRequest);
    }

    [Test]
    public async Task Waiting_After_The_Stream_Ended_Cleanly_Fails_At_Once()
    {
        var connection = new FakePublishConnection();
        await using var publisher = CreatePublisher(connection);
        await publisher.WriteBlockAsync(CreateBlock(3));
        connection.Send(Ack(3));
        await publisher.WaitForAcknowledgementAsync(3);
        var endTask = publisher.EndStreamAsync(PublishStreamEndCode.Reset, 3, 3);
        connection.CloseResponses();
        await endTask;
        // Block 4 was never acknowledged, and no acknowledgement can arrive now.
        var exception = await Assert.ThrowsAsync<BlockNodeException>(async () => await publisher.WaitForAcknowledgementAsync(4).WaitAsync(TimeSpan.FromSeconds(10)));
        await Assert.That(exception!.Code).IsEqualTo(BlockNodeExceptionCode.CommunicationError);
        // An acknowledged block still returns at once.
        await publisher.WaitForAcknowledgementAsync(3);
    }

    [Test]
    public async Task Cancelling_A_Wait_Does_Not_Disturb_Later_Waits()
    {
        var connection = new FakePublishConnection();
        await using var publisher = CreatePublisher(connection);
        await publisher.WriteBlockAsync(CreateBlock(5));
        using var cts = new CancellationTokenSource();
        var abandoned = publisher.WaitForAcknowledgementAsync(5, cts.Token);
        cts.Cancel();
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await abandoned);
        // The session is unaffected: a fresh wait still resolves off the
        // cumulative acknowledgement.
        var live = publisher.WaitForAcknowledgementAsync(5);
        connection.Send(Ack(5));
        await live;
        await Assert.That(publisher.LastAcknowledgedBlockNumber).IsEqualTo(5ul);
    }

    [Test]
    public async Task EndStream_Honors_Its_Cancellation_Token_When_The_Node_Never_Closes()
    {
        var connection = new FakePublishConnection();
        await using var publisher = CreatePublisher(connection);
        await publisher.WriteBlockAsync(CreateBlock(1));
        // The node acknowledges but never closes its side of the stream;
        // without token support this would hang forever.
        connection.Send(Ack(1));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await publisher.EndStreamAsync(PublishStreamEndCode.Reset, 1, 1, cts.Token));
    }

    [Test]
    public async Task Dispose_Without_End_Is_Clean_And_Idempotent()
    {
        var connection = new FakePublishConnection();
        var publisher = CreatePublisher(connection);
        await publisher.WriteBlockAsync(CreateBlock(1));
        await publisher.DisposeAsync();
        await Assert.That(connection.Disposed).IsTrue();
        // Second dispose is a no-op.
        await publisher.DisposeAsync();
    }
}
