using Hiero.Mirror;
using Hiero.Mirror.Filters;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Mirror;

public class MirrorScheduleDataTests
{
    [Test]
    public async Task Can_Get_Schedule_By_Id()
    {
        await using var fxSchedule = await TestScheduledTransfer.CreateAsync();
        var scheduleId = fxSchedule.ScheduleReceipt!.Schedule;

        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var data = await mirror.GetScheduleAsync(scheduleId);

        await Assert.That(data).IsNotNull();
        await Assert.That(data!.Schedule).IsEqualTo(scheduleId);
        await Assert.That(data.Creator).IsEqualTo(TestNetwork.Payer);
        await Assert.That(data.Payer).IsEqualTo(fxSchedule.PayingAccount.CreateReceipt!.Address);
        await Assert.That(data.Administrator).IsEqualTo(fxSchedule.PublicKey);
        await Assert.That(data.Memo).IsEqualTo(fxSchedule.ScheduleParams.Memo);
        await Assert.That(data.Created.Seconds > 0).IsTrue();
        await Assert.That(data.Deleted).IsFalse();
        // Schedule has not collected enough signatures yet, so Executed
        // should be null. (TestScheduledTransfer creates the schedule but
        // doesn't sign it.)
        await Assert.That(data.Executed).IsNull();
        await Assert.That(data.TransactionBody.IsEmpty).IsFalse();
    }

    [Test]
    public async Task Can_Get_Schedules_Filtered_By_Id()
    {
        // Use ScheduleFilter.Is(...) to narrow the network-wide schedule
        // list down to a single fixture schedule so we don't paginate
        // through testnet's history.
        await using var fxSchedule = await TestScheduledTransfer.CreateAsync();
        var scheduleId = fxSchedule.ScheduleReceipt!.Schedule;

        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var matches = new List<ScheduleData>();
        await foreach (var data in mirror.GetSchedulesAsync(ScheduleFilter.Is(scheduleId)))
        {
            matches.Add(data);
            if (matches.Count >= 5)
            {
                break;
            }
        }

        await Assert.That(matches.Count).IsEqualTo(1);
        await Assert.That(matches[0].Schedule).IsEqualTo(scheduleId);
        await Assert.That(matches[0].Memo).IsEqualTo(fxSchedule.ScheduleParams.Memo);
        await Assert.That(matches[0].Administrator).IsEqualTo(fxSchedule.PublicKey);
    }
}
