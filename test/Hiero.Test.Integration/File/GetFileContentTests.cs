using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.File;

public class GetFileContentTests
{
    [Test]
    public async Task Can_Get_File_Content()
    {
        await using var test = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var retrievedContents = await client.GetFileContentAsync(test.CreateReceipt!.File);
        await Assert.That(retrievedContents.ToArray()).IsEquivalentTo(test.CreateParams.Contents.ToArray(), TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task Get_File_Content_Requires_A_Fee()
    {
        await using var test = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var txId = client.CreateNewTransactionId();
        var contents = await client.GetFileContentAsync(test.CreateReceipt!.File, default, ctx => ctx.TransactionId = txId);
        var record = await client.GetTransactionRecordAsync(txId);
        await Assert.That(record.Transfers[TestNetwork.Payer] < 0).IsTrue();
    }
}
