using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;
using System.Text;

namespace Hiero.Test.Integration.Consensus;

public class SubmitSegmentedMessageTests
{
    [Test]
    public async Task Can_Submit_Single_Segmented_Message()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var submitParams = new SubmitMessageParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Message = Encoding.ASCII.GetBytes(Generator.String(120, 199)),
            SegmentIndex = 1,
            TotalSegmentCount = 1,
            Signatory = fx.ParticipantPrivateKey
        };
        var receipt = await client.SubmitMessageAsync(submitParams);
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
    public async Task Can_Submit_Two_Segmented_Message()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var submitParams = new SubmitMessageParams[2];
        var receipts = new SubmitMessageReceipt[2];
        submitParams[0] = new SubmitMessageParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Message = Encoding.ASCII.GetBytes(Generator.String(120, 199)),
            SegmentIndex = 1,
            TotalSegmentCount = 2,
            Signatory = fx.ParticipantPrivateKey
        };
        receipts[0] = await client.SubmitMessageAsync(submitParams[0]);
        await Assert.That(receipts[0].Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipts[0].SequenceNumber).IsEqualTo(1ul);
        await Assert.That(receipts[0].RunningHash.IsEmpty).IsFalse();
        await Assert.That(receipts[0].RunningHashVersion).IsEqualTo(3ul);
        var txId = receipts[0].TransactionId;

        submitParams[1] = new SubmitMessageParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Message = Encoding.ASCII.GetBytes(Generator.String(120, 199)),
            ParentTransactionId = txId,
            SegmentIndex = 2,
            TotalSegmentCount = 2,
            Signatory = fx.ParticipantPrivateKey
        };
        receipts[1] = await client.SubmitMessageAsync(submitParams[1]);
        await Assert.That(receipts[1].Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipts[1].SequenceNumber).IsEqualTo(2ul);
        await Assert.That(receipts[1].RunningHash.IsEmpty).IsFalse();
        await Assert.That(receipts[1].RunningHashVersion).IsEqualTo(3ul);

        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.Memo).IsEqualTo(fx.CreateParams.Memo);
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo(2UL);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsEqualTo(new Endorsement(fx.AdminPublicKey));
        await Assert.That(info.Participant).IsEqualTo(new Endorsement(fx.ParticipantPublicKey));
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsEqualTo(fx.RenewAccount.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Submit_Bogus_Segmented_Message_Metadata()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var parentTx = client.CreateNewTransactionId();
        var submitParams = new SubmitMessageParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Message = Encoding.ASCII.GetBytes(Generator.String(120, 199)),
            ParentTransactionId = parentTx,
            SegmentIndex = 100,
            TotalSegmentCount = 200,
            Signatory = fx.ParticipantPrivateKey
        };
        var receipt = await client.SubmitMessageAsync(submitParams);
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
        var submitParams = new SubmitMessageParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Message = Encoding.ASCII.GetBytes(Generator.String(120, 199)),
            SegmentIndex = 1,
            TotalSegmentCount = 1,
            Signatory = fx.ParticipantPrivateKey
        };
        var receipt = await client.SubmitMessageAsync(submitParams);
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
    public async Task Submit_Message_Without_Topic_Raises_Error()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            var submitParams = new SubmitMessageParams
            {
                Topic = null!,
                Message = Encoding.ASCII.GetBytes(Generator.String(120, 199)),
                SegmentIndex = 1,
                TotalSegmentCount = 1,
                Signatory = null
            };
            await client.SubmitMessageAsync(submitParams);
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

        var ex = await Assert.That(async () =>
        {
            var txId = client.CreateNewTransactionId();
            var submitParams = new SubmitMessageParams
            {
                Topic = EntityId.None,
                Message = Encoding.ASCII.GetBytes(Generator.String(120, 199)),
                ParentTransactionId = txId,
                SegmentIndex = 1,
                TotalSegmentCount = 1,
                Signatory = null
            };
            await client.SubmitMessageAsync(submitParams, ctx => ctx.TransactionId = txId);
        }).ThrowsException();
        var aore = ex as ArgumentOutOfRangeException;
        await Assert.That(aore).IsNotNull();
        await Assert.That(aore!.ParamName).IsEqualTo("ParentTransactionId");
        await Assert.That(aore.Message).StartsWith("The Parent Transaction cannot be specified (must be null) when the segment index is one. (Parameter 'ParentTransactionId')");
    }

    [Test]
    public async Task Submit_Message_Without_Message_Raises_Error()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            var txId = client.CreateNewTransactionId();
            var submitParams = new SubmitMessageParams
            {
                Topic = fx.CreateReceipt!.Topic,
                Message = null,
                ParentTransactionId = txId,
                SegmentIndex = 2,
                TotalSegmentCount = 2,
                Signatory = null
            };
            await client.SubmitMessageAsync(submitParams, ctx => ctx.TransactionId = txId);
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

        var ex = await Assert.That(async () =>
        {
            var submitParams = new SubmitMessageParams
            {
                Topic = fx.CreateReceipt!.Topic,
                Message = Encoding.ASCII.GetBytes(Generator.String(120, 199)),
                SegmentIndex = 1,
                TotalSegmentCount = 1,
                Signatory = fx.ParticipantPrivateKey
            };
            await client.SubmitMessageAsync(submitParams);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidTopicId);
        await Assert.That(tex.Message).StartsWith("Submit Message failed with status: InvalidTopicId");
    }

    [Test]
    public async Task Can_Call_Get_Record()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var expectedSequenceNumber = Generator.Integer(10, 20);
        for (int i = 0; i < expectedSequenceNumber; i++)
        {
            var submitParams = new SubmitMessageParams
            {
                Topic = fx.CreateReceipt!.Topic,
                Message = Encoding.ASCII.GetBytes(Generator.String(120, 199)),
                SegmentIndex = 1,
                TotalSegmentCount = 1,
                Signatory = fx.ParticipantPrivateKey
            };

            var receipt = await client.SubmitMessageAsync(submitParams);
            await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
            await Assert.That(receipt.SequenceNumber).IsEqualTo((ulong)(i + 1));
            await Assert.That(receipt.RunningHash.IsEmpty).IsFalse();
            await Assert.That(receipt.RunningHashVersion).IsEqualTo(3ul);
            await Assert.That(receipt.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

            var genericRecord = await client.GetTransactionRecordAsync(receipt.TransactionId);
            var messageRecord = genericRecord as SubmitMessageRecord;
            await Assert.That(messageRecord).IsNotNull();
            await Assert.That(messageRecord!.Status).IsEqualTo(ResponseCode.Success);
            await Assert.That(messageRecord.SequenceNumber).IsEqualTo((ulong)(i + 1));
            await Assert.That(messageRecord.RunningHashVersion).IsEqualTo(3ul);
            await Assert.That(messageRecord.TransactionId).IsEqualTo(receipt.TransactionId);
            await Assert.That(messageRecord.RunningHash.ToArray()).IsEquivalentTo(receipt.RunningHash.ToArray(), TUnit.Assertions.Enums.CollectionOrdering.Matching);
            await Assert.That(messageRecord.Hash.IsEmpty).IsFalse();
            await Assert.That(messageRecord.Consensus).IsNotNull();
            await Assert.That(messageRecord.Memo).IsEmpty();
            await Assert.That(messageRecord.Fee >= 0UL).IsTrue();
        }
        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.SequenceNumber).IsEqualTo((ulong)expectedSequenceNumber);
    }

    [Test]
    public async Task Parent_Transaction_Is_Enforced_For_First_Segment()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var message = Encoding.ASCII.GetBytes(Generator.String(10, 100));

        var ex = await Assert.That(async () =>
        {
            var submitParams = new SubmitMessageParams
            {
                Topic = fx.CreateReceipt!.Topic,
                Message = message,
                ParentTransactionId = client.CreateNewTransactionId(),
                SegmentIndex = 1,
                TotalSegmentCount = 2,
                Signatory = fx.ParticipantPrivateKey
            };
            await client.SubmitMessageAsync(submitParams);
        }).ThrowsException();
        var aore = ex as ArgumentOutOfRangeException;
        await Assert.That(aore).IsNotNull();
        await Assert.That(aore!.Message).StartsWith("The Parent Transaction cannot be specified (must be null) when the segment index is one. (Parameter 'ParentTransactionId')");
    }

    [Test]
    public async Task Parent_Transaction_Is_N_Ot_Enforced_For_Second_Segment()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var message = Encoding.ASCII.GetBytes(Generator.String(10, 100));
        var receipt1 = await client.SubmitMessageAsync(new SubmitMessageParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Message = message,
            SegmentIndex = 1,
            TotalSegmentCount = 2,
            Signatory = fx.ParticipantPrivateKey
        });
        await Assert.That(receipt1.Status).IsEqualTo(ResponseCode.Success);

        var receipt2 = await client.SubmitMessageAsync(new SubmitMessageParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Message = message,
            ParentTransactionId = client.CreateNewTransactionId(),
            SegmentIndex = 2,
            TotalSegmentCount = 2,
            Signatory = fx.ParticipantPrivateKey
        });
        await Assert.That(receipt2.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Submit_Message_With_Negative_Sgment_Index_Raises_Error()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var message = Encoding.ASCII.GetBytes(Generator.String(10, 100));

        var ex = await Assert.That(async () =>
        {
            var txId = client.CreateNewTransactionId();
            var submitParams = new SubmitMessageParams
            {
                Topic = fx.CreateReceipt!.Topic,
                Message = message,
                ParentTransactionId = txId,
                SegmentIndex = -5,
                TotalSegmentCount = 1,
                Signatory = null
            };
            await client.SubmitMessageAsync(submitParams);
        }).ThrowsException();
        var aore = ex as ArgumentOutOfRangeException;
        await Assert.That(aore).IsNotNull();
        await Assert.That(aore!.ParamName).IsEqualTo("SegmentIndex");
        await Assert.That(aore.Message).StartsWith("Segment index must be between one and the total segment count inclusively.");
    }

    [Test]
    public async Task Submit_Message_Index_Too_Large_Raises_Error()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var message = Encoding.ASCII.GetBytes(Generator.String(10, 100));

        var ex = await Assert.That(async () =>
        {
            var txId = client.CreateNewTransactionId();
            var submitParams = new SubmitMessageParams
            {
                Topic = fx.CreateReceipt!.Topic,
                Message = message,
                ParentTransactionId = txId,
                SegmentIndex = 5,
                TotalSegmentCount = 2,
                Signatory = null
            };
            await client.SubmitMessageAsync(submitParams);
        }).ThrowsException();
        var aore = ex as ArgumentOutOfRangeException;
        await Assert.That(aore).IsNotNull();
        await Assert.That(aore!.ParamName).IsEqualTo("SegmentIndex");
        await Assert.That(aore.Message).StartsWith("Segment index must be between one and the total segment count inclusively.");
    }

    [Test]
    public async Task Submit_Message_Negative_Total_Segment_Count_Raises_Error()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var message = Encoding.ASCII.GetBytes(Generator.String(10, 100));

        var ex = await Assert.That(async () =>
        {
            var txId = client.CreateNewTransactionId();
            var submitParams = new SubmitMessageParams
            {
                Topic = fx.CreateReceipt!.Topic,
                Message = message,
                ParentTransactionId = txId,
                SegmentIndex = 2,
                TotalSegmentCount = -2,
                Signatory = null
            };
            await client.SubmitMessageAsync(submitParams);
        }).ThrowsException();
        var aore = ex as ArgumentOutOfRangeException;
        await Assert.That(aore).IsNotNull();
        await Assert.That(aore!.ParamName).IsEqualTo("TotalSegmentCount");
        await Assert.That(aore.Message).StartsWith("Total Segment Count must be a positive number.");
    }

    [Test]
    public async Task Network_Allows_Duplicate_Segments()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var parentTx = client.CreateNewTransactionId();
        var copies = Generator.Integer(10, 20);
        for (int i = 0; i < copies; i++)
        {
            var receipt = await client.SubmitMessageAsync(new SubmitMessageParams
            {
                Topic = fx.CreateReceipt!.Topic,
                Message = Encoding.ASCII.GetBytes(Generator.String(120, 199)),
                ParentTransactionId = parentTx,
                SegmentIndex = 2,
                TotalSegmentCount = 3,
                Signatory = fx.ParticipantPrivateKey
            });
            await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
            await Assert.That(receipt.SequenceNumber).IsEqualTo((ulong)(i + 1));
            await Assert.That(receipt.RunningHash.IsEmpty).IsFalse();
            await Assert.That(receipt.RunningHashVersion).IsEqualTo(3ul);
        }

        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.Memo).IsEqualTo(fx.CreateParams.Memo);
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo((ulong)copies);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsEqualTo(new Endorsement(fx.AdminPublicKey));
        await Assert.That(info.Participant).IsEqualTo(new Endorsement(fx.ParticipantPublicKey));
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsEqualTo(fx.RenewAccount.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Submitting_Messages_Can_Return_Record()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var expectedSequenceNumber = Generator.Integer(10, 20);
        for (int i = 0; i < expectedSequenceNumber; i++)
        {
            var submitParams = new SubmitMessageParams
            {
                Topic = fx.CreateReceipt!.Topic,
                Message = Encoding.ASCII.GetBytes(Generator.String(120, 199)),
                SegmentIndex = 1,
                TotalSegmentCount = 1,
                Signatory = fx.ParticipantPrivateKey
            };

            var messageReceipt = await client.SubmitMessageAsync(submitParams);
            var messageRecord = await client.GetTransactionRecordAsync(messageReceipt.TransactionId) as SubmitMessageRecord;
            await Assert.That(messageRecord).IsNotNull();
            await Assert.That(messageRecord!.Status).IsEqualTo(ResponseCode.Success);
            await Assert.That(messageRecord.SequenceNumber).IsEqualTo((ulong)(i + 1));
            await Assert.That(messageRecord.RunningHashVersion).IsEqualTo(3ul);
            await Assert.That(messageRecord.RunningHash.IsEmpty).IsFalse();
            await Assert.That(messageRecord.Hash.IsEmpty).IsFalse();
            await Assert.That(messageRecord.Consensus).IsNotNull();
            await Assert.That(messageRecord.Memo).IsEmpty();
            await Assert.That(messageRecord.Fee >= 0UL).IsTrue();
        }
        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.SequenceNumber).IsEqualTo((ulong)expectedSequenceNumber);
    }

    [Test]
    public async Task Can_Schedule_Submit_Single_Segmented_Message()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxTopic = await TestTopic.CreateAsync();
        await using var baseClient = await TestNetwork.CreateClientAsync();
        var parentTransactionId = baseClient.CreateNewTransactionId();
        var messageTransactionId = new TransactionId(parentTransactionId.Payer, parentTransactionId.ValidStartSeconds, parentTransactionId.ValidStartNanos, true);
        var submitParams = new SubmitMessageParams
        {
            Topic = fxTopic.CreateReceipt!.Topic,
            Message = Encoding.ASCII.GetBytes(Generator.String(120, 199)),
            SegmentIndex = 1,
            TotalSegmentCount = 1,
            Signatory = fxTopic.ParticipantPrivateKey,
            ParentTransactionId = messageTransactionId,
        };
        var schedulingReceipt = await baseClient.ScheduleAsync(new ScheduleParams
        {
            Transaction = submitParams,
            Payer = fxPayer.CreateReceipt!.Address
        }, ctx => ctx.TransactionId = parentTransactionId);
        await Assert.That(schedulingReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var payerClient = baseClient.Clone(ctx =>
        {
            ctx.Payer = fxPayer.CreateReceipt!.Address;
            ctx.Signatory = fxPayer.PrivateKey;
        });
        var counterReceipt = await payerClient.SignScheduleAsync(schedulingReceipt.Schedule);

        var pendingReceipt = await baseClient.GetReceiptAsync(schedulingReceipt.ScheduledTxId);
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
}
