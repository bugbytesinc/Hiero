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

    [Test]
    public async Task A_Spender_Revokes_Its_Approval_For_All_With_An_Approve_Allowance()
    {
        var token = new EntityId(0, 0, 2001);
        var owner = new EntityId(0, 0, 1001);
        var spender = new EntityId(0, 0, 1002);
        var body = (CryptoApproveAllowanceTransactionBody)((INetworkParams<TransactionReceipt>)new RevokeNftAllowanceParams
        {
            Token = token,
            Owner = owner,
            Spender = spender
        }).CreateNetworkTransaction();

        await Assert.That(body.NftAllowances.Count).IsEqualTo(1);
        var allowance = body.NftAllowances[0];
        await Assert.That(allowance.TokenId.AsAddress()).IsEqualTo(token);
        await Assert.That(allowance.Owner.AsAddress()).IsEqualTo(owner);
        await Assert.That(allowance.Spender.AsAddress()).IsEqualTo(spender);
        // Present and false: an absent value would leave the approval as it is.
        await Assert.That(allowance.ApprovedForAll).IsNotNull();
        await Assert.That(allowance.ApprovedForAll!.Value).IsFalse();
        await Assert.That(allowance.SerialNumbers.Count).IsEqualTo(0);
        await Assert.That(body.CryptoAllowances.Count + body.TokenAllowances.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Serial_Numbers_And_A_Spender_Together_Are_Rejected()
    {
        var revoke = new RevokeNftAllowanceParams
        {
            Token = new EntityId(0, 0, 2001),
            Owner = new EntityId(0, 0, 1001),
            Spender = new EntityId(0, 0, 1002),
            SerialNumbers = [1]
        };
        var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await Task.Run(() => ((INetworkParams<TransactionReceipt>)revoke).CreateNetworkTransaction()));
        await Assert.That(ex!.ParamName).IsEqualTo("Spender");
    }

    [Test]
    public async Task A_Granted_Approval_For_All_Reads_Back_With_Null_Serial_Numbers()
    {
        var granted = new GrantedNftAllowance { TokenId = new TokenID(new EntityId(0, 0, 2001)), Spender = new AccountID(new EntityId(0, 0, 1002)) };
        var allowance = new NftAllowance(granted, new EntityId(0, 0, 1001));
        await Assert.That(allowance.SerialNumbers).IsNull();
        await Assert.That(allowance.Spender).IsEqualTo(new EntityId(0, 0, 1002));
    }

    private static CryptoDeleteAllowanceTransactionBody CreateBody(INetworkParams<TransactionReceipt> parameters)
    {
        return (CryptoDeleteAllowanceTransactionBody)parameters.CreateNetworkTransaction();
    }
}
