using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Schedule;

public class SignScheduledTransactionTests
{
    [Test]
    public async Task Can_Sign_A_Scheduled_Transfer_Transaction()
    {
        await using var pendingFx = await TestScheduledTransfer.CreateAsync();
        await Assert.That(pendingFx.ScheduleReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.SignScheduleAsync(new SignScheduleParams
        {
            Schedule = pendingFx.ScheduleReceipt.Schedule,
            Signatory = pendingFx.SendingAccount,
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.TransactionId).IsNotEqualTo(TransactionId.None);
        await Assert.That(receipt.CurrentExchangeRate).IsNotNull();
        await Assert.That(receipt.CurrentExchangeRate!.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(receipt.CurrentExchangeRate.Expiration < ConsensusTimeStamp.MaxValue).IsTrue();
        await Assert.That(receipt.NextExchangeRate).IsNotNull();
        await Assert.That(receipt.NextExchangeRate!.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(receipt.NextExchangeRate.Expiration < ConsensusTimeStamp.MaxValue).IsTrue();
    }

    [Test]
    public async Task Can_Sign_A_Scheduled_Transfer_And_Get_Receipt()
    {
        await using var pendingFx = await TestScheduledTransfer.CreateAsync();
        await Assert.That(pendingFx.ScheduleReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.SignScheduleAsync(new SignScheduleParams
        {
            Schedule = pendingFx.ScheduleReceipt.Schedule,
            Signatory = pendingFx.SendingAccount,
        });
        var scheduledReceipt = await client.GetReceiptAsync(pendingFx.ScheduleReceipt.ScheduledTransactionId);
        await Assert.That(scheduledReceipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(scheduledReceipt.TransactionId).IsNotEqualTo(TransactionId.None);
        await Assert.That(scheduledReceipt.CurrentExchangeRate).IsNotNull();
        await Assert.That(scheduledReceipt.CurrentExchangeRate!.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(scheduledReceipt.CurrentExchangeRate.Expiration < ConsensusTimeStamp.MaxValue).IsTrue();
        await Assert.That(scheduledReceipt.NextExchangeRate).IsNotNull();
        await Assert.That(scheduledReceipt.NextExchangeRate!.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(scheduledReceipt.NextExchangeRate.Expiration < ConsensusTimeStamp.MaxValue).IsTrue();
    }
}
