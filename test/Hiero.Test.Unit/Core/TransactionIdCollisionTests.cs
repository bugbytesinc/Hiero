using Hiero.Implementation;

namespace Hiero.Test.Unit.Core;

public class TransactionIdCollisionTests
{
    [Test]
    public async Task Tics_Creator_Does_Not_Collide()
    {
        for (int i = 0; i < 100; i++)
        {
            var tic1 = Epoch.UniqueClockNanos();
            var tic2 = Epoch.UniqueClockNanos();
            await Assert.That(tic1).IsNotEqualTo(tic2);
        }
    }

    [Test]
    public async Task Tics_Creator_Does_Not_Collide_Multi_Thread()
    {
        var tasks = new Task[20];
        for (int j = 0; j < tasks.Length; j++)
        {
            tasks[j] = Task.Run(async () =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    var tic1 = Epoch.UniqueClockNanos();
                    var tic2 = Epoch.UniqueClockNanos();
                    await Assert.That(tic1).IsNotEqualTo(tic2);
                }
            });
        }
        await Task.WhenAll(tasks);
    }

    [Test]
    [Category("Slow")]
    public async Task Tics_Creator_Does_Not_Collide_Multi_Thread_In_Linq()
    {
        var tasks = Enumerable.Range(1, 30000).Select(_ => Task.Run(() => Epoch.UniqueClockNanos()));
        var nano = await Task.WhenAll(tasks);
        for (int i = 0; i < nano.Length; i++)
        {
            for (int j = i + 1; j < nano.Length; j++)
            {
                await Assert.That(nano[i]).IsNotEqualTo(nano[j]);
            }
        }
    }

    [Test]
    [Category("Slow")]
    public async Task Client_Creator_Does_Not_Collide_Multi_Thread_In_Linq()
    {
        await using ConsensusClient client = new(cfg =>
        {
            cfg.Payer = new EntityId(0, 0, 3);
        });
        var tasks = Enumerable.Range(1, 20000).Select(_ => Task.Run(() => client.CreateNewTransactionId()));
        var txids = await Task.WhenAll(tasks);
        for (int i = 0; i < txids.Length; i++)
        {
            for (int j = i + 1; j < txids.Length; j++)
            {
                await Assert.That(txids[i]).IsNotEqualTo(txids[j]);
            }
        }
    }
}
