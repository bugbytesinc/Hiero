// SPDX-License-Identifier: Apache-2.0
using Proto;
using Google.Protobuf.Collections;

namespace Hiero.Test.Unit.Token;

public class RoyaltyTransferTests
{
    [Test]
    public async Task Constructor_Maps_Assessed_Custom_Fee()
    {
        var token = new EntityId(0, 0, 2001);
        var receiver = new EntityId(0, 0, 1001);
        var payerOne = new EntityId(0, 0, 1002);
        var payerTwo = new EntityId(0, 0, 1003);
        var fee = new AssessedCustomFee
        {
            TokenId = new TokenID(token),
            FeeCollectorAccountId = new AccountID(receiver),
            Amount = 100
        };
        fee.EffectivePayerAccountId.Add(new AccountID(payerOne));
        fee.EffectivePayerAccountId.Add(new AccountID(payerTwo));

        var transfer = new RoyaltyTransfer(fee);

        await Assert.That(transfer.Token).IsEqualTo(token);
        await Assert.That(transfer.Receiver).IsEqualTo(receiver);
        await Assert.That(transfer.Amount).IsEqualTo(100);
        await Assert.That(transfer.Payers.Count).IsEqualTo(2);
        await Assert.That(transfer.Payers[0]).IsEqualTo(payerOne);
        await Assert.That(transfer.Payers[1]).IsEqualTo(payerTwo);
    }

    [Test]
    public async Task Constructor_Uses_Empty_Payers_For_No_Effective_Payers()
    {
        var token = new EntityId(0, 0, 2001);
        var receiver = new EntityId(0, 0, 1001);
        var transfer = new RoyaltyTransfer(new AssessedCustomFee
        {
            TokenId = new TokenID(token),
            FeeCollectorAccountId = new AccountID(receiver),
            Amount = 100
        });

        await Assert.That(transfer.Payers).IsEmpty();
    }

    [Test]
    public async Task AsRoyaltyTransferList_Returns_Empty_Array_For_Empty_List()
    {
        var list = new RepeatedField<AssessedCustomFee>();

        var result = list.AsRoyaltyTransferList();

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task AsRoyaltyTransferList_Maps_Assessed_Custom_Fees()
    {
        var tokenOne = new EntityId(0, 0, 2001);
        var tokenTwo = new EntityId(0, 0, 2002);
        var receiverOne = new EntityId(0, 0, 1001);
        var receiverTwo = new EntityId(0, 0, 1002);
        var list = new RepeatedField<AssessedCustomFee>();
        list.Add(new AssessedCustomFee
        {
            TokenId = new TokenID(tokenOne),
            FeeCollectorAccountId = new AccountID(receiverOne),
            Amount = 100
        });
        list.Add(new AssessedCustomFee
        {
            TokenId = new TokenID(tokenTwo),
            FeeCollectorAccountId = new AccountID(receiverTwo),
            Amount = 200
        });

        var result = list.AsRoyaltyTransferList();

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0].Token).IsEqualTo(tokenOne);
        await Assert.That(result[0].Receiver).IsEqualTo(receiverOne);
        await Assert.That(result[0].Amount).IsEqualTo(100);
        await Assert.That(result[1].Token).IsEqualTo(tokenTwo);
        await Assert.That(result[1].Receiver).IsEqualTo(receiverTwo);
        await Assert.That(result[1].Amount).IsEqualTo(200);
    }
}
