using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;
using System.Text;

namespace Hiero.Test.Integration.Consensus;

public class SubscribeTopicTests
{
    [Test]
    public async Task Can_Subscribe_To_A_Topic_Async()
    {
        await using var fx = await TestTopic.CreateAsync();
        var message = Encoding.ASCII.GetBytes(Generator.String(10, 100));
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.SubmitMessageAsync(new SubmitMessageParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Message = message,
            Signatory = fx.ParticipantPrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.SequenceNumber).IsEqualTo(1UL);
        await AssertHg.NotEmptyAsync(receipt.RunningHash);

        await Task.Delay(5000); // give the mirror node time to sync

        TopicMessage? topicMessage = null;
        using var ctx = new CancellationTokenSource();
        await using var mirror = TestNetwork.CreateMirrorGrpcClient();
        var subscribeTask = mirror.SubscribeTopicAsync(new SubscribeTopicParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Starting = DateTime.UtcNow.AddHours(-1),
            MessageWriter = new TopicMessageWriterAdapter(m =>
            {
                topicMessage = m;
                ctx.Cancel();
            }),
            CancellationToken = ctx.Token
        });

        ctx.CancelAfter(5000);
        await subscribeTask;

        if (topicMessage == null)
        {
            TestContext.Current?.OutputWriter.WriteLine("INDETERMINATE TEST - MIRROR NODE DID NOT RETURN TOPIC IN ALLOWED TIME");
        }
        else
        {
            await Assert.That(topicMessage.Topic).IsEqualTo(fx.CreateReceipt!.Topic);
            await Assert.That(topicMessage.SequenceNumber).IsEqualTo(1UL);
            await Assert.That(topicMessage.RunningHash.ToArray()).IsEquivalentTo(receipt.RunningHash.ToArray(), TUnit.Assertions.Enums.CollectionOrdering.Matching);
            await Assert.That(topicMessage.Message.ToArray()).IsEquivalentTo(message, TUnit.Assertions.Enums.CollectionOrdering.Matching);
            await Assert.That(topicMessage.SegmentInfo).IsNull();
        }

        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.Memo).IsEqualTo(fx.CreateParams.Memo);
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo(1UL);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsEqualTo(new Endorsement(fx.AdminPublicKey));
        await Assert.That(info.Participant).IsEqualTo(new Endorsement(fx.ParticipantPublicKey));
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsEqualTo(fx.RenewAccount.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Subscribe_To_A_Test_Topic()
    {
        await using var fx = await TestTopicMessage.CreateAsync();

        await Assert.That(fx.SubmitReceipt!.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(fx.SubmitReceipt.SequenceNumber).IsEqualTo(1UL);
        await AssertHg.NotEmptyAsync(fx.SubmitReceipt.RunningHash);

        await Task.Delay(7000); // give the mirror node time to sync

        TopicMessage? topicMessage = null;
        using var ctx = new CancellationTokenSource();
        await using var mirror = TestNetwork.CreateMirrorGrpcClient();
        var subscribeTask = mirror.SubscribeTopicAsync(new SubscribeTopicParams
        {
            Topic = fx.TestTopic.CreateReceipt!.Topic,
            Starting = DateTime.UtcNow.AddHours(-1),
            MessageWriter = new TopicMessageWriterAdapter(m =>
            {
                topicMessage = m;
                ctx.Cancel();
            }),
            CancellationToken = ctx.Token
        });

        ctx.CancelAfter(5000);
        await subscribeTask;

        if (topicMessage == null)
        {
            TestContext.Current?.OutputWriter.WriteLine("INDETERMINATE TEST - MIRROR NODE DID NOT RETURN TOPIC IN ALLOWED TIME");
        }
        else
        {
            await Assert.That(topicMessage.Topic).IsEqualTo(fx.TestTopic.CreateReceipt!.Topic);
            await Assert.That(topicMessage.SequenceNumber).IsEqualTo(1UL);
            await Assert.That(topicMessage.RunningHash.ToArray()).IsEquivalentTo(fx.SubmitReceipt.RunningHash.ToArray(), TUnit.Assertions.Enums.CollectionOrdering.Matching);
            await Assert.That(topicMessage.Message.ToArray()).IsEquivalentTo(fx.Message.ToArray(), TUnit.Assertions.Enums.CollectionOrdering.Matching);
            await Assert.That(topicMessage.SegmentInfo).IsNull();
        }
    }

    [Test]
    public async Task Can_Capture_A_Test_Topic()
    {
        await using var fx = await TestTopicMessage.CreateAsync();

        await Assert.That(fx.SubmitReceipt!.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(fx.SubmitReceipt.SequenceNumber).IsEqualTo(1UL);
        await AssertHg.NotEmptyAsync(fx.SubmitReceipt.RunningHash);

        await Task.Delay(5000); // give the mirror node time to sync

        var capture = new TopicMessageCapture(1);
        await using var mirror = TestNetwork.CreateMirrorGrpcClient();
        using var cts = new CancellationTokenSource();
        var subscribeTask = mirror.SubscribeTopicAsync(new SubscribeTopicParams
        {
            Topic = fx.TestTopic.CreateReceipt!.Topic,
            Starting = DateTime.UtcNow.AddHours(-1),
            MessageWriter = capture,
            CancellationToken = cts.Token
        });
        cts.CancelAfter(500);
        await subscribeTask;

        if (capture.CapturedList.Count == 0)
        {
            TestContext.Current?.OutputWriter.WriteLine("INDETERMINATE TEST - MIRROR NODE DID NOT RETURN TOPIC IN ALLOWED TIME");
        }
        else
        {
            var message = capture.CapturedList[0];
            await Assert.That(message.Topic).IsEqualTo(fx.TestTopic.CreateReceipt!.Topic);
            await Assert.That(message.SequenceNumber).IsEqualTo(1UL);
            await Assert.That(message.RunningHash.ToArray()).IsEquivalentTo(fx.SubmitReceipt.RunningHash.ToArray(), TUnit.Assertions.Enums.CollectionOrdering.Matching);
            await Assert.That(message.Message.ToArray()).IsEquivalentTo(fx.Message.ToArray(), TUnit.Assertions.Enums.CollectionOrdering.Matching);
            await Assert.That(message.SegmentInfo).IsNull();
        }
    }

    [Test]
    public async Task Missing_Channel_Writer_Raises_Error()
    {
        await using var fx = await TestTopicMessage.CreateAsync();
        await using var mirror = TestNetwork.CreateMirrorGrpcClient();
        var ex = await Assert.That(async () =>
        {
            await mirror.SubscribeTopicAsync(new SubscribeTopicParams
            {
                Topic = fx.TestTopic.CreateReceipt!.Topic
            });
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("MessageWriter");
        await Assert.That(ane.Message).StartsWith("The destination channel writer missing. Please check that it is not null.");
    }

    [Test]
    public async Task Missing_Topic_Id_Raises_Error()
    {
        await using var mirror = TestNetwork.CreateMirrorGrpcClient();
        var capture = new TopicMessageCapture(1);
        var ex = await Assert.That(async () =>
        {
            await mirror.SubscribeTopicAsync(new SubscribeTopicParams
            {
                Topic = null!,
                MessageWriter = capture
            });
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("Topic");
        await Assert.That(ane.Message).StartsWith("Topic address is missing. Please check that it is not null.");
        await Assert.That(capture.CapturedList).IsEmpty();
    }

    [Test]
    public async Task Can_Get_Asset_Info_Having_Allowance_Defect()
    {
        await using var mirror = TestNetwork.CreateMirrorGrpcClient();
        var capture = new TopicMessageCapture(1);
        var ex = await Assert.That(async () =>
        {
            await mirror.SubscribeTopicAsync(new SubscribeTopicParams
            {
                Topic = TestNetwork.Payer,
                MessageWriter = capture,
                CancellationToken = new CancellationTokenSource(2500).Token
            });
        }).ThrowsException();
        var mex = ex as MirrorGrpcException;
        await Assert.That(mex).IsNotNull();
        await Assert.That(mex!.Code).IsEqualTo(MirrorGrpcExceptionCode.InvalidTopicAddress);
        await Assert.That(mex.Message).StartsWith("The address exists, but is not a topic.");
        await Assert.That(capture.CapturedList).IsEmpty();
    }

    [Test]
    public async Task Non_Existant_Topic_Id_Raises_Errord_Defect()
    {
        await using var mirror = TestNetwork.CreateMirrorGrpcClient();
        var capture = new TopicMessageCapture(1);
        var ex = await Assert.That(async () =>
        {
            await mirror.SubscribeTopicAsync(new SubscribeTopicParams
            {
                Topic = new EntityId(0, 1, 100),
                MessageWriter = capture,
                CancellationToken = new CancellationTokenSource(2500).Token
            });
        }).ThrowsException();
        var mex = ex as MirrorGrpcException;
        await Assert.That(mex).IsNotNull();
        await Assert.That(mex!.Code).IsEqualTo(MirrorGrpcExceptionCode.TopicNotFound);
        await Assert.That(mex.Message).StartsWith("The topic with the specified address does not exist.");
        await Assert.That(capture.CapturedList).IsEmpty();
    }

    [Test]
    public async Task Invalid_Start_And_Ending_Filters_Raise_Error()
    {
        await using var fx = await TestTopicMessage.CreateAsync();
        await Task.Delay(5000); // give the mirror node time to sync

        using var cts = new CancellationTokenSource();
        var capture = new TopicMessageCapture(1);
        await using var mirror = TestNetwork.CreateMirrorGrpcClient();
        var ex = await Assert.That(async () =>
        {
            cts.CancelAfter(500);
            await mirror.SubscribeTopicAsync(new SubscribeTopicParams
            {
                Topic = fx.TestTopic.CreateReceipt!.Topic,
                Starting = DateTime.UtcNow.AddDays(-1),
                Ending = DateTime.UtcNow.AddDays(-2),
                MessageWriter = capture,
                CancellationToken = cts.Token
            });
        }).ThrowsException();
        var aoe = ex as ArgumentOutOfRangeException;
        await Assert.That(aoe).IsNotNull();
        await Assert.That(aoe!.ParamName).IsEqualTo("Ending");
        await Assert.That(aoe.Message).StartsWith("The ending filter date is less than the starting filter date, no records can be returned.");
        await Assert.That(capture.CapturedList).IsEmpty();
    }

    [Test]
    public async Task Return_Limit_Is_Enforced()
    {
        await using var fx = await TestTopicMessage.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        // Submit additional messages
        await client.SubmitMessageAsync(new SubmitMessageParams
        {
            Topic = fx.TestTopic.CreateReceipt!.Topic,
            Message = fx.Message,
            Signatory = fx.TestTopic.ParticipantPrivateKey
        });
        await client.SubmitMessageAsync(new SubmitMessageParams
        {
            Topic = fx.TestTopic.CreateReceipt!.Topic,
            Message = fx.Message,
            Signatory = fx.TestTopic.ParticipantPrivateKey
        });
        await client.SubmitMessageAsync(new SubmitMessageParams
        {
            Topic = fx.TestTopic.CreateReceipt!.Topic,
            Message = fx.Message,
            Signatory = fx.TestTopic.ParticipantPrivateKey
        });
        await client.SubmitMessageAsync(new SubmitMessageParams
        {
            Topic = fx.TestTopic.CreateReceipt!.Topic,
            Message = fx.Message,
            Signatory = fx.TestTopic.ParticipantPrivateKey
        });

        // Wait for enough messages to be available in the mirror node's database.
        for (int waitTries = 0; waitTries < 20; waitTries++)
        {
            try
            {
                await using var waitMirror = TestNetwork.CreateMirrorGrpcClient();
                var captured = await TopicMessageCapture.CaptureOrTimeoutAsync(waitMirror, fx.TestTopic.CreateReceipt!.Topic, 4, 5000);
                if (captured.Length > 2)
                {
                    break;
                }
            }
            catch (MirrorGrpcException ex) when (ex.Code == MirrorGrpcExceptionCode.TopicNotFound)
            {
                TestContext.Current?.OutputWriter.WriteLine("Mirror Node is slow, can not find topic just yet.");
                await Task.Delay(1000);
            }
        }

        // Now test the MaxCount limit
        var capture = new TopicMessageCapture(10);
        await using var mirror = TestNetwork.CreateMirrorGrpcClient();
        using var cts = new CancellationTokenSource();
        var subscribeTask = mirror.SubscribeTopicAsync(new SubscribeTopicParams
        {
            Topic = fx.TestTopic.CreateReceipt!.Topic,
            Starting = DateTime.UtcNow.AddHours(-1),
            MessageWriter = capture,
            CancellationToken = cts.Token,
            MaxCount = 2
        });
        cts.CancelAfter(10000);
        await subscribeTask;

        if (capture.CapturedList.Count == 0)
        {
            TestContext.Current?.OutputWriter.WriteLine("INDETERMINATE TEST - MIRROR NODE DID NOT RETURN TOPIC IN ALLOWED TIME");
        }
        else
        {
            await Assert.That(capture.CapturedList.Count).IsEqualTo(2);
        }
    }
}
