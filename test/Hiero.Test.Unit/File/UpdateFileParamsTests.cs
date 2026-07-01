// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Hiero.Test.Helpers;
using Proto;

namespace Hiero.Test.Unit.File;

public class UpdateFileParamsTests
{
    [Test]
    public async Task CreateNetworkTransaction_Maps_File_Contents_Keys_Memo_And_Expiration()
    {
        var file = new EntityId(0, 0, 1001);
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var contents = new byte[] { 0x01, 0x02, 0x03 };
        var expiration = new ConsensusTimeStamp(1_700_000_000, 123);
        var body = CreateBody(new UpdateFileParams
        {
            File = file,
            Contents = contents,
            Endorsements = [new Endorsement(publicKey)],
            Memo = "memo",
            Expiration = expiration
        });

        await Assert.That(body.FileID.AsAddress()).IsEqualTo(file);
        await Assert.That(body.Contents.ToByteArray()).IsEquivalentTo(contents);
        await Assert.That(body.Keys.Keys.Count).IsEqualTo(1);
        await Assert.That(body.Memo).IsEqualTo("memo");
        await Assert.That(body.ExpirationTime.ToConsensusTimeStamp()).IsEqualTo(expiration);
    }

    [Test]
    public async Task CreateNetworkTransaction_Rejects_Blank_Update()
    {
        var parameters = new UpdateFileParams
        {
            File = new EntityId(0, 0, 1001)
        };

        var ex = Assert.Throws<ArgumentException>(() => CreateBody(parameters));
        await Assert.That(ex).IsNotNull();
    }

    private static FileUpdateTransactionBody CreateBody(INetworkParams<TransactionReceipt> parameters)
    {
        return (FileUpdateTransactionBody)parameters.CreateNetworkTransaction();
    }
}
