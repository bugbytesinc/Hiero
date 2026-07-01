// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;

namespace Hiero.Test.Unit.Crypto;

public class RevokeNftAllowanceParamsTests
{
    [Test]
    public async Task CreateNetworkTransaction_Maps_Token_Owner_And_Serial_Numbers()
    {
        var token = new EntityId(0, 0, 2001);
        var owner = new EntityId(0, 0, 1001);
        var body = CreateBody(new RevokeNftAllowanceParams
        {
            Token = token,
            Owner = owner,
            SerialNumbers = [1, 2, 3]
        });

        await Assert.That(body.NftAllowances.Count).IsEqualTo(1);
        await Assert.That(body.NftAllowances[0].TokenId.AsAddress()).IsEqualTo(token);
        await Assert.That(body.NftAllowances[0].Owner.AsAddress()).IsEqualTo(owner);
        await Assert.That(body.NftAllowances[0].SerialNumbers.Count).IsEqualTo(3);
        await Assert.That(body.NftAllowances[0].SerialNumbers[0]).IsEqualTo(1);
        await Assert.That(body.NftAllowances[0].SerialNumbers[1]).IsEqualTo(2);
        await Assert.That(body.NftAllowances[0].SerialNumbers[2]).IsEqualTo(3);
    }

    [Test]
    public async Task CreateNetworkTransaction_Rejects_Empty_Serial_Numbers()
    {
        var token = new EntityId(0, 0, 2001);
        var owner = new EntityId(0, 0, 1001);
        var parameters = new RevokeNftAllowanceParams
        {
            Token = token,
            Owner = owner,
            SerialNumbers = []
        };

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => CreateBody(parameters));
        await Assert.That(ex).IsNotNull();
    }

    private static CryptoDeleteAllowanceTransactionBody CreateBody(INetworkParams<TransactionReceipt> parameters)
    {
        return (CryptoDeleteAllowanceTransactionBody)parameters.CreateNetworkTransaction();
    }
}
