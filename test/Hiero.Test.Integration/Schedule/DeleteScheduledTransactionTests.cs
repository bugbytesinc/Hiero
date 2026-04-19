using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Schedule;

public class DeleteScheduledTransactionTests
{
    [Test]
    public async Task Can_Delete_Scheduled_Transaction()
    {
        await using var fx = await TestScheduledTransfer.CreateAsync();
        await Assert.That(fx.ScheduleReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.DeleteScheduleAsync(fx.ScheduleReceipt.Schedule, ctx => ctx.Signatory = new Signatory(fx.PrivateKey, ctx.Signatory!));
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Deleting_Scheduled_Transaction_Does_Not_Remove_Info()
    {
        await using var fx = await TestScheduledTransfer.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.DeleteScheduleAsync(fx.ScheduleReceipt!.Schedule, ctx => ctx.Signatory = new Signatory(fx.PrivateKey, ctx.Signatory!));
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);

        var info = await client.GetScheduleInfoAsync(fx.ScheduleReceipt.Schedule);
        await Assert.That(info.Schedule).IsEqualTo(fx.ScheduleReceipt.Schedule);
        await Assert.That(info.TransactionId).IsEqualTo(fx.ScheduleReceipt.ScheduledTransactionId);
        await Assert.That(info.Creator).IsEqualTo(TestNetwork.Payer);
        await Assert.That(info.Payer).IsEqualTo(fx.PayingAccount.CreateReceipt!.Address);
        await Assert.That(info.Endorsements.Length).IsEqualTo(1);
        await Assert.That(info.Endorsements[0]).IsEqualTo(new Endorsement(fx.PayingAccount.PublicKey));
        await Assert.That(info.Administrator).IsEqualTo(new Endorsement(fx.PublicKey));
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Executed).IsNull();
        await Assert.That(info.Deleted).IsEqualTo(record.Consensus);
        await Assert.That(info.ScheduledTransactionBodyBytes.IsEmpty).IsFalse();
        await Assert.That(info.DelayExecution).IsFalse();
    }

    [Test]
    public async Task Can_Delete_Scheduled_Transaction_With_Signatory_Params()
    {
        await using var fx = await TestScheduledTransfer.CreateAsync();
        await Assert.That(fx.ScheduleReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.DeleteScheduleAsync(new DeleteScheduleParams
        {
            Schedule = fx.ScheduleReceipt.Schedule,
            Signatory = fx.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetScheduleInfoAsync(fx.ScheduleReceipt.Schedule);
        await Assert.That(info.Schedule).IsEqualTo(fx.ScheduleReceipt.Schedule);
        await Assert.That(info.TransactionId).IsEqualTo(fx.ScheduleReceipt.ScheduledTransactionId);
        await Assert.That(info.Creator).IsEqualTo(TestNetwork.Payer);
        await Assert.That(info.Payer).IsEqualTo(fx.PayingAccount.CreateReceipt!.Address);
        await Assert.That(info.Endorsements.Length).IsEqualTo(1);
        await Assert.That(info.Endorsements[0]).IsEqualTo(new Endorsement(fx.PayingAccount.PublicKey));
        await Assert.That(info.Administrator).IsEqualTo(new Endorsement(fx.PublicKey));
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Executed).IsNull();
        await Assert.That(info.ScheduledTransactionBodyBytes.IsEmpty).IsFalse();
    }
}
