// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;

namespace Hiero.Test.Unit.Nft;

public class UpdateNftMetadataParamsTests
{
    [Test]
    public async Task CreateNetworkTransaction_Maps_Token_Serial_Numbers_And_Metadata()
    {
        var token = new EntityId(0, 0, 2001);
        var metadata = new byte[] { 0x01, 0x02, 0x03 };
        var body = CreateBody(new UpdateNftMetadataParams
        {
            Token = token,
            SerialNumbers = [1, 2, 3],
            Metadata = metadata
        });

        await Assert.That(body.Token.AsAddress()).IsEqualTo(token);
        await Assert.That(body.SerialNumbers.Count).IsEqualTo(3);
        await Assert.That(body.SerialNumbers[0]).IsEqualTo(1);
        await Assert.That(body.SerialNumbers[1]).IsEqualTo(2);
        await Assert.That(body.SerialNumbers[2]).IsEqualTo(3);
        await Assert.That(body.Metadata.ToByteArray()).IsEquivalentTo(metadata);
    }

    [Test]
    public async Task CreateNetworkTransaction_Rejects_Empty_Serial_Numbers()
    {
        var token = new EntityId(0, 0, 2001);
        var parameters = new UpdateNftMetadataParams
        {
            Token = token,
            SerialNumbers = [],
            Metadata = new byte[] { 0x01 }
        };

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateBody(parameters));
        await Assert.That(ex).IsNotNull();
    }

    private static TokenUpdateNftsTransactionBody CreateBody(INetworkParams<TransactionReceipt> parameters)
    {
        return (TokenUpdateNftsTransactionBody)parameters.CreateNetworkTransaction();
    }
}
