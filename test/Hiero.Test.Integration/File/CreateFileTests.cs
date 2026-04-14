using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Text;

namespace Hiero.Test.Integration.File;

public class CreateFileTests
{
    [Test]
    public async Task Can_Create_A_File()
    {
        await using var test = await TestFile.CreateAsync();
        await Assert.That(test.CreateReceipt).IsNotNull();
        await Assert.That(test.CreateReceipt!.File).IsNotEqualTo(EntityId.None);
        await Assert.That(test.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Create_File()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var client = await TestNetwork.CreateClientAsync();
        var (publicKey, privateKey) = Generator.KeyPair();

        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new CreateFileParams
                {
                    Expiration = Generator.TruncateToSeconds(DateTime.UtcNow.AddSeconds(7890000)),
                    Endorsements = [publicKey],
                    Contents = Encoding.Unicode.GetBytes("Scheduled File " + Generator.Code(20)).Take(48).ToArray(),
                    Memo = Generator.Memo(20),
                },
                Payer = fxPayer,
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<TransactionException>();
        await Assert.That(((TransactionException)tex!).Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    }
}
