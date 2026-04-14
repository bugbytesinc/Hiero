using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Record;

public class GetAccountRecordTests
{
    [Test]
    public async Task Transaction_Records_Are_Stored_For_A_Limited_Time()
    {
        await using var fx = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var childFeeLimit = 1_000_000;
        var transferAmount = Generator.Integer(200, 500);
        var transactionCount = Generator.Integer(3, 6);
        var childAccount = fx.CreateReceipt!.Address;
        var parentAccount = TestNetwork.Payer;
        await client.TransferAsync(parentAccount, childAccount, transactionCount * (childFeeLimit + transferAmount));
        await using (var childClient = client.Clone(ctx => { ctx.Payer = childAccount; ctx.Signatory = fx.PrivateKey; ctx.FeeLimit = childFeeLimit; }))
        {
            for (int i = 0; i < transactionCount; i++)
            {
                await childClient.TransferAsync(childAccount, parentAccount, transferAmount);
            }
        }
        var records = await client.GetAccountRecordsAsync(childAccount);
        await Assert.That(records).IsNotNull();
        await Assert.That(records.Length).IsEqualTo(transactionCount);
        foreach (var record in records)
        {
            await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
            await Assert.That(record.Transfers.Count >= 3).IsTrue();
            await Assert.That(record.Transfers[childAccount]).IsEqualTo(-transferAmount - (long)record.Fee);
            await Assert.That(record.Transfers[parentAccount]).IsEqualTo(transferAmount);
            await Assert.That(record.TokenTransfers).IsEmpty();
            await Assert.That(record.NftTransfers).IsEmpty();
            await Assert.That(record.Royalties).IsEmpty();
            await Assert.That(record.Associations).IsEmpty();
        }
    }

    [Test]
    public async Task Empty_Account_Raises_Error()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.GetAccountRecordsAsync(null!);
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("account");
        await Assert.That(ane.Message).StartsWith("Account Address/Alias is missing. Please check that it is not null.");
    }

    [Test]
    public async Task Deleted_Account_Raises_Error()
    {
        await using var fx = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = fx.CreateReceipt!.Address,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fx.PrivateKey
        });

        var ex = await Assert.That(async () =>
        {
            await client.GetAccountRecordsAsync(fx.CreateReceipt.Address);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.AccountDeleted);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: AccountDeleted");
    }
}
