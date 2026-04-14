using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.QueryFee;

public class QueryFeeTests
{
    [Test]
    public async Task Can_Get_Query_Fees_Of_Query_Fees()
    {
        await using var fx = await TestAccount.CreateAsync();
        var account = fx.CreateReceipt!.Address;
        await using var payerClient = await TestNetwork.CreateClientAsync();
        await payerClient.TransferAsync(TestNetwork.Payer, account, 10_000_000);
        await using var client = payerClient.Clone(ctx =>
        {
            ctx.Payer = account;
            ctx.Signatory = fx.PrivateKey;
            ctx.FeeLimit = 1_000_000;
        });

        var balanceBeforeQuery = (long)await payerClient.GetAccountBalanceAsync(account);

        var txGetAccountInfo = client.CreateNewTransactionId();
        var accountInfo = await client.GetAccountInfoAsync(TestNetwork.Payer, default, ctx => ctx.TransactionId = txGetAccountInfo);

        await payerClient.GetReceiptAsync(txGetAccountInfo);
        var balanceAfterQuery = (long)await payerClient.GetAccountBalanceAsync(account);

        var txQueryRecord = client.CreateNewTransactionId();
        var queryRecord = await client.GetTransactionRecordAsync(txGetAccountInfo, default, ctx => ctx.TransactionId = txQueryRecord);

        await payerClient.GetReceiptAsync(txQueryRecord);
        var balanceAfterQueryRecord = (long)await payerClient.GetAccountBalanceAsync(account);

        var txQueryRecordRecord = client.CreateNewTransactionId();
        var queryRecordRecord = await client.GetTransactionRecordAsync(txQueryRecord, default, ctx => ctx.TransactionId = txQueryRecordRecord);

        await payerClient.GetReceiptAsync(txQueryRecordRecord);
        var balanceAfterQueryRecordRecord = (long)await payerClient.GetAccountBalanceAsync(account);

        TestContext.Current?.OutputWriter.WriteLine($"Initial Balance:              {balanceBeforeQuery:n0}");
        TestContext.Current?.OutputWriter.WriteLine($"After Get Account Info Query: {balanceAfterQuery:n0}");
        TestContext.Current?.OutputWriter.WriteLine($"After Get Record:             {balanceAfterQueryRecord:n0}");
        TestContext.Current?.OutputWriter.WriteLine($"After Record Record:          {balanceAfterQueryRecordRecord:n0}");

        await Assert.That(balanceAfterQuery).IsEqualTo(balanceBeforeQuery + queryRecord.Transfers[account]);
        await Assert.That(balanceAfterQueryRecord).IsEqualTo(balanceAfterQuery + queryRecordRecord.Transfers[account]);
        await Assert.That(balanceAfterQueryRecord > balanceAfterQueryRecordRecord).IsTrue();
    }
}
