// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;

namespace Hiero.Test.Unit.File;

public class AppendFileParamsTests
{
    [Test]
    public async Task CreateNetworkTransaction_Maps_File_And_Contents()
    {
        var file = new EntityId(0, 0, 1001);
        var contents = new byte[] { 0x01, 0x02, 0x03 };
        var body = CreateBody(new AppendFileParams
        {
            File = file,
            Contents = contents
        });

        await Assert.That(body.FileID.AsAddress()).IsEqualTo(file);
        await Assert.That(body.Contents.ToByteArray()).IsEquivalentTo(contents);
    }

    private static FileAppendTransactionBody CreateBody(INetworkParams<TransactionReceipt> parameters)
    {
        return (FileAppendTransactionBody)parameters.CreateNetworkTransaction();
    }
}
