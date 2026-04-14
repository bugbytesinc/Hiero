using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;
using System.Text;

namespace Hiero.Test.Integration.Consensus;

public class SubmitLargeMessageTests
{
    [Test]
    public async Task Can_Submit_Large_Segmented_Message()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var message = Encoding.ASCII.GetBytes(Generator.String(1200, 1990));
        var segmentSize = Generator.Integer(100, 200);
        var expectedCount = (message.Length + segmentSize - 1) / segmentSize;
        var receipts = await client.SubmitLargeMessageAsync(fx.CreateReceipt!.Topic, message, segmentSize, fx.ParticipantPrivateKey);
        await Assert.That(receipts.Length).IsEqualTo(expectedCount);
        for (int i = 0; i < expectedCount; i++)
        {
            var receipt = receipts[i];
            await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
            await Assert.That(receipt.SequenceNumber).IsEqualTo((ulong)(i + 1));
            await Assert.That(receipt.RunningHash.IsEmpty).IsFalse();
            await Assert.That(receipt.RunningHashVersion).IsEqualTo(3ul);
        }

        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.Memo).IsEqualTo(fx.CreateParams.Memo);
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo((ulong)expectedCount);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsEqualTo(new Endorsement(fx.AdminPublicKey));
        await Assert.That(info.Participant).IsEqualTo(new Endorsement(fx.ParticipantPublicKey));
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsEqualTo(fx.RenewAccount.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Submit_Message_Without_Key_Raises_Error()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var message = Encoding.ASCII.GetBytes(Generator.String(1200, 1990));
        var segmentSize = Generator.Integer(100, 200);

        var ex = await Assert.That(async () =>
        {
            await client.SubmitLargeMessageAsync(fx.CreateReceipt!.Topic, message, segmentSize);
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
        var message = Encoding.ASCII.GetBytes(Generator.String(1200, 1990));
        var segmentSize = Generator.Integer(100, 200);

        var ex = await Assert.That(async () =>
        {
            await client.SubmitLargeMessageAsync(null!, message, segmentSize);
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
        var message = Encoding.ASCII.GetBytes(Generator.String(1200, 1990));
        var segmentSize = Generator.Integer(100, 200);

        var ex = await Assert.That(async () =>
        {
            await client.SubmitLargeMessageAsync(EntityId.None, message, segmentSize, fx.ParticipantPrivateKey);
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
        var segmentSize = Generator.Integer(100, 200);

        var ex = await Assert.That(async () =>
        {
            await client.SubmitLargeMessageAsync(fx.CreateReceipt!.Topic, null!, segmentSize, fx.ParticipantPrivateKey);
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

        var message = Encoding.ASCII.GetBytes(Generator.String(1200, 1990));
        var segmentSize = Generator.Integer(100, 200);

        var ex = await Assert.That(async () =>
        {
            await client.SubmitLargeMessageAsync(fx.CreateReceipt!.Topic, message, segmentSize, fx.ParticipantPrivateKey);
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
        var message = Encoding.ASCII.GetBytes(Generator.String(1200, 1990));
        var segmentSize = Generator.Integer(100, 200);
        var expectedCount = (message.Length + segmentSize - 1) / segmentSize;
        var receipts = await client.SubmitLargeMessageAsync(fx.CreateReceipt!.Topic, message, segmentSize, fx.ParticipantPrivateKey);
        await Assert.That(receipts.Length).IsEqualTo(expectedCount);
        for (int i = 0; i < expectedCount; i++)
        {
            var receipt = receipts[i];
            await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
            await Assert.That(receipt.SequenceNumber).IsEqualTo((ulong)(i + 1));
            await Assert.That(receipt.RunningHash.IsEmpty).IsFalse();
            await Assert.That(receipt.RunningHashVersion).IsEqualTo(3ul);

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
        await Assert.That(info.SequenceNumber).IsEqualTo((ulong)expectedCount);
    }

    [Test]
    public async Task Can_Submit_Large_Segmented_Message_With_Even_Boundary()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var segmentSize = Generator.Integer(100, 200);
        var expectedCount = Generator.Integer(3, 10);
        var message = Encoding.ASCII.GetBytes(Generator.Code(segmentSize * expectedCount));
        var receipts = await client.SubmitLargeMessageAsync(fx.CreateReceipt!.Topic, message, segmentSize, fx.ParticipantPrivateKey);
        await Assert.That(receipts.Length).IsEqualTo(expectedCount);
        for (int i = 0; i < expectedCount; i++)
        {
            var receipt = receipts[i];
            await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
            await Assert.That(receipt.SequenceNumber).IsEqualTo((ulong)(i + 1));
            await Assert.That(receipt.RunningHash.IsEmpty).IsFalse();
            await Assert.That(receipt.RunningHashVersion).IsEqualTo(3ul);
        }

        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.Memo).IsEqualTo(fx.CreateParams.Memo);
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo((ulong)expectedCount);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsEqualTo(new Endorsement(fx.AdminPublicKey));
        await Assert.That(info.Participant).IsEqualTo(new Endorsement(fx.ParticipantPublicKey));
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsEqualTo(fx.RenewAccount.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Submit_Large_Segmented_Message_Smaller_Than_Segment()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var segmentSize = Generator.Integer(100, 200);
        var expectedCount = 1;
        var message = Encoding.ASCII.GetBytes(Generator.Code(segmentSize / 2));
        var receipts = await client.SubmitLargeMessageAsync(fx.CreateReceipt!.Topic, message, segmentSize, fx.ParticipantPrivateKey);
        await Assert.That(receipts.Length).IsEqualTo(expectedCount);
        for (int i = 0; i < expectedCount; i++)
        {
            var receipt = receipts[i];
            await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
            await Assert.That(receipt.SequenceNumber).IsEqualTo((ulong)(i + 1));
            await Assert.That(receipt.RunningHash.IsEmpty).IsFalse();
            await Assert.That(receipt.RunningHashVersion).IsEqualTo(3ul);
        }

        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.Memo).IsEqualTo(fx.CreateParams.Memo);
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo((ulong)expectedCount);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsEqualTo(new Endorsement(fx.AdminPublicKey));
        await Assert.That(info.Participant).IsEqualTo(new Endorsement(fx.ParticipantPublicKey));
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsEqualTo(fx.RenewAccount.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Submit_Large_Segmented_Message_With_Two_Segments()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var segmentSize = Generator.Integer(100, 200);
        var expectedCount = 2;
        var message = Encoding.ASCII.GetBytes(Generator.Code(3 * segmentSize / 2));
        var receipts = await client.SubmitLargeMessageAsync(fx.CreateReceipt!.Topic, message, segmentSize, fx.ParticipantPrivateKey);
        await Assert.That(receipts.Length).IsEqualTo(expectedCount);
        for (int i = 0; i < expectedCount; i++)
        {
            var receipt = receipts[i];
            await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
            await Assert.That(receipt.SequenceNumber).IsEqualTo((ulong)(i + 1));
            await Assert.That(receipt.RunningHash.IsEmpty).IsFalse();
            await Assert.That(receipt.RunningHashVersion).IsEqualTo(3ul);
        }

        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.Memo).IsEqualTo(fx.CreateParams.Memo);
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo((ulong)expectedCount);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsEqualTo(new Endorsement(fx.AdminPublicKey));
        await Assert.That(info.Participant).IsEqualTo(new Endorsement(fx.ParticipantPublicKey));
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsEqualTo(fx.RenewAccount.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }
}
