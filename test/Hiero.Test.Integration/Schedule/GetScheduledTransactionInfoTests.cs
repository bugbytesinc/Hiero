using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Schedule;

public class GetScheduledTransactionInfoTests
{
    [Test]
    public async Task Can_Get_Scheduled_Transaction_Info()
    {
        await using var fx = await TestScheduledTransfer.CreateAsync();
        await Assert.That(fx.ScheduleReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
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
        await Assert.That(info.Deleted).IsNull();
        await Assert.That(info.ScheduledTransactionBodyBytes.IsEmpty).IsFalse();
        await Assert.That(info.DelayExecution).IsFalse();
    }
}
