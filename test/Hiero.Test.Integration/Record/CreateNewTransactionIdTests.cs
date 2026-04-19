using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Record;

public class CreateNewTransactionIdTests
{
    [Test]
    public async Task Can_Create_Transaction_Id()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var txId = client.CreateNewTransactionId();
        await Assert.That(txId).IsNotNull();
    }

    [Test]
    public async Task Transaction_Matches_Paying_Account()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var txId = client.CreateNewTransactionId();
        await Assert.That(txId.Payer).IsEqualTo(TestNetwork.Payer);
    }

    [Test]
    public async Task Transaction_Timestamp_Is_Reasonable()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var before = DateTime.UtcNow;
        var txId = client.CreateNewTransactionId();
        await Assert.That(txId.ValidStartSeconds > 0).IsTrue();
    }

    [Test]
    public async Task Requires_Payer_Account()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        client.Configure(ctx => ctx.Payer = null);

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            client.CreateNewTransactionId();
        });
        await Assert.That(ex.Message).StartsWith("The Payer address has not been configured. Please check that 'Payer' is set in the Client context.");
    }

    [Test]
    public async Task Creating_Two_In_A_Row_Increases_Timestamp()
    {
        await using var client = await TestNetwork.CreateClientAsync();

        var txId1 = client.CreateNewTransactionId();
        var txId2 = client.CreateNewTransactionId();

        await Assert.That(txId1 != txId2).IsTrue();
        await Assert.That(
            txId2.ValidStartSeconds > txId1.ValidStartSeconds ||
            (txId2.ValidStartSeconds == txId1.ValidStartSeconds && txId2.ValidStartNanos > txId1.ValidStartNanos)
        ).IsTrue();
    }

    [Test]
    public async Task Creating_Multiple_In_Parallel_Produces_Unique_Ids()
    {
        await using var client = await TestNetwork.CreateClientAsync();

        async Task<TransactionId> AsyncMethod()
        {
            await Task.Delay(1);
            return client.CreateNewTransactionId();
        }

        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(AsyncMethod)).ToArray();
        await Task.WhenAll(tasks);

        for (int i = 0; i < tasks.Length; i++)
        {
            for (int j = i + 1; j < tasks.Length; j++)
            {
                await Assert.That(tasks[i].Result != tasks[j].Result).IsTrue();
            }
        }
    }

    [Test]
    public async Task Can_Change_Address_In_Method_Options()
    {
        await using var client = await TestNetwork.CreateClientAsync();

        var address = new EntityId(Generator.Integer(20, 100), 0, Generator.Integer(20, 100));
        var txId = client.CreateNewTransactionId(ctx => ctx.Payer = address);

        await Assert.That(txId.Payer).IsEqualTo(address);
    }

    [Test]
    public async Task Can_Pin_Transaction_Id_In_Options()
    {
        await using var client = await TestNetwork.CreateClientAsync();

        var txExpected = new TransactionId(new EntityId(Generator.Integer(20, 100), 0, Generator.Integer(20, 100)), DateTime.UtcNow);
        var txReturned = client.CreateNewTransactionId(ctx => ctx.TransactionId = txExpected);

        await Assert.That(txReturned).IsEqualTo(txExpected);
    }
}
