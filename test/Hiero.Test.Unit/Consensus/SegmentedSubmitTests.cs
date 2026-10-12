// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Grpc.Core;
using Hiero.Test.Helpers;
using Hiero.Test.Unit.BlockNode;
using Proto;

namespace Hiero.Test.Unit.Consensus;

/// <summary>
/// The first segment of a segmented message embeds its own transaction id
/// in the message body; that id must be the one the caller configured.
/// </summary>
public class SegmentedSubmitTests
{
    private const string SubmitPath = "/proto.ConsensusService/submitMessage";
    private const string ReceiptPath = "/proto.CryptoService/getTransactionReceipts";
    private static readonly EntityId Topic = new(0, 0, 4001);

    [Test]
    public async Task The_First_Segment_Uses_The_Transaction_Id_The_Caller_Chose()
    {
        var (payer, signatory) = Account();
        var node = CreateNode();
        await using var client = CreateClient(node, payer, signatory);
        var chosen = client.CreateNewTransactionId();
        await client.SubmitMessageAsync(FirstSegment(), ctx => ctx.TransactionId = chosen);
        var body = SentBody(node);
        await Assert.That(body.TransactionID.AsTransactionId()).IsEqualTo(chosen);
        await Assert.That(body.ConsensusSubmitMessage.ChunkInfo.InitialTransactionID.AsTransactionId()).IsEqualTo(chosen);
    }

    [Test]
    public async Task The_First_Segment_Is_Paid_By_The_Configured_Payer()
    {
        var (rootPayer, rootSignatory) = Account();
        var (otherPayer, otherSignatory) = Account();
        var node = CreateNode();
        await using var client = CreateClient(node, rootPayer, rootSignatory);
        await client.SubmitMessageAsync(FirstSegment(), ctx =>
        {
            ctx.Payer = otherPayer;
            ctx.Signatory = otherSignatory;
        });
        var body = SentBody(node);
        await Assert.That(body.TransactionID.AsTransactionId().Payer).IsEqualTo(otherPayer);
        await Assert.That(body.ConsensusSubmitMessage.ChunkInfo.InitialTransactionID.AsTransactionId().Payer).IsEqualTo(otherPayer);
    }

    [Test]
    public async Task A_Large_Message_Uses_The_Chosen_Id_For_Its_First_Segment_Only()
    {
        var (payer, signatory) = Account();
        var node = CreateNode();
        await using var client = CreateClient(node, payer, signatory);
        var chosen = client.CreateNewTransactionId();
        var receipts = await client.SubmitLargeMessageAsync(Topic, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 3, null, ctx => ctx.TransactionId = chosen);

        var bodies = node.Requests(SubmitPath).Select(Body).ToArray();
        await Assert.That(bodies.Length).IsEqualTo(3);
        await Assert.That(bodies[0].TransactionID.AsTransactionId()).IsEqualTo(chosen);
        await Assert.That(receipts[0].TransactionId).IsEqualTo(chosen);
        // Every segment points back to the first, and none reuses its id.
        var ids = bodies.Select(b => b.TransactionID.AsTransactionId()).ToArray();
        await Assert.That(ids.Distinct().Count()).IsEqualTo(3);
        foreach (var body in bodies)
        {
            await Assert.That(body.ConsensusSubmitMessage.ChunkInfo.InitialTransactionID.AsTransactionId()).IsEqualTo(chosen);
        }
    }

    private static SubmitMessageParams FirstSegment()
    {
        return new SubmitMessageParams
        {
            Topic = Topic,
            Message = new byte[] { 1, 2, 3 },
            SegmentIndex = 1,
            TotalSegmentCount = 2
        };
    }

    private static (EntityId Payer, Signatory Signatory) Account()
    {
        var (_, privateKey) = Generator.Ed25519KeyPair();
        return (new EntityId(0, 0, Generator.Integer(1000, 100_000)), new Signatory(privateKey));
    }

    private static FakeBlockNodeHandler CreateNode()
    {
        var node = new FakeBlockNodeHandler();
        node.Enqueue(SubmitPath, StatusCode.OK, new TransactionResponse { NodeTransactionPrecheckCode = ResponseCodeEnum.Ok });
        node.Enqueue(ReceiptPath, StatusCode.OK, new Response
        {
            TransactionGetReceipt = new TransactionGetReceiptResponse
            {
                Header = new ResponseHeader { NodeTransactionPrecheckCode = ResponseCodeEnum.Ok },
                Receipt = new Proto.TransactionReceipt { Status = ResponseCodeEnum.Success, TopicSequenceNumber = 1 }
            }
        });
        return node;
    }

    private static ConsensusClient CreateClient(FakeBlockNodeHandler node, EntityId payer, Signatory signatory)
    {
        return new ConsensusClient(_ => node.CreateChannel(), ctx =>
        {
            ctx.Endpoint = new ConsensusNodeEndpoint(new EntityId(0, 0, 3), new Uri("http://fake-node:50211"));
            ctx.Payer = payer;
            ctx.Signatory = signatory;
            ctx.RetryDelay = TimeSpan.FromMilliseconds(1);
        });
    }

    private static TransactionBody SentBody(FakeBlockNodeHandler node)
    {
        return Body(node.Requests(SubmitPath)[0]);
    }

    private static TransactionBody Body(byte[] request)
    {
        var transaction = Transaction.Parser.ParseFrom(request);
        var signed = SignedTransaction.Parser.ParseFrom(transaction.SignedTransactionBytes);
        return TransactionBody.Parser.ParseFrom(signed.BodyBytes);
    }
}
