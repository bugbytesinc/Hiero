// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;

namespace Hiero.Test.Unit.Crypto;

public class AllowanceParamsTests
{
    [Test]
    public async Task CreateNetworkTransaction_Maps_Crypto_Allowances()
    {
        var owner = new EntityId(0, 0, 1001);
        var spender = new EntityId(0, 0, 1002);
        var body = CreateBody(new AllowanceParams
        {
            CryptoAllowances =
            [
                new CryptoAllowance(owner, spender, 100)
            ]
        });

        await Assert.That(body.CryptoAllowances.Count).IsEqualTo(1);
        await Assert.That(body.CryptoAllowances[0].Owner.AsAddress()).IsEqualTo(owner);
        await Assert.That(body.CryptoAllowances[0].Spender.AsAddress()).IsEqualTo(spender);
        await Assert.That(body.CryptoAllowances[0].Amount).IsEqualTo(100);
    }

    [Test]
    public async Task CreateNetworkTransaction_Maps_Token_Allowances()
    {
        var token = new EntityId(0, 0, 2001);
        var owner = new EntityId(0, 0, 1001);
        var spender = new EntityId(0, 0, 1002);
        var body = CreateBody(new AllowanceParams
        {
            TokenAllowances =
            [
                new TokenAllowance(token, owner, spender, 100)
            ]
        });

        await Assert.That(body.TokenAllowances.Count).IsEqualTo(1);
        await Assert.That(body.TokenAllowances[0].TokenId.AsAddress()).IsEqualTo(token);
        await Assert.That(body.TokenAllowances[0].Owner.AsAddress()).IsEqualTo(owner);
        await Assert.That(body.TokenAllowances[0].Spender.AsAddress()).IsEqualTo(spender);
        await Assert.That(body.TokenAllowances[0].Amount).IsEqualTo(100);
    }

    [Test]
    public async Task CreateNetworkTransaction_Maps_Nft_Serial_Allowances()
    {
        var token = new EntityId(0, 0, 2001);
        var owner = new EntityId(0, 0, 1001);
        var spender = new EntityId(0, 0, 1002);
        var delegatingSpender = new EntityId(0, 0, 1003);
        var body = CreateBody(new AllowanceParams
        {
            NftAllowances =
            [
                new NftAllowance(token, owner, spender, [1, 2], delegatingSpender)
            ]
        });

        await Assert.That(body.NftAllowances.Count).IsEqualTo(1);
        await Assert.That(body.NftAllowances[0].TokenId.AsAddress()).IsEqualTo(token);
        await Assert.That(body.NftAllowances[0].Owner.AsAddress()).IsEqualTo(owner);
        await Assert.That(body.NftAllowances[0].Spender.AsAddress()).IsEqualTo(spender);
        await Assert.That(body.NftAllowances[0].DelegatingSpender.AsAddress()).IsEqualTo(delegatingSpender);
        await Assert.That(body.NftAllowances[0].SerialNumbers.Count).IsEqualTo(2);
        await Assert.That(body.NftAllowances[0].ApprovedForAll).IsNull();
    }

    [Test]
    public async Task CreateNetworkTransaction_Maps_Nft_Approved_For_All_Allowances()
    {
        var token = new EntityId(0, 0, 2001);
        var owner = new EntityId(0, 0, 1001);
        var spender = new EntityId(0, 0, 1002);
        var body = CreateBody(new AllowanceParams
        {
            NftAllowances =
            [
                new NftAllowance(token, owner, spender)
            ]
        });

        await Assert.That(body.NftAllowances.Count).IsEqualTo(1);
        await Assert.That(body.NftAllowances[0].TokenId.AsAddress()).IsEqualTo(token);
        await Assert.That(body.NftAllowances[0].ApprovedForAll).IsTrue();
        await Assert.That(body.NftAllowances[0].SerialNumbers.Count).IsEqualTo(0);
    }

    private static CryptoApproveAllowanceTransactionBody CreateBody(INetworkParams<TransactionReceipt> parameters)
    {
        return (CryptoApproveAllowanceTransactionBody)parameters.CreateNetworkTransaction();
    }
}
