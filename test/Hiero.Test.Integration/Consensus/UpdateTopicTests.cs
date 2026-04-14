using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.Consensus;

public class UpdateTopicTests
{
    [Test]
    public async Task Update_Witnout_Topic_Raises_Error()
    {
        await using var fx = await TestTopic.CreateAsync();
        var newMemo = Generator.Memo(20, 100);
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.UpdateTopicAsync(new UpdateTopicParams
            {
                Signatory = fx.AdminPrivateKey,
                Memo = newMemo,
            });
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("Topic");
        await Assert.That(ane.Message).StartsWith("Topic address is missing");
    }

    [Test]
    public async Task Update_Witnout_Changes_Raises_Error()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.UpdateTopicAsync(new UpdateTopicParams
            {
                Topic = fx.CreateReceipt!.Topic,
                Signatory = fx.AdminPrivateKey
            });
        }).ThrowsException();
        var ae = ex as ArgumentException;
        await Assert.That(ae).IsNotNull();
        await Assert.That(ae!.ParamName).IsEqualTo("UpdateTopicParams");
        await Assert.That(ae.Message).StartsWith("The Topic Updates contain no update properties, it is blank");
    }

    [Test]
    public async Task Can_Update_Memo()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var newMemo = Generator.Memo(20, 100);
        var receipt = await client.UpdateTopicAsync(new UpdateTopicParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Signatory = fx.AdminPrivateKey,
            Memo = newMemo,
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.Memo).IsEqualTo(newMemo);
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo(0UL);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsEqualTo(new Endorsement(fx.AdminPublicKey));
        await Assert.That(info.Participant).IsEqualTo(new Endorsement(fx.ParticipantPublicKey));
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsEqualTo(fx.RenewAccount.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Update_Memo_With_Record()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var newMemo = Generator.Memo(20, 100);
        var receipt = await client.UpdateTopicAsync(new UpdateTopicParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Signatory = fx.AdminPrivateKey,
            Memo = newMemo,
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee >= 0UL).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.Memo).IsEqualTo(newMemo);
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo(0UL);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsEqualTo(new Endorsement(fx.AdminPublicKey));
        await Assert.That(info.Participant).IsEqualTo(new Endorsement(fx.ParticipantPublicKey));
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsEqualTo(fx.RenewAccount.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Update_Memo_To_Empty()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.UpdateTopicAsync(new UpdateTopicParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Signatory = fx.AdminPrivateKey,
            Memo = string.Empty,
        });

        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.Memo).IsEmpty();
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo(0UL);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsEqualTo(new Endorsement(fx.AdminPublicKey));
        await Assert.That(info.Participant).IsEqualTo(new Endorsement(fx.ParticipantPublicKey));
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsEqualTo(fx.RenewAccount.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Removing_Administrator_Without_Removing_Auto_Renew_Account_Raises_Error()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.UpdateTopicAsync(new UpdateTopicParams
            {
                Topic = fx.CreateReceipt!.Topic,
                Signatory = fx.AdminPrivateKey,
                Administrator = Endorsement.None
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AutorenewAccountNotAllowed);
        await Assert.That(tex.Message).StartsWith("Topic Update failed with status: AutorenewAccountNotAllowed");
    }

    [Test]
    public async Task Can_Make_Imutable()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.UpdateTopicAsync(new UpdateTopicParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Signatory = fx.AdminPrivateKey,
            Administrator = Endorsement.None,
            RenewAccount = EntityId.None
        });

        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.Memo).IsEqualTo(fx.CreateParams.Memo);
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo(0UL);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsNull();
        await Assert.That(info.Participant).IsEqualTo(new Endorsement(fx.ParticipantPublicKey));
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsNull();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Cannot_Update_After_Made_Immutable()
    {
        await using var fx = await TestTopic.CreateAsync(fx => fx.CreateParams.RenewAccount = null);
        await using var client = await TestNetwork.CreateClientAsync();

        var memo = Generator.Memo(20, 100);
        await client.UpdateTopicAsync(new UpdateTopicParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Signatory = fx.AdminPrivateKey,
            Memo = memo
        });

        var info = await client.GetTopicInfoAsync(fx);
        await Assert.That(info.Memo).IsEqualTo(memo);

        await client.UpdateTopicAsync(new UpdateTopicParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Signatory = fx.AdminPrivateKey,
            Administrator = Endorsement.None
        });

        var ex = await Assert.That(async () =>
        {
            await client.UpdateTopicAsync(new UpdateTopicParams
            {
                Topic = fx.CreateReceipt!.Topic,
                Signatory = fx.AdminPrivateKey,
                Memo = Generator.Memo(20, 100)
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.Unauthorized);
        await Assert.That(tex.Message).StartsWith("Topic Update failed with status: Unauthorized");

        ex = await Assert.That(async () =>
        {
            await client.UpdateTopicAsync(new UpdateTopicParams
            {
                Topic = fx.CreateReceipt!.Topic,
                Memo = Generator.Memo(20, 100)
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.Unauthorized);
        await Assert.That(tex.Message).StartsWith("Topic Update failed with status: Unauthorized");

        info = await client.GetTopicInfoAsync(fx);
        await Assert.That(info.Memo).IsEqualTo(memo);
    }

    [Test]
    public async Task Can_Update_Participant()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var (newPublic, _) = Generator.KeyPair();

        await client.UpdateTopicAsync(new UpdateTopicParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Signatory = fx.AdminPrivateKey,
            Submitter = newPublic
        });

        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.Memo).IsEqualTo(fx.CreateParams.Memo);
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo(0UL);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsEqualTo(new Endorsement(fx.AdminPublicKey));
        await Assert.That(info.Participant).IsEqualTo(new Endorsement(newPublic));
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsEqualTo(fx.RenewAccount.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Update_Participant_To_None()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.UpdateTopicAsync(new UpdateTopicParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Signatory = fx.AdminPrivateKey,
            Submitter = Endorsement.None
        });

        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.Memo).IsEqualTo(fx.CreateParams.Memo);
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo(0UL);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsEqualTo(new Endorsement(fx.AdminPublicKey));
        await Assert.That(info.Participant).IsNull();
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsEqualTo(fx.RenewAccount.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Update_Renew_Period()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.UpdateTopicAsync(new UpdateTopicParams
            {
                Topic = fx.CreateReceipt!.Topic,
                Signatory = fx.AdminPrivateKey,
                RenewPeriod = TimeSpan.FromDays(1)
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AutorenewDurationNotInRange);
        await Assert.That(tex.Message).StartsWith("Topic Update failed with status: AutorenewDurationNotInRange");
    }

    [Test]
    public async Task Can_Update_Auto_Renew_Account()
    {
        await using var fxTopic = await TestTopic.CreateAsync();
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.UpdateTopicAsync(new UpdateTopicParams
        {
            Topic = fxTopic.CreateReceipt!.Topic,
            Signatory = new Signatory(fxTopic.AdminPrivateKey, fxAccount.PrivateKey),
            RenewAccount = fxAccount.CreateReceipt!.Address
        });

        var info = await client.GetTopicInfoAsync(fxTopic.CreateReceipt!.Topic);
        await Assert.That(info.Memo).IsEqualTo(fxTopic.CreateParams.Memo);
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo(0UL);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsEqualTo(new Endorsement(fxTopic.AdminPublicKey));
        await Assert.That(info.Participant).IsEqualTo(new Endorsement(fxTopic.ParticipantPublicKey));
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Update_Auto_Renew_Account_To_Alias_Account_Defect()
    {
        await using var fxTopic = await TestTopic.CreateAsync();
        await using var fxAccount = await TestAliasAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var infoBefore = await client.GetTopicInfoAsync(fxTopic.CreateReceipt!.Topic);
        await Assert.That(infoBefore.RenewAccount).IsEqualTo(fxTopic.RenewAccount.CreateReceipt!.Address);

        var ex = await Assert.That(async () =>
        {
            await client.UpdateTopicAsync(new UpdateTopicParams
            {
                Topic = fxTopic.CreateReceipt!.Topic,
                Signatory = new Signatory(fxTopic.AdminPrivateKey, fxAccount.PrivateKey),
                RenewAccount = fxAccount.Alias
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAutorenewAccount);
        await Assert.That(tex.Message).StartsWith("Topic Update failed with status: InvalidAutorenewAccount");

        var infoAfter = await client.GetTopicInfoAsync(fxTopic.CreateReceipt!.Topic);
        await Assert.That(infoAfter.RenewAccount).IsEqualTo(infoBefore.RenewAccount);
    }

    [Test]
    public async Task Can_Update_Auto_Renew_Account_To_None()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.UpdateTopicAsync(new UpdateTopicParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Signatory = fx.AdminPrivateKey,
            RenewAccount = EntityId.None
        });

        var info = await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        await Assert.That(info.Memo).IsEqualTo(fx.CreateParams.Memo);
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo(0UL);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsEqualTo(new Endorsement(fx.AdminPublicKey));
        await Assert.That(info.Participant).IsEqualTo(new Endorsement(fx.ParticipantPublicKey));
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsNull();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Needs_Admin_Signature()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newMemo = Generator.Memo(20, 100);

        var ex = await Assert.That(async () =>
        {
            await client.UpdateTopicAsync(new UpdateTopicParams
            {
                Topic = fx.CreateReceipt!.Topic,
                Memo = newMemo,
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Topic Update failed with status: InvalidSignature");
    }

    [Test]
    public async Task Can_Not_Schedule_Update()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxTopic = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newMemo = Generator.Memo(20, 100);
        var ex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new UpdateTopicParams
            {
                Topic = fxTopic.CreateReceipt!.Topic,
                Memo = newMemo,
                Signatory = fxTopic.AdminPrivateKey,
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
        await Assert.That(tex.Message).StartsWith("Scheduling Topic Update failed with status: ScheduledTransactionNotInWhitelist");
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Update_Topic()
    {
        await using var fxTopic = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new UpdateTopicParams
                {
                    Topic = fxTopic.CreateReceipt!.Topic,
                    Memo = Generator.Memo(10, 20),
                },
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<TransactionException>();
        await Assert.That(((TransactionException)tex!).Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    }
}
