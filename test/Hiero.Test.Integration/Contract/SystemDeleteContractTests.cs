using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Contract;

// NOTE: All tests in this class require the configured Payer key to have admin
// rights over the System Delete Administrator account (Hedera 0.0.59). Without
// those rights, the network returns AUTHORIZATION_FAILED/NOT_SUPPORTED and the
// tests will fail at runtime. Tests are marked [Skip] until such a configuration
// is available. When running against a privileged environment, remove the [Skip]
// attribute.

public class SystemDeleteContractTests
{
    [Test]
    [Skip("Requires System Delete Administrator account (elevated privileges not available)")]
    public async Task Can_Delete_Contract()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var systemAddress = TestNetwork.SystemDeleteAdminAddress;

        var receipt = await client.SystemDeleteContractAsync(
            new SystemDeleteContractParams { Contract = fx.ContractReceipt!.Contract },
            ctx => ctx.Payer = systemAddress);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var ex = await Assert.That(async () =>
        {
            await client.GetContractInfoAsync(fx.ContractReceipt!.Contract);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.ContractDeleted);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: ContractDeleted");
    }

    [Test]
    [Skip("Requires System Delete Administrator account (elevated privileges not available)")]
    public async Task Can_Not_Schedule_Delete()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var fxPayer = await TestAccount.CreateAsync(a => a.CreateParams.InitialBalance = 20_00_000_000);
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new SystemDeleteContractParams
            {
                Contract = fx.ContractReceipt!.Contract
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
        await Assert.That(tex.Message).StartsWith("Unable to schedule transaction, status: ScheduledTransactionNotInWhitelist");
    }
}
