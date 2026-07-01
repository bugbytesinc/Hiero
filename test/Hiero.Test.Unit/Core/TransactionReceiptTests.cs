// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf.Collections;
using Proto;

namespace Hiero.Test.Unit.Core;

public class TransactionReceiptTests
{
    [Test]
    public async Task Create_Returns_Empty_Collection_For_No_Receipts()
    {
        var transactionId = new TransactionID(new TransactionId(new EntityId(0, 0, 1001), 100, 200));

        var result = TransactionReceiptExtensions.Create(transactionId, null, null, null);

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task Create_Materializes_Root_Child_And_Duplicate_Receipts()
    {
        var payer = new EntityId(0, 0, 1001);
        var transactionId = new TransactionID(new TransactionId(payer, 100, 200));
        var rootReceipt = new Proto.TransactionReceipt { Status = ResponseCodeEnum.Success };
        var childReceipts = new RepeatedField<Proto.TransactionReceipt>();
        childReceipts.Add(new Proto.TransactionReceipt { Status = ResponseCodeEnum.Success });
        childReceipts.Add(new Proto.TransactionReceipt { Status = ResponseCodeEnum.Success });
        var duplicateReceipts = new RepeatedField<Proto.TransactionReceipt>();
        duplicateReceipts.Add(new Proto.TransactionReceipt { Status = ResponseCodeEnum.DuplicateTransaction });

        var result = TransactionReceiptExtensions.Create(transactionId, rootReceipt, childReceipts, duplicateReceipts);

        await Assert.That(result.Count).IsEqualTo(4);
        await Assert.That(result[0].TransactionId.ChildNonce).IsEqualTo(0);
        await Assert.That(result[1].TransactionId.ChildNonce).IsEqualTo(1);
        await Assert.That(result[2].TransactionId.ChildNonce).IsEqualTo(2);
        await Assert.That(result[3].TransactionId.ChildNonce).IsEqualTo(0);
        await Assert.That(result[3].Status).IsEqualTo(ResponseCode.DuplicateTransaction);
    }
}
