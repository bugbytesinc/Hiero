// SPDX-License-Identifier: Apache-2.0
using Com.Hedera.Mirror.Api.Proto;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using System.IO.Pipelines;
using System.Net;
using System.Threading.Channels;

namespace Hiero.Test.Unit.Consensus;

public class MirrorGrpcSubscriptionTests
{
    private static readonly EntityId Topic = new(0, 0, 1001);

    [Test]
    public async Task A_Bounded_Channel_Receives_Each_Message_Exactly_Once()
    {
        var mirror = new FakeMirrorStream(holdOpen: false, StatusCode.OK, 1, 2, 3);
        await using var client = mirror.CreateClient();
        var channel = Channel.CreateBounded<TopicMessage>(1);
        var subscription = client.SubscribeTopicAsync(new SubscribeTopicParams
        {
            Topic = Topic,
            MessageWriter = channel.Writer
        });
        var received = new List<ulong>();
        await foreach (var message in channel.Reader.ReadAllAsync())
        {
            received.Add(message.SequenceNumber);
            if (received.Count > 3)
            {
                // A message delivered twice; stop rather than read forever.
                break;
            }
            // A slow reader, so the writer has to wait for room.
            await Task.Delay(20);
        }
        await Assert.That(received).IsEquivalentTo([1ul, 2ul, 3ul]);
        await subscription.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task Completing_The_Channel_Ends_The_Subscription()
    {
        var mirror = new FakeMirrorStream(holdOpen: true, StatusCode.OK, 1, 2, 3) { HeldBack = 2 };
        await using var client = mirror.CreateClient();
        var channel = Channel.CreateUnbounded<TopicMessage>();
        var subscription = client.SubscribeTopicAsync(new SubscribeTopicParams
        {
            Topic = Topic,
            MessageWriter = channel.Writer,
            CompleteChannelWhenFinished = false
        });
        await channel.Reader.ReadAsync();
        channel.Writer.TryComplete();
        // A live subscription notices the completed channel when the next
        // message arrives; the mirror then keeps the stream open, and the
        // subscription must still end.
        mirror.ReleaseHeldBack();
        await subscription.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task Cancelling_While_The_Channel_Is_Full_Returns_Normally()
    {
        var mirror = new FakeMirrorStream(holdOpen: true, StatusCode.OK, 1, 2, 3);
        await using var client = mirror.CreateClient();
        using var cts = new CancellationTokenSource();
        var channel = Channel.CreateBounded<TopicMessage>(1);
        var subscription = client.SubscribeTopicAsync(new SubscribeTopicParams
        {
            Topic = Topic,
            MessageWriter = channel.Writer,
            CancellationToken = cts.Token
        });
        // Nobody reads: message 1 fills the channel and message 2 waits for room.
        while (channel.Reader.Count < 1)
        {
            await Task.Delay(5);
        }
        cts.Cancel();
        await subscription.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Test]
    public async Task A_Cancelled_Status_The_Caller_Did_Not_Ask_For_Is_An_Error()
    {
        var mirror = new FakeMirrorStream(holdOpen: false, StatusCode.Cancelled, 1);
        await using var client = mirror.CreateClient();
        var channel = Channel.CreateUnbounded<TopicMessage>();
        var exception = await Assert.ThrowsAsync<MirrorGrpcException>(async () => await client.SubscribeTopicAsync(new SubscribeTopicParams
        {
            Topic = Topic,
            MessageWriter = channel.Writer
        }));
        await Assert.That(exception!.Code).IsEqualTo(MirrorGrpcExceptionCode.CommunicationError);
    }

    [Test]
    public async Task Disposing_The_Client_Twice_Leaves_Its_Clone_Holding_The_Channel()
    {
        var mirror = new FakeMirrorStream(holdOpen: false, StatusCode.OK, 1);
        var tracking = new Core.ClientLifetimeTests.TrackingHandler(mirror);
        var client = new MirrorGrpcClient(_ => GrpcChannel.ForAddress("http://fake-mirror:5600", new GrpcChannelOptions { HttpHandler = tracking, DisposeHttpClient = true }), ctx => ctx.Uri = new Uri("http://fake-mirror:5600"));
        var clone = client.Clone();
        await client.SubscribeTopicAsync(new SubscribeTopicParams { Topic = Topic, MessageWriter = Channel.CreateUnbounded<TopicMessage>().Writer });
        await client.DisposeAsync();
        await client.DisposeAsync();
        await Assert.That(tracking.Disposed).IsFalse();
        await clone.DisposeAsync();
        await Assert.That(tracking.Disposed).IsTrue();
    }

    /// <summary>
    /// Impersonates a mirror node's topic subscription: sends the scripted
    /// messages, then either ends the stream with the given status or holds
    /// it open, as a live subscription does, until the call is cancelled.
    /// </summary>
    private sealed class FakeMirrorStream(bool holdOpen, StatusCode status, params ulong[] sequenceNumbers) : HttpMessageHandler
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// How many of the last scripted messages wait for
        /// <see cref="ReleaseHeldBack"/> before they are sent.
        /// </summary>
        public int HeldBack { get; init; }

        public void ReleaseHeldBack()
        {
            _release.TrySetResult();
        }

        public MirrorGrpcClient CreateClient()
        {
            return new MirrorGrpcClient(_ => GrpcChannel.ForAddress("http://fake-mirror:5600", new GrpcChannelOptions { HttpHandler = this }), ctx =>
            {
                ctx.Uri = new Uri("http://fake-mirror:5600");
            });
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var pipe = new Pipe();
            var sentAtOnce = sequenceNumbers.Length - HeldBack;
            foreach (var sequenceNumber in sequenceNumbers[..sentAtOnce])
            {
                await pipe.Writer.WriteAsync(Frame(sequenceNumber), cancellationToken);
            }
            if (HeldBack > 0)
            {
                _ = Task.Run(async () =>
                {
                    await _release.Task;
                    foreach (var sequenceNumber in sequenceNumbers[sentAtOnce..])
                    {
                        await pipe.Writer.WriteAsync(Frame(sequenceNumber));
                    }
                });
            }
            if (holdOpen)
            {
                cancellationToken.Register(() => pipe.Writer.Complete(new OperationCanceledException(cancellationToken)));
            }
            else
            {
                await pipe.Writer.CompleteAsync();
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Version = HttpVersion.Version20,
                Content = new StreamContent(pipe.Reader.AsStream())
            };
            response.Content.Headers.Add("Content-Type", "application/grpc");
            if (!holdOpen)
            {
                response.TrailingHeaders.Add("grpc-status", ((int)status).ToString());
            }
            return response;
        }

        private static byte[] Frame(ulong sequenceNumber)
        {
            var payload = new ConsensusTopicResponse
            {
                ConsensusTimestamp = new Proto.Timestamp { Seconds = 1_700_000_000, Nanos = (int)sequenceNumber },
                Message = ByteString.CopyFromUtf8($"message {sequenceNumber}"),
                RunningHash = ByteString.CopyFrom(new byte[48]),
                SequenceNumber = sequenceNumber
            }.ToByteArray();
            var frame = new byte[5 + payload.Length];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(1, 4), (uint)payload.Length);
            payload.CopyTo(frame.AsSpan(5));
            return frame;
        }
    }
}
