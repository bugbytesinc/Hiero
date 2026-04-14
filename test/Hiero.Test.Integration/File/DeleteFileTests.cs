using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.File;

public class DeleteFileTests
{
    [Test]
    public async Task Can_Delete_A_File()
    {
        await using var test = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var result = await client.DeleteFileAsync(new DeleteFileParams
        {
            File = test.CreateReceipt!.File,
            Signatory = test.CreateParams.Signatory
        });
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetFileInfoAsync(test.CreateReceipt.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(test.CreateReceipt.File);
        await Assert.That(info.Size).IsEqualTo(0);
        await Assert.That(info.Expiration).IsEqualTo(test.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEquivalentTo(new Endorsement[] { test.PublicKey });
        await Assert.That(info.Deleted).IsTrue();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Delete_A_File_With_Params()
    {
        await using var test = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var result = await client.DeleteFileAsync(new DeleteFileParams
        {
            File = test.CreateReceipt!.File,
            Signatory = test.CreateParams.Signatory
        });
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetFileInfoAsync(test.CreateReceipt.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(test.CreateReceipt.File);
        await Assert.That(info.Size).IsEqualTo(0);
        await Assert.That(info.Expiration).IsEqualTo(test.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEquivalentTo(new Endorsement[] { test.PublicKey });
        await Assert.That(info.Deleted).IsTrue();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Cannot_Delete_An_Immutable_File()
    {
        await using var test = await TestFile.CreateAsync(fx =>
        {
            fx.CreateParams.Endorsements = Array.Empty<Endorsement>();
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.DeleteFileAsync(new DeleteFileParams
            {
                File = test.CreateReceipt!.File,
                Signatory = test.CreateParams.Signatory
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.Unauthorized);
        await Assert.That(tex.Message).StartsWith("Delete File failed with status: Unauthorized");

        var info = await client.GetFileInfoAsync(test.CreateReceipt!.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(test.CreateReceipt.File);
        await Assert.That(info.Size).IsEqualTo(test.CreateParams.Contents.Length);
        await Assert.That(info.Expiration).IsEqualTo(test.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Delete_File()
    {
        await using var fxFile = await TestFile.CreateAsync();
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var client = await TestNetwork.CreateClientAsync();

        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new DeleteFileParams
                {
                    File = fxFile.CreateReceipt!.File,
                },
                Payer = fxPayer,
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<TransactionException>();
        await Assert.That(((TransactionException)tex!).Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    }
}
