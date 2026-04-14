using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.Consensus;

public class CreateTopicTests
{
    [Test]
    public async Task Can_Create_A_Topic_Async()
    {
        await using var fx = await TestTopic.CreateAsync();
        await Assert.That(fx.CreateReceipt).IsNotNull();
        await Assert.That(fx.CreateReceipt!.Topic).IsNotNull();
        await Assert.That(fx.CreateReceipt.Topic.AccountNum > 0).IsTrue();
        await Assert.That(fx.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTopicInfoAsync(fx.CreateReceipt.Topic);
        await Assert.That(info.Memo).IsEqualTo(fx.CreateParams.Memo);
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
    public async Task Can_Create_A_Topic_With_Receipt_Async()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.CreateTopicAsync(new CreateTopicParams
        {
            Memo = "Receipt Version"
        });
        await Assert.That(receipt).IsNotNull();
        await Assert.That(receipt.Topic).IsNotNull();
        await Assert.That(receipt.Topic.AccountNum > 0).IsTrue();
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTopicInfoAsync(receipt.Topic);
        await Assert.That(info.Memo).IsEqualTo("Receipt Version");
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo(0UL);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsNull();
        await Assert.That(info.Participant).IsNull();
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsNull();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Create_With_Null_Memo_Raises_Error()
    {
        var ex = await Assert.That(async () =>
        {
            await TestTopic.CreateAsync(fx =>
            {
                fx.CreateParams.Memo = null;
            });
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("Memo");
        await Assert.That(ane.Message).StartsWith("Memo can not be null.");
    }

    [Test]
    public async Task Can_Create_A_Topic_With_Empty_Memo_Async()
    {
        await using var fx = await TestTopic.CreateAsync(fx =>
        {
            fx.CreateParams.Memo = string.Empty;
        });
        await using var client = await TestNetwork.CreateClientAsync();
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
    public async Task Can_Create_A_Topic_With_No_Administrator_And_Auto_Renew_Account_Raises_Error()
    {
        var ex = await Assert.That(async () =>
        {
            await TestTopic.CreateAsync(fx =>
            {
                fx.CreateParams.Administrator = null;
            });
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("Administrator");
        await Assert.That(ane.Message).StartsWith("The Administrator endorsement must not be null if RenewAccount is specified.");
    }

    [Test]
    public async Task Can_Create_A_Topic_With_No_Administrator()
    {
        await using var fx = await TestTopic.CreateAsync(fx =>
        {
            fx.CreateParams.Administrator = null;
            fx.CreateParams.RenewAccount = null;
        });
        await using var client = await TestNetwork.CreateClientAsync();
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
    public async Task Can_Create_A_Topic_With_No_Participant()
    {
        await using var fx = await TestTopic.CreateAsync(fx =>
        {
            fx.CreateParams.Submitter = null;
        });
        await using var client = await TestNetwork.CreateClientAsync();
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
    public async Task Can_Create_A_Topic_With_Invalid_Renew_Period_Raises_Error()
    {
        var ex = await Assert.That(async () =>
        {
            await TestTopic.CreateAsync(fx =>
            {
                fx.CreateParams.RenewPeriod = TimeSpan.FromDays(1);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AutorenewDurationNotInRange);
        await Assert.That(tex.Message).StartsWith("Create Topic failed with status: AutorenewDurationNotInRange");
    }

    [Test]
    public async Task Can_Create_A_Topic_With_No_Renew_Account()
    {
        await using var fx = await TestTopic.CreateAsync(fx =>
        {
            fx.CreateParams.RenewAccount = null;
        });
        await using var client = await TestNetwork.CreateClientAsync();
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
    public async Task Can_Create_A_Topic_With_Alias_Renew_Account_Defect()
    {
        await using var fxRenew = await TestAliasAccount.CreateAsync();
        var ex = await Assert.That(async () =>
        {
            await using var fx = await TestTopic.CreateAsync(fx =>
            {
                fx.CreateParams.RenewAccount = fxRenew.Alias;
                fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.ParticipantPrivateKey, fxRenew.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAutorenewAccount);
        await Assert.That(tex.Message).StartsWith("Create Topic failed with status: InvalidAutorenewAccount");
    }

    [Test]
    public async Task Can_Create_A_Topic_With_Missing_Signatures_Raises_Error()
    {
        var ex = await Assert.That(async () =>
        {
            await TestTopic.CreateAsync(fx =>
            {
                fx.CreateParams.Signatory = null;
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Create Topic failed with status: InvalidSignature");
    }

    [Test]
    public async Task Can_Not_Schedule_A_Create_Topic()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxTopic = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(fxTopic.CreateParams);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
        await Assert.That(tex.Message).StartsWith("Scheduling Create Topic failed with status: ScheduledTransactionNotInWhitelist");
    }

    [Test]
    public async Task Can_Create_Minimal_Topic_With_Renew_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 10_00_000_000);
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();

        await using var baseClient = await TestNetwork.CreateClientAsync();
        await using var client = baseClient.Clone(ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = fxAccount.PrivateKey;
        });

        var receipt = await client.CreateTopicAsync(new CreateTopicParams
        {
            RenewAccount = fxAccount.CreateReceipt!.Address,
            Memo = "TEST",
            Administrator = newPublicKey,
            Signatory = newPrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await baseClient.GetTopicInfoAsync(receipt.Topic);
        await Assert.That(info.Memo).IsEqualTo("TEST");
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo(0UL);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsEqualTo(new Endorsement(newPublicKey));
        await Assert.That(info.Participant).IsNull();
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Create_Topic()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new CreateTopicParams
                {
                    Memo = Generator.Memo(10, 20),
                },
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<TransactionException>();
        await Assert.That(((TransactionException)tex!).Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    }
}
