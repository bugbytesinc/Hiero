using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Consensus;

public class DeleteTopicTests
{
    [Test]
    public async Task Can_Delete_Topic()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.DeleteTopicAsync(new DeleteTopicParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Signatory = fx.AdminPrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var ex = await Assert.That(async () =>
        {
            await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InvalidTopicId);
    }

    [Test]
    public async Task Calling_Delete_Without_Admin_Key_Raises_Error()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.DeleteTopicAsync(fx.CreateReceipt!.Topic);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Delete Topic failed with status: InvalidSignature");
    }

    [Test]
    public async Task Calling_Delete_On_Imutable_Topic_Raises_Error()
    {
        await using var fx = await TestTopic.CreateAsync(fx =>
        {
            fx.CreateParams.Administrator = null;
            fx.CreateParams.RenewAccount = null;
        });
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.DeleteTopicAsync(fx.CreateReceipt!.Topic);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.Unauthorized);
        await Assert.That(tex.Message).StartsWith("Delete Topic failed with status: Unauthorized");
    }

    [Test]
    public async Task Can_Delete_Topic_With_Multi_Sig()
    {
        var (pubAdminKey2, privateAdminKey2) = Generator.KeyPair();
        await using var fx = await TestTopic.CreateAsync(fx =>
        {
            fx.CreateParams.Administrator = new Endorsement(1, fx.AdminPublicKey, pubAdminKey2);
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, privateAdminKey2);
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.DeleteTopicAsync(new DeleteTopicParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Signatory = privateAdminKey2
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var ex = await Assert.That(async () =>
        {
            await client.GetTopicInfoAsync(fx.CreateReceipt!.Topic);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InvalidTopicId);
    }

    [Test]
    public async Task Deleting_Deleted_Topic_Raises_Error_Defect()
    {
        await using var fx = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.DeleteTopicAsync(new DeleteTopicParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Signatory = fx.AdminPrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        // THIS SHOULD THROW AN ERROR BUT IT DOES NOT.
        await client.DeleteTopicAsync(new DeleteTopicParams
        {
            Topic = fx.CreateReceipt!.Topic,
            Signatory = fx.AdminPrivateKey
        });
    }

    [Test]
    public async Task Calling_Delete_On_Invalid_Topic_ID_Raises_Error()
    {
        await using var fx = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.DeleteTopicAsync(fx.CreateReceipt!.Address);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidTopicId);
        await Assert.That(tex.Message).StartsWith("Delete Topic failed with status: InvalidTopicId");
    }

    [Test]
    public async Task Calling_Delete_With_Missing_Topic_ID_Raises_Error()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.DeleteTopicAsync((EntityId)null!);
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("topic");
        await Assert.That(ane.Message).StartsWith("Topic Address is missing. Please check that it is not null.");
    }

    [Test]
    public async Task Can_Not_Schedule_A_Delete_Topic()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxTopic = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new DeleteTopicParams
            {
                Topic = fxTopic.CreateReceipt!.Topic,
                Signatory = fxTopic.AdminPrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
        await Assert.That(tex.Message).StartsWith("Scheduling Delete Topic failed with status: ScheduledTransactionNotInWhitelist");
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Delete_Topic()
    {
        await using var fxTopic = await TestTopic.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new DeleteTopicParams
                {
                    Topic = fxTopic.CreateReceipt!.Topic,
                },
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<TransactionException>();
        await Assert.That(((TransactionException)tex!).Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    }
}
