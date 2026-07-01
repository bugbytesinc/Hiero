// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;

namespace Hiero.Test.Unit.Nft;

public class ConfiscateNftParamsTests
{
    [Test]
    public async Task CreateNetworkTransaction_Maps_Token_Holder_And_Serial_Numbers()
    {
        var token = new EntityId(0, 0, 2001);
        var holder = new EntityId(0, 0, 1001);
        var body = CreateBody(new ConfiscateNftParams
        {
            Token = token,
            Holder = holder,
            SerialNumbers = [1, 2, 3]
        });

        await Assert.That(body.Token.AsAddress()).IsEqualTo(token);
        await Assert.That(body.Account.AsAddress()).IsEqualTo(holder);
        await Assert.That(body.SerialNumbers.Count).IsEqualTo(3);
        await Assert.That(body.SerialNumbers[0]).IsEqualTo(1);
        await Assert.That(body.SerialNumbers[1]).IsEqualTo(2);
        await Assert.That(body.SerialNumbers[2]).IsEqualTo(3);
    }

    [Test]
    public async Task CreateNetworkTransaction_Preserves_Empty_Serial_Number_Behavior()
    {
        var token = new EntityId(0, 0, 2001);
        var holder = new EntityId(0, 0, 1001);
        var body = CreateBody(new ConfiscateNftParams
        {
            Token = token,
            Holder = holder,
            SerialNumbers = []
        });

        await Assert.That(body.SerialNumbers.Count).IsEqualTo(0);
    }

    private static TokenWipeAccountTransactionBody CreateBody(INetworkParams<TokenReceipt> parameters)
    {
        return (TokenWipeAccountTransactionBody)parameters.CreateNetworkTransaction();
    }
}
