using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Schedule;

public class CreateScheduledTransactionTests
{
    [Test]
    public async Task Can_Schedule_A_Transfer_Transaction()
    {
        await using var pendingFx = await TestScheduledTransfer.CreateAsync();
        await Assert.That(pendingFx.ScheduleReceipt!.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(pendingFx.ScheduleReceipt.Schedule).IsNotEqualTo(EntityId.None);
    }
}
