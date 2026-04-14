using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.File;

// NOTE: All tests in this class require the configured Payer key to have admin
// rights over both the System Delete Administrator account (Hedera 0.0.59) and
// the System Undelete Administrator account (Hedera 0.0.60). Without those
// rights, the network returns AUTHORIZATION_FAILED/NOT_SUPPORTED and the tests
// will fail at runtime. Tests are marked [Skip] until such a configuration is
// available. When running against a privileged environment, remove the [Skip]
// attribute.

public class SystemRestoreFileTests
{
    [Test]
    [Skip("Requires System Delete/Undelete Administrator accounts (elevated privileges not available)")]
    public async Task Can_System_Restore_A_File()
    {
        await using var fxFile = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var deleteAddress = TestNetwork.SystemDeleteAdminAddress;
        var restoreAddress = TestNetwork.SystemUndeleteAdminAddress;

        await client.SystemDeleteFileAsync(
            new SystemDeleteFileParams { File = fxFile.CreateReceipt!.File },
            ctx => ctx.Payer = deleteAddress);

        var receipt = await client.SystemRestoreFileAsync(
            new SystemRestoreFileParams { File = fxFile.CreateReceipt.File },
            ctx => ctx.Payer = restoreAddress);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.TransactionId.Payer).IsEqualTo(restoreAddress);

        var info = await client.GetFileInfoAsync(fxFile.CreateReceipt.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(fxFile.CreateReceipt.File);
        await Assert.That(info.Size).IsEqualTo(fxFile.CreateParams.Contents.Length);
        await Assert.That(info.Expiration).IsEqualTo(fxFile.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEquivalentTo(new Endorsement[] { fxFile.PublicKey }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(info.Deleted).IsFalse();
    }

    [Test]
    [Skip("Requires System Delete/Undelete Administrator accounts (elevated privileges not available)")]
    public async Task Can_System_Restore_A_File_Using_Signatory()
    {
        await using var fxFile = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var deleteAddress = TestNetwork.SystemDeleteAdminAddress;
        var restoreAddress = TestNetwork.SystemUndeleteAdminAddress;

        await client.SystemDeleteFileAsync(
            new SystemDeleteFileParams { File = fxFile.CreateReceipt!.File },
            ctx => ctx.Payer = deleteAddress);

        var receipt = await client.SystemRestoreFileAsync(
            new SystemRestoreFileParams
            {
                File = fxFile.CreateReceipt.File,
                Signatory = TestNetwork.PrivateKey
            },
            ctx => ctx.Payer = restoreAddress);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.TransactionId.Payer).IsEqualTo(restoreAddress);

        var info = await client.GetFileInfoAsync(fxFile.CreateReceipt.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(fxFile.CreateReceipt.File);
        await Assert.That(info.Size).IsEqualTo(fxFile.CreateParams.Contents.Length);
        await Assert.That(info.Expiration).IsEqualTo(fxFile.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEquivalentTo(new Endorsement[] { fxFile.PublicKey }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(info.Deleted).IsFalse();
    }

    [Test]
    [Skip("Requires System Delete/Undelete Administrator accounts (elevated privileges not available)")]
    public async Task Can_System_Restore_A_File_And_Get_Record()
    {
        await using var fxFile = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var deleteAddress = TestNetwork.SystemDeleteAdminAddress;
        var restoreAddress = TestNetwork.SystemUndeleteAdminAddress;

        await client.SystemDeleteFileAsync(
            new SystemDeleteFileParams { File = fxFile.CreateReceipt!.File },
            ctx => ctx.Payer = deleteAddress);

        var receipt = await client.SystemRestoreFileAsync(
            new SystemRestoreFileParams { File = fxFile.CreateReceipt.File },
            ctx => ctx.Payer = restoreAddress);
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await AssertHg.NotEmptyAsync(record.Hash);
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee >= 0UL).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(restoreAddress);

        var info = await client.GetFileInfoAsync(fxFile.CreateReceipt.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(fxFile.CreateReceipt.File);
        await Assert.That(info.Size).IsEqualTo(fxFile.CreateParams.Contents.Length);
        await Assert.That(info.Expiration).IsEqualTo(fxFile.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEquivalentTo(new Endorsement[] { fxFile.PublicKey }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(info.Deleted).IsFalse();
    }

    [Test]
    [Skip("Requires System Delete/Undelete Administrator accounts (elevated privileges not available)")]
    public async Task Can_System_Restore_A_File_And_Get_Record_Using_Signatory()
    {
        await using var fxFile = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var deleteAddress = TestNetwork.SystemDeleteAdminAddress;
        var restoreAddress = TestNetwork.SystemUndeleteAdminAddress;

        await client.SystemDeleteFileAsync(
            new SystemDeleteFileParams { File = fxFile.CreateReceipt!.File },
            ctx => ctx.Payer = deleteAddress);

        var receipt = await client.SystemRestoreFileAsync(
            new SystemRestoreFileParams
            {
                File = fxFile.CreateReceipt.File,
                Signatory = TestNetwork.PrivateKey
            },
            ctx => ctx.Payer = restoreAddress);
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await AssertHg.NotEmptyAsync(record.Hash);
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee >= 0UL).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(restoreAddress);

        var info = await client.GetFileInfoAsync(fxFile.CreateReceipt.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(fxFile.CreateReceipt.File);
        await Assert.That(info.Size).IsEqualTo(fxFile.CreateParams.Contents.Length);
        await Assert.That(info.Expiration).IsEqualTo(fxFile.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEquivalentTo(new Endorsement[] { fxFile.PublicKey }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(info.Deleted).IsFalse();
    }

    [Test]
    [Skip("Requires System Delete/Undelete Administrator accounts (elevated privileges not available)")]
    public async Task Can_Not_Schedule_System_Restore()
    {
        await using var fxFile = await TestFile.CreateAsync();
        await using var fxPayer = await TestAccount.CreateAsync(a => a.CreateParams.InitialBalance = 20_00_000_000);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.DeleteFileAsync(new DeleteFileParams
        {
            File = fxFile.CreateReceipt!.File,
            Signatory = fxFile.PrivateKey
        });

        var ex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new SystemRestoreFileParams
            {
                File = fxFile.CreateReceipt.File
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
        await Assert.That(tex.Message).StartsWith("Unable to schedule transaction, status: ScheduledTransactionNotInWhitelist");
    }
}
