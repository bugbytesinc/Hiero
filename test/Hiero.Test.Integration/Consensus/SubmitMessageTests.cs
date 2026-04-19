using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;
using System.Text;

namespace Hiero.Test.Integration.Consensus;

public class SubmitMessageTests
{
    [Test]
    public async Task Can_Submit_Message()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var message = Encoding.ASCII.GetBytes(Generator.String(10, 100));

        var receipt = await client.SubmitMessageAsync(new SubmitMessageParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Message = message,
            Signatory = fx.ParticipantPrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.SequenceNumber).IsEqualTo(1ul);
        await Assert.That(receipt.RunningHash.IsEmpty).IsFalse();
        await Assert.That(receipt.RunningHashVersion).IsEqualTo(3ul);

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
    public async Task Can_Submit_Message_To_Open_Topic()
    {
        await using var fx = await TestTopic.CreateAsync(fx =>
        {
            fx.CreateParams.Submitter = null;
        });
        await using var client = await TestNetwork.CreateClientAsync();
        var message = Encoding.ASCII.GetBytes(Generator.Memo(20, 100));

        var receipt = await client.SubmitMessageAsync(fx.CreateReceipt!.Topic, message);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.SequenceNumber).IsEqualTo(1ul);
        await Assert.That(receipt.RunningHash.IsEmpty).IsFalse();
        await Assert.That(receipt.RunningHashVersion).IsEqualTo(3ul);

        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.Memo).IsEqualTo(fx.CreateParams.Memo);
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo(1UL);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsEqualTo(new Endorsement(fx.AdminPublicKey));
        await Assert.That(info.Participant).IsNull();
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsEqualTo(fx.RenewAccount.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Submit_Message_Without_Key_Raises_Error()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var message = Encoding.ASCII.GetBytes(Generator.Memo(20, 100));

        var ex = await Assert.That(async () =>
        {
            await client.SubmitMessageAsync(fx.CreateReceipt!.Topic, message);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Submit Message failed with status: InvalidSignature");
    }

    [Test]
    public async Task Submit_Message_Without_Topic_Raises_Error()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var message = Encoding.ASCII.GetBytes(Generator.Memo(20, 100));

        var ex = await Assert.That(async () =>
        {
            await client.SubmitMessageAsync(null!, message);
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("topic");
        await Assert.That(ane.Message).StartsWith("Topic Address is missing. Please check that it is not null.");
    }

    [Test]
    public async Task Submit_Message_With_Invalid_Topic_Raises_Error()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var message = Encoding.ASCII.GetBytes(Generator.Memo(20, 100));

        var ex = await Assert.That(async () =>
        {
            await client.SubmitMessageAsync(EntityId.None, message);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidTopicId);
        await Assert.That(tex.Message).StartsWith("Submit Message failed with status: InvalidTopicId");
    }

    [Test]
    public async Task Submit_Message_Without_Message_Raises_Error()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.SubmitMessageAsync(new SubmitMessageParams
            {
                Topic = fx.CreateReceipt!.Topic,
                Message = null,
                Signatory = fx.ParticipantPrivateKey
            });
        }).ThrowsException();
        var aore = ex as ArgumentOutOfRangeException;
        await Assert.That(aore).IsNotNull();
        await Assert.That(aore!.ParamName).IsEqualTo("Message");
        await Assert.That(aore.Message).StartsWith("Topic Message can not be empty.");
    }

    [Test]
    public async Task Submit_Message_To_Deleted_Topic_Raises_Error()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var deleteReceipt = await client.DeleteTopicAsync(new DeleteTopicParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Signatory = fx.AdminPrivateKey
        });
        await Assert.That(deleteReceipt.Status).IsEqualTo(ResponseCode.Success);

        var message = Encoding.ASCII.GetBytes(Generator.Memo(10, 100));

        var ex = await Assert.That(async () =>
        {
            await client.SubmitMessageAsync(fx.CreateReceipt!.Topic, message);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidTopicId);
        await Assert.That(tex.Message).StartsWith("Submit Message failed with status: InvalidTopicId");
    }

    [Test]
    public async Task Can_Increment_Sequence_Number()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var expectedSequenceNumber = Generator.Integer(10, 20);

        for (int i = 0; i < expectedSequenceNumber; i++)
        {
            var message = Encoding.ASCII.GetBytes(Generator.String(10, 100));
            var receipt = await client.SubmitMessageAsync(new SubmitMessageParams
            {
                Topic = fx.CreateReceipt!.Topic,
                Message = message,
                Signatory = fx.ParticipantPrivateKey
            });
            await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
            await Assert.That(receipt.SequenceNumber).IsEqualTo((ulong)i + 1);
            await Assert.That(receipt.RunningHash.IsEmpty).IsFalse();
            await Assert.That(receipt.RunningHashVersion).IsEqualTo(3ul);
        }

        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.SequenceNumber).IsEqualTo((ulong)expectedSequenceNumber);
    }

    [Test]
    public async Task Can_Call_With_Record()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var expectedSequenceNumber = Generator.Integer(10, 20);
        SubmitMessageRecord? record = null;

        for (int i = 0; i < expectedSequenceNumber; i++)
        {
            var message = Encoding.ASCII.GetBytes(Generator.String(10, 100));
            var receipt = await client.SubmitMessageAsync(new SubmitMessageParams
            {
                Topic = fx.CreateReceipt!.Topic,
                Message = message,
                Signatory = fx.ParticipantPrivateKey
            });
            record = await client.GetTransactionRecordAsync(receipt.TransactionId) as SubmitMessageRecord;
            await Assert.That(record).IsNotNull();
            await Assert.That(record!.Status).IsEqualTo(ResponseCode.Success);
            await Assert.That(record.SequenceNumber).IsEqualTo((ulong)i + 1);
            await Assert.That(record.RunningHash.IsEmpty).IsFalse();
            await Assert.That(record.Hash.IsEmpty).IsFalse();
            await Assert.That(record.Consensus).IsNotNull();
            await Assert.That(record.Memo).IsEmpty();
            await Assert.That(record.Fee >= 0UL).IsTrue();
            await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        }

        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.SequenceNumber).IsEqualTo((ulong)expectedSequenceNumber);
        await Assert.That(record!.SequenceNumber).IsEqualTo((ulong)expectedSequenceNumber);
        await Assert.That(record.RunningHash.ToArray()).IsEquivalentTo(info.RunningHash.ToArray(), TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Call_With_Record_Payer_Signatory()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var expectedSequenceNumber = Generator.Integer(10, 20);
        SubmitMessageRecord? record = null;

        for (int i = 0; i < expectedSequenceNumber; i++)
        {
            var message = Encoding.ASCII.GetBytes(Generator.String(10, 100));
            var receipt = await client.SubmitMessageAsync(new SubmitMessageParams
            {
                Topic = fx.CreateReceipt!.Topic,
                Message = message,
                Signatory = fx.ParticipantPrivateKey
            });
            record = await client.GetTransactionRecordAsync(receipt.TransactionId) as SubmitMessageRecord;
            await Assert.That(record).IsNotNull();
            await Assert.That(record!.Status).IsEqualTo(ResponseCode.Success);
            await Assert.That(record.SequenceNumber).IsEqualTo((ulong)i + 1);
            await Assert.That(record.RunningHash.IsEmpty).IsFalse();
            await Assert.That(record.Hash.IsEmpty).IsFalse();
            await Assert.That(record.Consensus).IsNotNull();
            await Assert.That(record.Memo).IsEmpty();
            await Assert.That(record.Fee >= 0UL).IsTrue();
            await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        }

        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.SequenceNumber).IsEqualTo((ulong)expectedSequenceNumber);
        await Assert.That(record!.SequenceNumber).IsEqualTo((ulong)expectedSequenceNumber);
        await Assert.That(record.RunningHash.ToArray()).IsEquivalentTo(info.RunningHash.ToArray(), TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Schedule_Submit_Message()
    {
        await using var fxTopic = await TestTopic.CreateAsync();
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var baseClient = await TestNetwork.CreateClientAsync();
        await using var client = baseClient.Clone(ctx => ctx.FeeLimit = 5_00_000_000);

        var message = Encoding.ASCII.GetBytes(Generator.String(10, 100));
        var schedulingReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new SubmitMessageParams
            {
                Topic = fxTopic.CreateReceipt!.Topic,
                Message = message,
                Signatory = fxTopic.ParticipantPrivateKey,
            },
            Payer = fxPayer.CreateReceipt!.Address,
        });
        await Assert.That(schedulingReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var payerClient = baseClient.Clone(ctx =>
        {
            ctx.Payer = fxPayer.CreateReceipt!.Address;
            ctx.Signatory = fxPayer.PrivateKey;
        });
        var counterReceipt = await payerClient.SignScheduleAsync(schedulingReceipt.Schedule);

        var noThrowClient = baseClient.Clone(ctx => ctx.ThrowIfNotSuccess = false);
        var pendingReceipt = await noThrowClient.GetReceiptAsync(schedulingReceipt.ScheduledTransactionId);
        await Assert.That(pendingReceipt.Status).IsEqualTo(ResponseCode.Success);

        var messageReceipt = pendingReceipt as SubmitMessageReceipt;
        await Assert.That(messageReceipt).IsNotNull();
        await Assert.That(messageReceipt!.SequenceNumber).IsEqualTo(1ul);
        await Assert.That(messageReceipt.RunningHash.IsEmpty).IsFalse();
        await Assert.That(messageReceipt.RunningHashVersion).IsEqualTo(3ul);

        var info = await baseClient.GetTopicInfoAsync(fxTopic.CreateReceipt!.Topic);
        await Assert.That(info.Memo).IsEqualTo(fxTopic.CreateParams.Memo);
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo(1UL);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsEqualTo(new Endorsement(fxTopic.AdminPublicKey));
        await Assert.That(info.Participant).IsEqualTo(new Endorsement(fxTopic.ParticipantPublicKey));
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsEqualTo(fxTopic.RenewAccount.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Submit_A_Birst_Of_Messages()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 200_00_000_000);
        await using var baseClient = await TestNetwork.CreateClientAsync();
        await using var client = baseClient.Clone(ctx => { ctx.Payer = fxPayer.CreateReceipt!.Address; ctx.Signatory = fxPayer.PrivateKey; });
        await using var fx = await TestTopic.CreateAsync();
        var message = Encoding.ASCII.GetBytes(Generator.String(10, 100));
        var tasks = Enumerable.Range(1, 4000).Select(_ => Task.Run(async () =>
        {
            try
            {
                return await client.SubmitMessageAsync(new SubmitMessageParams
                {
                    Topic = fx.CreateReceipt!.Topic,
                    Message = message,
                    Signatory = fx.ParticipantPrivateKey
                });
            }
            catch (PrecheckException pex) when (pex.Status == ResponseCode.DuplicateTransaction)
            {
                return await client.GetReceiptAsync(pex.TransactionId) as SubmitMessageReceipt;
            }
            catch (PrecheckException)
            {
                return null;
            }
        }));
        var receipts = (await Task.WhenAll(tasks)).Where(r => r != null).ToArray();
        foreach (var receipt in receipts)
        {
            await Assert.That(receipt).IsNotNull();
            await Assert.That(receipt!.Status).IsEqualTo(ResponseCode.Success);
        }

        var info = await baseClient.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.SequenceNumber).IsEqualTo((ulong)receipts.Length);
    }

    [Test]
    public async Task Can_Submit_Multiple_Messages_In_Atomic_Batch()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var baseClient = await TestNetwork.CreateClientAsync();
        await using var client = baseClient.Clone(ctx => { ctx.Payer = fxPayer.CreateReceipt!.Address; ctx.Signatory = fxPayer.PrivateKey; });
        await using var fx = await TestTopic.CreateAsync();
        var messageParams = Enumerable.Range(1, 10).Select(_ => new SubmitMessageParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Message = Encoding.ASCII.GetBytes(Generator.String(10, 100)),
            Signatory = fx.ParticipantPrivateKey
        }).ToArray();
        var receipt = await client.ExecuteAsync(new BatchedTransactionParams { TransactionParams = messageParams });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Execute_Send_Message_Generically()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var baseClient = await TestNetwork.CreateClientAsync();
        await using var client = baseClient.Clone(ctx => { ctx.Payer = fxPayer.CreateReceipt!.Address; ctx.Signatory = fxPayer.PrivateKey; });
        await using var fx = await TestTopic.CreateAsync();
        var submitParams = new SubmitMessageParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Message = Encoding.ASCII.GetBytes(Generator.String(10, 100)),
            Signatory = fx.ParticipantPrivateKey
        };
        var receipt = await client.ExecuteAsync(submitParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Submit_Send_Message_Generically()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var baseClient = await TestNetwork.CreateClientAsync();
        await using var client = baseClient.Clone(ctx => { ctx.Payer = fxPayer.CreateReceipt!.Address; ctx.Signatory = fxPayer.PrivateKey; });
        await using var fx = await TestTopic.CreateAsync();
        var submitParams = new SubmitMessageParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Message = Encoding.ASCII.GetBytes(Generator.String(10, 100)),
            Signatory = fx.ParticipantPrivateKey
        };
        var code = await client.SubmitAsync(submitParams);
        await Assert.That(code).IsEqualTo(ResponseCode.Ok);
        // Send a second time, we need to wait for consensus before we can check for sequence numbers.
        var receipt = await client.ExecuteAsync(submitParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        var info = await baseClient.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.SequenceNumber).IsEqualTo(2UL);
    }

    [Test]
    public async Task Can_Submit_Multiple_Messages_In_Atomic_Batch_Generically()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var baseClient = await TestNetwork.CreateClientAsync();
        await using var client = baseClient.Clone(ctx => { ctx.Payer = fxPayer.CreateReceipt!.Address; ctx.Signatory = fxPayer.PrivateKey; });
        await using var fx = await TestTopic.CreateAsync();
        var messageParams = Enumerable.Range(1, 10).Select(_ => new SubmitMessageParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Message = Encoding.ASCII.GetBytes(Generator.String(10, 100)),
            Signatory = fx.ParticipantPrivateKey
        }).ToArray();
        var receipt = await client.ExecuteAsync(new BatchedTransactionParams { TransactionParams = messageParams });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Send_Multiple_Messages_In_Atomic_Batch_Generically()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var baseClient = await TestNetwork.CreateClientAsync();
        await using var client = baseClient.Clone(ctx => { ctx.Payer = fxPayer.CreateReceipt!.Address; ctx.Signatory = fxPayer.PrivateKey; });
        await using var fx = await TestTopic.CreateAsync();
        var messageParams = Enumerable.Range(1, 10).Select(_ => new SubmitMessageParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Message = Encoding.ASCII.GetBytes(Generator.String(10, 100)),
            Signatory = fx.ParticipantPrivateKey
        }).ToArray();
        var code = await client.SubmitAsync(new BatchedTransactionParams { TransactionParams = messageParams });
        await Assert.That(code).IsEqualTo(ResponseCode.Ok);
        // Send a second time, we need to wait for consensus before we can check for sequence numbers.
        var receipt = await client.ExecuteAsync(messageParams[0]);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        var info = await baseClient.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.SequenceNumber).IsEqualTo(11UL);
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Submit_Message()
    {
        await using var fxTopic = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var schedulingReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new SubmitMessageParams
            {
                Topic = fxTopic.CreateReceipt!.Topic,
                Message = Encoding.ASCII.GetBytes("test message"),
            },
        });
        await Assert.That(schedulingReceipt.Schedule).IsNotEqualTo(EntityId.None);
        var signReceipt = await client.SignScheduleAsync(new SignScheduleParams
        {
            Schedule = schedulingReceipt.Schedule,
            Signatory = fxTopic.ParticipantPrivateKey
        });
        await Assert.That(signReceipt.Status).IsEqualTo(ResponseCode.Success);
    }
}
