using Hiero.Test.Helpers;

namespace Hiero.Test.Integration.Fixtures;

public class TestScheduledTransfer : IAsyncDisposable
{
    public required TestAccount SendingAccount;
    public required TestAccount ReceivingAccount;
    public required TestAccount PayingAccount;
    public required ScheduleParams ScheduleParams;
    public required ScheduleReceipt ScheduleReceipt;
    public required ReadOnlyMemory<byte> PublicKey;
    public required ReadOnlyMemory<byte> PrivateKey;

    public static async Task<TestScheduledTransfer> CreateAsync(Action<TestScheduledTransfer>? customize = null)
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING SETUP: Scheduled Transfer Crypto Transaction");
        var (publicKey, privateKey) = Generator.KeyPair();
        var sendingAccount = await TestAccount.CreateAsync();
        var receivingAccount = await TestAccount.CreateAsync();
        var payingAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 10_00_000_000);
        var xferAmount = (long)sendingAccount.CreateParams.InitialBalance / 2;
        var transferParams = new TransferParams
        {
            CryptoTransfers = new[]
            {
                new CryptoTransfer(sendingAccount, -xferAmount),
                new CryptoTransfer(receivingAccount, xferAmount)
            },
            Signatory = new Signatory(payingAccount.PrivateKey, privateKey)
        };
        var scheduleParams = new ScheduleParams
        {
            Transaction = transferParams,
            Payer = payingAccount,
            Administrator = publicKey,
            Memo = Generator.Memo(20),
        };
        var fixture = new TestScheduledTransfer
        {
            PublicKey = publicKey,
            PrivateKey = privateKey,
            SendingAccount = sendingAccount,
            ReceivingAccount = receivingAccount,
            PayingAccount = payingAccount,
            ScheduleParams = scheduleParams,
            ScheduleReceipt = null!,
        };
        customize?.Invoke(fixture);
        await using var client = await TestNetwork.CreateClientAsync();
        fixture.ScheduleReceipt = await client.ScheduleAsync(fixture.ScheduleParams);
        TestContext.Current?.OutputWriter.WriteLine("SETUP COMPLETED: Scheduled Transfer Crypto Transaction");
        return fixture;
    }
    public async ValueTask DisposeAsync()
    {
        TestContext.Current?.OutputWriter.WriteLine("STARTING TEARDOWN: Scheduled Transfer Crypto Transaction");
        await ReceivingAccount.DisposeAsync();
        await SendingAccount.DisposeAsync();
        await PayingAccount.DisposeAsync();
        TestContext.Current?.OutputWriter.WriteLine("TEARDOWN COMPLETED: Scheduled Transfer Crypto Transaction");
    }
}
