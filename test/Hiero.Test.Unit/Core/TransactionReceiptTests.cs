// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf.Collections;
using Proto;

namespace Hiero.Test.Unit.Core;

public class TransactionReceiptTests
{
    [Test]
    public async Task Create_Returns_Empty_Groups_For_No_Receipts()
    {
        var transactionId = new TransactionID(new TransactionId(new EntityId(0, 0, 1001), 100, 200));

        var result = TransactionReceiptExtensions.Create(transactionId, null, null, null);

        await Assert.That(result.Parent).IsNull();
        await Assert.That(result.Children).IsEmpty();
        await Assert.That(result.Duplicates).IsEmpty();
    }

    [Test]
    public async Task Create_Groups_Parent_Child_And_Duplicate_Receipts()
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

        // The parent keeps the submitted transaction ID; children get a
        // synthesized nonce; duplicates share the parent's ID.
        await Assert.That(result.Parent!.TransactionId.ChildNonce).IsEqualTo(0);
        await Assert.That(result.Children.Count).IsEqualTo(2);
        await Assert.That(result.Children[0].TransactionId.ChildNonce).IsEqualTo(1);
        await Assert.That(result.Children[1].TransactionId.ChildNonce).IsEqualTo(2);
        await Assert.That(result.Duplicates.Count).IsEqualTo(1);
        await Assert.That(result.Duplicates[0].TransactionId.ChildNonce).IsEqualTo(0);
        await Assert.That(result.Duplicates[0].Status).IsEqualTo(ResponseCode.DuplicateTransaction);
    }

    [Test]
    public async Task Create_Types_A_Registered_Node_Receipt()
    {
        // Regression: the HIP-1137 work added registered_node_id to the receipt
        // and a RegisteredNodeReceipt type, but never taught the sniffer about
        // it — so the id was unreachable through GetAllReceiptsAsync even though
        // CreateRegisteredNodeAsync returned it correctly.
        var transactionId = new TransactionID(new TransactionId(new EntityId(0, 0, 1001), 100, 200));
        var receipt = new Proto.TransactionReceipt
        {
            Status = ResponseCodeEnum.Success,
            RegisteredNodeId = 42
        };

        var result = TransactionReceiptExtensions.Create(transactionId, receipt, null, null);

        var typed = result.Parent as RegisteredNodeReceipt;
        await Assert.That(typed).IsNotNull();
        await Assert.That(typed!.RegisteredNodeId).IsEqualTo(42ul);
    }

    [Test]
    public async Task Create_Types_A_Registered_Node_Child_Receipt()
    {
        var transactionId = new TransactionID(new TransactionId(new EntityId(0, 0, 1001), 100, 200));
        var children = new RepeatedField<Proto.TransactionReceipt>
        {
            new Proto.TransactionReceipt { Status = ResponseCodeEnum.Success, RegisteredNodeId = 7 }
        };

        var result = TransactionReceiptExtensions.Create(transactionId, null, children, null);

        await Assert.That(result.Children.OfType<RegisteredNodeReceipt>().Single().RegisteredNodeId).IsEqualTo(7ul);
    }

    [Test]
    public async Task Create_Cannot_Type_A_Zero_Valued_Identifier()
    {
        // Documents a known limit rather than asserting desired behaviour.
        // node_id, registered_node_id and newTotalSupply are plain uint64 with
        // no protobuf presence, and proto3 omits zero from the wire entirely,
        // so "id 0" and "not set" are the same bytes.  A supply burned to zero
        // or a node created with id 0 therefore falls back to the base type.
        // Only carrying the transaction type could fix this — see
        // docs/api-shape-audit.md S-2/S-3/S-4.
        var transactionId = new TransactionID(new TransactionId(new EntityId(0, 0, 1001), 100, 200));

        var burnedToZero = TransactionReceiptExtensions.Create(
            transactionId,
            new Proto.TransactionReceipt { Status = ResponseCodeEnum.Success, NewTotalSupply = 0 },
            null,
            null);
        var nodeZero = TransactionReceiptExtensions.Create(
            transactionId,
            new Proto.TransactionReceipt { Status = ResponseCodeEnum.Success, NodeId = 0 },
            null,
            null);

        await Assert.That(burnedToZero.Parent!.GetType()).IsEqualTo(typeof(TransactionReceipt));
        await Assert.That(nodeZero.Parent!.GetType()).IsEqualTo(typeof(TransactionReceipt));

        // A non-zero supply still types correctly, so the branch itself works.
        var burnedToOne = TransactionReceiptExtensions.Create(
            transactionId,
            new Proto.TransactionReceipt { Status = ResponseCodeEnum.Success, NewTotalSupply = 1 },
            null,
            null);
        await Assert.That(burnedToOne.Parent).IsTypeOf<TokenReceipt>();
    }
}
