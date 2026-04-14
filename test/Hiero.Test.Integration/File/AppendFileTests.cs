using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Text;

namespace Hiero.Test.Integration.File;

public class AppendFileContentTests
{
    [Test]
    public async Task Can_Append_To_File()
    {
        await using var test = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var appendedContent = Encoding.Unicode.GetBytes(Generator.Code(50));
        var concatenatedContent = test.CreateParams.Contents.ToArray().Concat(appendedContent).ToArray();

        var appendReceipt = await client.AppendFileAsync(new AppendFileParams
        {
            File = test.CreateReceipt!.File,
            Contents = appendedContent,
            Signatory = test.CreateParams.Signatory
        });
        await Assert.That(appendReceipt.Status).IsEqualTo(ResponseCode.Success);

        var newContent = await client.GetFileContentAsync(test.CreateReceipt.File);
        await Assert.That(newContent.ToArray()).IsEquivalentTo(concatenatedContent, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task Can_Append_To_File_Having_Extra_Signature()
    {
        await using var test = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var (publicKey, privateKey) = Generator.KeyPair();

        await client.UpdateFileAsync(new UpdateFileParams
        {
            File = test.CreateReceipt!.File,
            Endorsements = new[] { new Endorsement(publicKey) },
            Signatory = new Signatory(privateKey, test.CreateParams.Signatory!)
        });

        var appendedContent = Encoding.Unicode.GetBytes(Generator.Code(50));
        var concatenatedContent = test.CreateParams.Contents.ToArray().Concat(appendedContent).ToArray();

        var appendReceipt = await client.AppendFileAsync(new AppendFileParams
        {
            File = test.CreateReceipt.File,
            Contents = appendedContent,
            Signatory = privateKey
        });
        await Assert.That(appendReceipt.Status).IsEqualTo(ResponseCode.Success);

        var newContent = await client.GetFileContentAsync(test.CreateReceipt.File);
        await Assert.That(newContent.ToArray()).IsEquivalentTo(concatenatedContent, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task Appending_To_Deleted_File_Throws_Error()
    {
        await using var test = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var appendedContent = Encoding.Unicode.GetBytes(Generator.Code(50));

        var deleteReceipt = await client.DeleteFileAsync(new DeleteFileParams
        {
            File = test.CreateReceipt!.File,
            Signatory = test.CreateParams.Signatory
        });
        await Assert.That(deleteReceipt.Status).IsEqualTo(ResponseCode.Success);

        var ex = await Assert.That(async () =>
        {
            await client.AppendFileAsync(new AppendFileParams
            {
                File = test.CreateReceipt.File,
                Contents = appendedContent,
                Signatory = test.CreateParams.Signatory
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Message).StartsWith("Append File failed with status: FileDeleted");
    }

    [Test]
    public async Task Can_Not_Schedule_File_Append()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxFile = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var appendedContent = Encoding.Unicode.GetBytes(Generator.Code(50));

        var ex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new AppendFileParams
                {
                    File = fxFile.CreateReceipt!.File,
                    Contents = appendedContent,
                    Signatory = fxFile.CreateParams.Signatory
                },
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
        await Assert.That(tex.Message).StartsWith("Scheduling Append File failed with status: ScheduledTransactionNotInWhitelist");
    }

    [Test]
    public async Task Can_Not_Schedule_And_Sign_Append_File()
    {
        await using var fxFile = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var appendedContent = Encoding.Unicode.GetBytes(Generator.Code(50));

        var ex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new AppendFileParams
                {
                    File = fxFile.CreateReceipt!.File,
                    Contents = appendedContent,
                },
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    }
}
