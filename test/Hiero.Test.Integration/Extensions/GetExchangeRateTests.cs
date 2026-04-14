using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Extensions;

public class GetExchangeRateTests
{
    [Test]
    public async Task Can_Retrieve_Exchange_Rates()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var rate = await client.GetExchangeRatesAsync();
        await Assert.That(rate).IsNotNull();
        await Assert.That(rate.Current).IsNotNull();
        await Assert.That(rate.Next).IsNotNull();
    }

    [Test]
    public async Task Exchange_Rate_Matches_Recent_Transactions()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var txId = new TransactionId(TestNetwork.Payer, DateTime.UtcNow);
        var rate = await client.GetExchangeRatesAsync(default, ctx => ctx.TransactionId = txId);
        var receipt = await client.GetReceiptAsync(txId);
        await Assert.That(receipt.CurrentExchangeRate).IsEqualTo(rate.Current);
        await Assert.That(receipt.NextExchangeRate).IsEqualTo(rate.Next);
    }
}
