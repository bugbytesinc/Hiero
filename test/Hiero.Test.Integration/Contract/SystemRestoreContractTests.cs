using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Contract;

// NOTE: All tests in this class require the configured Payer key to have admin
// rights over the System Undelete Administrator account (Hedera 0.0.60) and
// possibly the System Delete Administrator account (Hedera 0.0.59). Without
// those rights, the network returns AUTHORIZATION_FAILED/NOT_SUPPORTED and the
// tests will fail at runtime. Tests are marked [Skip] until such a configuration
// is available. When running against a privileged environment, remove the [Skip]
// attribute.

public class SystemRestoreContractTests
{
    [Test]
    [Skip("Requires System Undelete Administrator account (elevated privileges not available)")]
    public async Task System_Restore_Contract_Is_Broken()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.DeleteContractAsync(new DeleteContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fx.PrivateKey
        });

        var systemAddress = TestNetwork.SystemUndeleteAdminAddress;

        var ex = await Assert.That(async () =>
        {
            await client.SystemRestoreContractAsync(
                new SystemRestoreContractParams { Contract = fx.ContractReceipt!.Contract },
                ctx => ctx.Payer = systemAddress);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidFileId);
        await Assert.That(tex.Message).StartsWith("Unable to restore contract, status: InvalidFileId");
    }

    [Test]
    [Skip("Requires System Undelete Administrator account (elevated privileges not available)")]
    public async Task Can_Not_Schedule_Restore()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var fxPayer = await TestAccount.CreateAsync(a => a.CreateParams.InitialBalance = 20_00_000_000);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.DeleteContractAsync(new DeleteContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fx.PrivateKey
        });

        var ex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new SystemRestoreContractParams
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
