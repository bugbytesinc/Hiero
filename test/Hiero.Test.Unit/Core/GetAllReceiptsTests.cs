// SPDX-License-Identifier: Apache-2.0
using Grpc.Core;
using Hiero.Test.Unit.BlockNode;
using Proto;

namespace Hiero.Test.Unit.Core;

/// <summary>
/// GetAllReceiptsAsync reads an answer without a verdict the way
/// GetReceiptAsync does: an unknown outcome throws ConsensusException,
/// or, with ThrowIfNotSuccess off, comes back as a stand-in parent.
/// </summary>
public class GetAllReceiptsTests
{
    private const string ReceiptPath = "/proto.CryptoService/getTransactionReceipts";
    private static readonly TransactionId TxId = new(new EntityId(0, 0, 2), 1_700_000_000, 0);

    [Test]
    [Arguments(ResponseCodeEnum.ReceiptNotFound, ResponseCode.ReceiptNotFound)]
    [Arguments(ResponseCodeEnum.Busy, ResponseCode.Busy)]
    public async Task A_Node_With_No_Receipt_Raises_ConsensusException(ResponseCodeEnum header, ResponseCode expected)
    {
        var node = Answering(HeaderOnly(header));
        await using var client = CreateClient(node, throwIfNotSuccess: true);
        var ex = await Assert.ThrowsAsync<ConsensusException>(async () => await client.GetAllReceiptsAsync(TxId));
        await Assert.That(ex!.Status).IsEqualTo(expected);
        await Assert.That(ex.TransactionId).IsEqualTo(TxId);
    }

    [Test]
    [Arguments(ResponseCodeEnum.ReceiptNotFound, ResponseCode.ReceiptNotFound)]
    [Arguments(ResponseCodeEnum.Busy, ResponseCode.Unknown)]
    public async Task With_ThrowIfNotSuccess_Off_A_Stand_In_Parent_Carries_The_Answer(ResponseCodeEnum header, ResponseCode expected)
    {
        var node = Answering(HeaderOnly(header));
        await using var client = CreateClient(node, throwIfNotSuccess: false);
        var receipts = await client.GetAllReceiptsAsync(TxId);
        await Assert.That(receipts.Parent).IsNotNull();
        await Assert.That(receipts.Parent!.Status).IsEqualTo(expected);
        await Assert.That(receipts.Parent.TransactionId).IsEqualTo(TxId);
    }

    [Test]
    public async Task A_Receipt_Still_Unknown_When_The_Retries_Run_Out_Is_No_Verdict()
    {
        var node = Answering(WithReceipt(ResponseCodeEnum.Unknown));
        await using (var client = CreateClient(node, throwIfNotSuccess: true))
        {
            var ex = await Assert.ThrowsAsync<ConsensusException>(async () => await client.GetAllReceiptsAsync(TxId));
            await Assert.That(ex!.Status).IsEqualTo(ResponseCode.Unknown);
        }
        await using (var client = CreateClient(node, throwIfNotSuccess: false))
        {
            var receipts = await client.GetAllReceiptsAsync(TxId);
            await Assert.That(receipts.Parent!.Status).IsEqualTo(ResponseCode.Unknown);
        }
    }

    [Test]
    public async Task A_Failure_Verdict_Is_Returned_Whatever_The_Setting()
    {
        var node = Answering(WithReceipt(ResponseCodeEnum.InsufficientAccountBalance));
        await using var client = CreateClient(node, throwIfNotSuccess: true);
        var receipts = await client.GetAllReceiptsAsync(TxId);
        await Assert.That(receipts.Parent!.Status).IsEqualTo(ResponseCode.InsufficientAccountBalance);
    }

    private static Response HeaderOnly(ResponseCodeEnum code)
    {
        return new Response { TransactionGetReceipt = new TransactionGetReceiptResponse { Header = new ResponseHeader { NodeTransactionPrecheckCode = code } } };
    }

    private static Response WithReceipt(ResponseCodeEnum status)
    {
        return new Response
        {
            TransactionGetReceipt = new TransactionGetReceiptResponse
            {
                Header = new ResponseHeader { NodeTransactionPrecheckCode = ResponseCodeEnum.Ok },
                Receipt = new Proto.TransactionReceipt { Status = status }
            }
        };
    }

    private static FakeBlockNodeHandler Answering(Response response)
    {
        var node = new FakeBlockNodeHandler();
        node.Enqueue(ReceiptPath, StatusCode.OK, response);
        return node;
    }

    private static ConsensusClient CreateClient(FakeBlockNodeHandler node, bool throwIfNotSuccess)
    {
        return new ConsensusClient(_ => node.CreateChannel(), ctx =>
        {
            ctx.Endpoint = new ConsensusNodeEndpoint(new EntityId(0, 0, 3), new Uri("http://fake-node:50211"));
            ctx.Payer = new EntityId(0, 0, 2);
            ctx.RetryCount = 2;
            ctx.RetryDelay = TimeSpan.FromMilliseconds(1);
            ctx.ThrowIfNotSuccess = throwIfNotSuccess;
        });
    }
}
