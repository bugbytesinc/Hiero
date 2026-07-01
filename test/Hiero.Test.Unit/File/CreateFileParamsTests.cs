// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Hiero.Test.Helpers;
using Proto;

namespace Hiero.Test.Unit.File;

public class CreateFileParamsTests
{
    [Test]
    public async Task CreateNetworkTransaction_Maps_Contents_Keys_Memo_And_Expiration()
    {
        var (publicKey, _) = Generator.Ed25519KeyPair();
        var contents = new byte[] { 0x01, 0x02, 0x03 };
        var expiration = new ConsensusTimeStamp(1_700_000_000, 123);
        var body = CreateBody(new CreateFileParams
        {
            Expiration = expiration,
            Endorsements = [new Endorsement(publicKey)],
            Contents = contents,
            Memo = "memo"
        });

        await Assert.That(body.Contents.ToByteArray()).IsEquivalentTo(contents);
        await Assert.That(body.Keys.Keys.Count).IsEqualTo(1);
        await Assert.That(body.Memo).IsEqualTo("memo");
        await Assert.That(body.ExpirationTime.ToConsensusTimeStamp()).IsEqualTo(expiration);
    }

    private static FileCreateTransactionBody CreateBody(INetworkParams<FileReceipt> parameters)
    {
        return (FileCreateTransactionBody)parameters.CreateNetworkTransaction();
    }
}
