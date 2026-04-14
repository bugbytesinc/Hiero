using Hiero.Implementation;
using Hiero.Test.Integration.Fixtures;
using Proto;

namespace Hiero.Test.Integration.Internals;

public class TimeDriftTests
{
    [Test]
    public async Task Network_Forces_Wait_On_Explicit_Transaction_IDs_Too_Forward_In_Time()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var account = TestNetwork.Payer;
        var startInstant = Epoch.UniqueClockNanos();
        var info = await client.GetAccountInfoAsync(account, default, ctx =>
        {
            ctx.TransactionId = new TransactionID
            {
                AccountID = new AccountID(TestNetwork.Payer),
                TransactionValidStart = new Timestamp(new ConsensusTimeStamp(DateTime.UtcNow.AddSeconds(16)))
            }.AsTxId();
        });
        var duration = Epoch.UniqueClockNanos() - startInstant;
        await Assert.That(duration).IsBetween(500_000L, 240_000_000_000L);
    }

    [Test]
    public async Task Simulate_Time_Drift()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var startInstant = Epoch.UniqueClockNanos();
        Epoch.AddToClockDrift(-5_000_000_000);
        var balance = await client.GetAccountBalanceAsync(TestNetwork.Payer, default, ctx => ctx.AdjustForLocalClockDrift = true);
        var duration = Epoch.UniqueClockNanos() - startInstant;
        await Assert.That(duration).IsBetween(4L, 240_000_000_000L);
    }
}
