// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf.Collections;
using Proto;

namespace Hiero.Test.Unit.Core;

public class TransactionRecordTests
{
    [Test]
    public async Task Create_Returns_Empty_Collection_For_No_Records()
    {
        var records = TransactionRecordExtensions.Create(null, null, null);

        await Assert.That(records).IsEmpty();
    }

    [Test]
    public async Task Create_Materializes_Root_Child_And_Duplicate_Records()
    {
        var transactionId = new TransactionID(new TransactionId(new EntityId(0, 0, 1001), 100, 200));
        var rootRecord = new Proto.TransactionRecord
        {
            TransactionID = transactionId.Clone(),
            Receipt = new Proto.TransactionReceipt { Status = ResponseCodeEnum.Success }
        };
        var childRecords = new RepeatedField<Proto.TransactionRecord>();
        childRecords.Add(new Proto.TransactionRecord
        {
            TransactionID = transactionId.Clone(),
            Receipt = new Proto.TransactionReceipt { Status = ResponseCodeEnum.Success }
        });
        childRecords.Add(new Proto.TransactionRecord
        {
            TransactionID = transactionId.Clone(),
            Receipt = new Proto.TransactionReceipt { Status = ResponseCodeEnum.Success }
        });
        var duplicateRecords = new RepeatedField<Proto.TransactionRecord>();
        duplicateRecords.Add(new Proto.TransactionRecord
        {
            TransactionID = transactionId.Clone(),
            Receipt = new Proto.TransactionReceipt { Status = ResponseCodeEnum.DuplicateTransaction }
        });

        var records = TransactionRecordExtensions.Create(rootRecord, childRecords, duplicateRecords);

        await Assert.That(records.Count).IsEqualTo(4);
        await Assert.That(records[0].TransactionId.ChildNonce).IsEqualTo(0);
        await Assert.That(records[1].TransactionId.ChildNonce).IsEqualTo(1);
        await Assert.That(records[2].TransactionId.ChildNonce).IsEqualTo(2);
        await Assert.That(records[3].TransactionId.ChildNonce).IsEqualTo(0);
        await Assert.That(records[3].Status).IsEqualTo(ResponseCode.DuplicateTransaction);
    }

    [Test]
    public async Task AsStakingRewards_Returns_Empty_Dictionary_For_Empty_Rewards()
    {
        var rewards = new RepeatedField<AccountAmount>();

        var result = TransactionRecordExtensions.AsStakingRewards(rewards);

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task AsStakingRewards_Aggregates_Rewards_By_Account()
    {
        var accountOne = new EntityId(0, 0, 1001);
        var accountTwo = new EntityId(0, 0, 1002);
        var rewards = new RepeatedField<AccountAmount>();
        rewards.Add(new AccountAmount { AccountID = new AccountID(accountOne), Amount = 10 });
        rewards.Add(new AccountAmount { AccountID = new AccountID(accountOne), Amount = 15 });
        rewards.Add(new AccountAmount { AccountID = new AccountID(accountTwo), Amount = 20 });

        var result = TransactionRecordExtensions.AsStakingRewards(rewards);

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[accountOne]).IsEqualTo(25);
        await Assert.That(result[accountTwo]).IsEqualTo(20);
    }
}
