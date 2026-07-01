// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;

namespace Hiero.Test.Unit.Nft;

public class BurnNftParamsTests
{
    [Test]
    public async Task CreateNetworkTransaction_Maps_Token_And_Serial_Numbers()
    {
        var token = new EntityId(0, 0, 2001);
        var body = CreateBody(new BurnNftParams
        {
            Token = token,
            SerialNumbers = [1, 2, 3]
        });

        await Assert.That(body.Token.AsAddress()).IsEqualTo(token);
        await Assert.That(body.SerialNumbers.Count).IsEqualTo(3);
        await Assert.That(body.SerialNumbers[0]).IsEqualTo(1);
        await Assert.That(body.SerialNumbers[1]).IsEqualTo(2);
        await Assert.That(body.SerialNumbers[2]).IsEqualTo(3);
    }

    [Test]
    public async Task CreateNetworkTransaction_Rejects_Empty_Serial_Numbers()
    {
        var token = new EntityId(0, 0, 2001);
        var parameters = new BurnNftParams
        {
            Token = token,
            SerialNumbers = []
        };

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateBody(parameters));
        await Assert.That(ex).IsNotNull();
    }

    private static TokenBurnTransactionBody CreateBody(INetworkParams<TokenReceipt> parameters)
    {
        return (TokenBurnTransactionBody)parameters.CreateNetworkTransaction();
    }
}
