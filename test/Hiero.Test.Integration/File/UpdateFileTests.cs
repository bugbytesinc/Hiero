using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;
using System.Text;

namespace Hiero.Test.Integration.File;

public class UpdateFileTests
{
    [Test]
    public async Task Can_Update_File_Key()
    {
        await using var test = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        var updateReceipt = await client.UpdateFileAsync(new UpdateFileParams
        {
            File = test.CreateReceipt!.File,
            Endorsements = new Endorsement[] { newPublicKey },
            Signatory = new Signatory(TestNetwork.PrivateKey, test.PrivateKey, newPrivateKey)
        });
        await Assert.That(updateReceipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetFileInfoAsync(test.CreateReceipt.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(test.CreateReceipt.File);
        await Assert.That(info.Size).IsEqualTo(test.CreateParams.Contents.Length);
        await Assert.That(info.Expiration).IsEqualTo(test.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEquivalentTo(new Endorsement[] { newPublicKey });
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Update_File_Key_To_Empty()
    {
        await using var test = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateReceipt = await client.UpdateFileAsync(new UpdateFileParams
        {
            File = test.CreateReceipt!.File,
            Endorsements = Array.Empty<Endorsement>(),
            Signatory = test.PrivateKey
        });
        await Assert.That(updateReceipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetFileInfoAsync(test.CreateReceipt.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(test.CreateReceipt.File);
        await Assert.That(info.Size).IsEqualTo(test.CreateParams.Contents.Length);
        await Assert.That(info.Expiration).IsEqualTo(test.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Not_Update_File_Key_From_Empty()
    {
        await using var test = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();

        var updateReceipt = await client.UpdateFileAsync(new UpdateFileParams
        {
            File = test.CreateReceipt!.File,
            Endorsements = Array.Empty<Endorsement>(),
            Signatory = test.PrivateKey
        });
        await Assert.That(updateReceipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetFileInfoAsync(test.CreateReceipt.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(test.CreateReceipt.File);
        await Assert.That(info.Size).IsEqualTo(test.CreateParams.Contents.Length);
        await Assert.That(info.Expiration).IsEqualTo(test.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var ex = await Assert.That(async () =>
        {
            await client.UpdateFileAsync(new UpdateFileParams
            {
                File = test.CreateReceipt.File,
                Endorsements = new[] { new Endorsement(newPublicKey) },
                Signatory = newPrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.Unauthorized);

        info = await client.GetFileInfoAsync(test.CreateReceipt.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(test.CreateReceipt.File);
        await Assert.That(info.Size).IsEqualTo(test.CreateParams.Contents.Length);
        await Assert.That(info.Expiration).IsEqualTo(test.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Replace_File_Contents()
    {
        await using var test = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var newContents = Encoding.Unicode.GetBytes("Hello Again Hashgraph " + Generator.Code(50));

        var updateReceipt = await client.UpdateFileAsync(new UpdateFileParams
        {
            File = test.CreateReceipt!.File,
            Contents = newContents,
            Signatory = test.CreateParams.Signatory
        });
        await Assert.That(updateReceipt.Status).IsEqualTo(ResponseCode.Success);

        var retrievedContents = await client.GetFileContentAsync(test.CreateReceipt.File);
        await Assert.That(retrievedContents.ToArray()).IsEquivalentTo(newContents);
    }

    [Test]
    public async Task Can_Update_Memo()
    {
        await using var test = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var newMemo = Generator.Memo(30);

        var updateReceipt = await client.UpdateFileAsync(new UpdateFileParams
        {
            File = test.CreateReceipt!.File,
            Memo = newMemo,
            Signatory = test.CreateParams.Signatory
        });
        await Assert.That(updateReceipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetFileInfoAsync(test.CreateReceipt.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(test.CreateReceipt.File);
        await Assert.That(info.Memo).IsEqualTo(newMemo);
        await Assert.That(info.Size).IsEqualTo(test.CreateParams.Contents.Length);
        await Assert.That(info.Expiration).IsEqualTo(test.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEquivalentTo(new Endorsement[] { test.PublicKey });
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Update_Memo_To_Empty()
    {
        await using var test = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateReceipt = await client.UpdateFileAsync(new UpdateFileParams
        {
            File = test.CreateReceipt!.File,
            Memo = string.Empty,
            Signatory = test.CreateParams.Signatory
        });
        await Assert.That(updateReceipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetFileInfoAsync(test.CreateReceipt.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(test.CreateReceipt.File);
        await Assert.That(info.Memo).IsEmpty();
        await Assert.That(info.Size).IsEqualTo(test.CreateParams.Contents.Length);
        await Assert.That(info.Expiration).IsEqualTo(test.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEquivalentTo(new Endorsement[] { test.PublicKey });
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Cannot_Update_Contents_Of_Deleted_File()
    {
        await using var test = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var deleteReceipt = await client.DeleteFileAsync(new DeleteFileParams
        {
            File = test.CreateReceipt!.File,
            Signatory = test.CreateParams.Signatory
        });
        await Assert.That(deleteReceipt.Status).IsEqualTo(ResponseCode.Success);

        var ex = await Assert.That(async () =>
        {
            await client.UpdateFileAsync(new UpdateFileParams
            {
                File = test.CreateReceipt.File,
                Contents = Encoding.Unicode.GetBytes("Hello Again Hashgraph " + Generator.Code(50)),
                Signatory = test.CreateParams.Signatory
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Message).StartsWith("File Update failed with status: FileDeleted");
    }

    [Test]
    public async Task Can_Update_File_After_Key_Rotation()
    {
        await using var test = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var (newPublicKey1, newPrivateKey1) = Generator.KeyPair();
        var (newPublicKey2, newPrivateKey2) = Generator.KeyPair();
        var updateReceipt = await client.UpdateFileAsync(new UpdateFileParams
        {
            File = test.CreateReceipt!.File,
            Endorsements = new Endorsement[] { newPublicKey1, newPublicKey2 },
            Signatory = new Signatory(TestNetwork.PrivateKey, test.PrivateKey, newPrivateKey1, newPrivateKey2)
        });
        await Assert.That(updateReceipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetFileInfoAsync(test.CreateReceipt.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(test.CreateReceipt.File);
        await Assert.That(info.Size).IsEqualTo(test.CreateParams.Contents.Length);
        await Assert.That(info.Expiration).IsEqualTo(test.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEquivalentTo(new Endorsement[] { newPublicKey1, newPublicKey2 });
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var newContents = Encoding.Unicode.GetBytes("Hello Again Hashgraph " + Generator.Code(50));

        // Should fail with old file key
        var ex = await Assert.That(async () =>
        {
            await client.UpdateFileAsync(new UpdateFileParams
            {
                File = test.CreateReceipt.File,
                Contents = newContents,
                Signatory = test.CreateParams.Signatory
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("File Update failed with status: InvalidSignature");

        // Should fail with only new private key one
        ex = await Assert.That(async () =>
        {
            await client.UpdateFileAsync(new UpdateFileParams
            {
                File = test.CreateReceipt.File,
                Contents = newContents,
                Signatory = newPrivateKey1
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("File Update failed with status: InvalidSignature");

        // Should fail with only new private key two
        ex = await Assert.That(async () =>
        {
            await client.UpdateFileAsync(new UpdateFileParams
            {
                File = test.CreateReceipt.File,
                Contents = newContents,
                Signatory = newPrivateKey2
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("File Update failed with status: InvalidSignature");

        // Both new keys are required to update
        var updateContentReceipt = await client.UpdateFileAsync(new UpdateFileParams
        {
            File = test.CreateReceipt.File,
            Contents = newContents,
            Signatory = new Signatory(newPrivateKey1, newPrivateKey2)
        });
        await Assert.That(updateContentReceipt.Status).IsEqualTo(ResponseCode.Success);

        var retrievedContents = await client.GetFileContentAsync(test.CreateReceipt.File);
        await Assert.That(retrievedContents.ToArray()).IsEquivalentTo(newContents, TUnit.Assertions.Enums.CollectionOrdering.Matching);

        // Only one key in the list is required to delete
        var deleteReceipt = await client.DeleteFileAsync(new DeleteFileParams
        {
            File = test.CreateReceipt.File,
            Signatory = newPrivateKey1
        });
        await Assert.That(deleteReceipt).IsNotNull();
        await Assert.That(deleteReceipt.Status).IsEqualTo(ResponseCode.Success);

        // Confirm file is deleted
        info = await client.GetFileInfoAsync(test.CreateReceipt.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(test.CreateReceipt.File);
        await Assert.That(info.Size).IsEqualTo(0);
        await Assert.That(info.Expiration).IsEqualTo(test.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEquivalentTo(new Endorsement[] { newPublicKey1, newPublicKey2 });
        await Assert.That(info.Deleted).IsTrue();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Update_File_After_Key_Rotation_One_Of_Many()
    {
        await using var test = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var (newPublicKey1, newPrivateKey1) = Generator.KeyPair();
        var (newPublicKey2, newPrivateKey2) = Generator.KeyPair();
        var (newPublicKey3, newPrivateKey3) = Generator.KeyPair();
        var updateReceipt = await client.UpdateFileAsync(new UpdateFileParams
        {
            File = test.CreateReceipt!.File,
            Endorsements = new Endorsement[] { new Endorsement(1, newPublicKey1, newPublicKey2, newPublicKey3) },
            Signatory = new Signatory(TestNetwork.PrivateKey, test.PrivateKey, newPrivateKey1, newPrivateKey2, newPrivateKey3)
        });
        await Assert.That(updateReceipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetFileInfoAsync(test.CreateReceipt.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(test.CreateReceipt.File);
        await Assert.That(info.Size).IsEqualTo(test.CreateParams.Contents.Length);
        await Assert.That(info.Expiration).IsEqualTo(test.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEquivalentTo(new Endorsement[] { new Endorsement(1, newPublicKey1, newPublicKey2, newPublicKey3) });
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        // First key can change contents
        var newContents = Encoding.Unicode.GetBytes("Hello Again Hashgraph " + Generator.Code(50));
        var updateContentReceipt = await client.UpdateFileAsync(new UpdateFileParams
        {
            File = test.CreateReceipt.File,
            Contents = newContents,
            Signatory = newPrivateKey1
        });
        await Assert.That(updateContentReceipt.Status).IsEqualTo(ResponseCode.Success);
        var retrievedContents = await client.GetFileContentAsync(test.CreateReceipt.File);
        await Assert.That(retrievedContents.ToArray()).IsEquivalentTo(newContents, TUnit.Assertions.Enums.CollectionOrdering.Matching);

        // Second key can change contents
        newContents = Encoding.Unicode.GetBytes("Hello Again Hashgraph " + Generator.Code(50));
        updateContentReceipt = await client.UpdateFileAsync(new UpdateFileParams
        {
            File = test.CreateReceipt.File,
            Contents = newContents,
            Signatory = newPrivateKey2
        });
        await Assert.That(updateContentReceipt.Status).IsEqualTo(ResponseCode.Success);
        retrievedContents = await client.GetFileContentAsync(test.CreateReceipt.File);
        await Assert.That(retrievedContents.ToArray()).IsEquivalentTo(newContents, TUnit.Assertions.Enums.CollectionOrdering.Matching);

        // Third key can change contents
        newContents = Encoding.Unicode.GetBytes("Hello Again Hashgraph " + Generator.Code(50));
        updateContentReceipt = await client.UpdateFileAsync(new UpdateFileParams
        {
            File = test.CreateReceipt.File,
            Contents = newContents,
            Signatory = newPrivateKey3
        });
        await Assert.That(updateContentReceipt.Status).IsEqualTo(ResponseCode.Success);
        retrievedContents = await client.GetFileContentAsync(test.CreateReceipt.File);
        await Assert.That(retrievedContents.ToArray()).IsEquivalentTo(newContents, TUnit.Assertions.Enums.CollectionOrdering.Matching);

        // Only one signature needed to delete
        var deleteReceipt = await client.DeleteFileAsync(new DeleteFileParams
        {
            File = test.CreateReceipt.File,
            Signatory = newPrivateKey1
        });
        await Assert.That(deleteReceipt).IsNotNull();
        await Assert.That(deleteReceipt.Status).IsEqualTo(ResponseCode.Success);

        // Confirm file is deleted
        info = await client.GetFileInfoAsync(test.CreateReceipt.File);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.File).IsEqualTo(test.CreateReceipt.File);
        await Assert.That(info.Size).IsEqualTo(0);
        await Assert.That(info.Expiration).IsEqualTo(test.CreateParams.Expiration);
        await Assert.That(info.Endorsements).IsEquivalentTo(new Endorsement[] { new Endorsement(1, newPublicKey1, newPublicKey2, newPublicKey3) });
        await Assert.That(info.Deleted).IsTrue();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Schedule_File_Update()
    {
        await using var fxFile = await TestFile.CreateAsync();
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var client = await TestNetwork.CreateClientAsync();
        var newContents = Encoding.Unicode.GetBytes("Hello Again Hashgraph " + Generator.Code(50));

        var scheduledReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new UpdateFileParams
            {
                File = fxFile.CreateReceipt!.File,
                Contents = newContents,
                Signatory = fxFile.PrivateKey
            },
            Payer = fxPayer,
        });
        await Assert.That(scheduledReceipt.Status).IsEqualTo(ResponseCode.Success);

        var executionReceipt = await client.SignScheduleAsync(scheduledReceipt.Schedule, ctx =>
        {
            ctx.Payer = fxPayer;
            ctx.Signatory = fxPayer;
        });
        var pendingReceipt = await client.GetReceiptAsync(scheduledReceipt.ScheduledTransactionId);
        await Assert.That(pendingReceipt.Status).IsEqualTo(ResponseCode.Success);

        var retrievedContents = await client.GetFileContentAsync(fxFile.CreateReceipt.File);
        await Assert.That(retrievedContents.ToArray()).IsEquivalentTo(newContents, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Update_File()
    {
        await using var fxFile = await TestFile.CreateAsync();
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var client = await TestNetwork.CreateClientAsync();
        var newContents = Encoding.Unicode.GetBytes("Scheduled Update " + Generator.Code(20));

        var receipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new UpdateFileParams
            {
                File = fxFile.CreateReceipt!.File,
                Contents = newContents,
            },
            Payer = fxPayer,
        });
        await Assert.That(receipt.Schedule).IsNotEqualTo(EntityId.None);

        var signReceipt = await client.SignScheduleAsync(new SignScheduleParams
        {
            Schedule = receipt.Schedule,
            Signatory = new Signatory(fxFile.PrivateKey, fxPayer.PrivateKey),
        });
        await Assert.That(signReceipt.Status).IsEqualTo(ResponseCode.Success);
    }
}
