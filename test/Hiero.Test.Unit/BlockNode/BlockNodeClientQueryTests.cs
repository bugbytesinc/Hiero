// SPDX-License-Identifier: Apache-2.0
using Grpc.Core;
using Hiero.BlockNode;
using Org.Hiero.Block.Api;

namespace Hiero.Test.Unit.BlockNode;

public class BlockNodeClientQueryTests
{
    private const string ServerStatusPath = "/org.hiero.block.api.BlockNodeService/serverStatus";
    private const string ServerStatusDetailPath = "/org.hiero.block.api.BlockNodeService/serverStatusDetail";
    private const string GetBlockPath = "/org.hiero.block.api.BlockAccessService/getBlock";
    private const string GetProofPath = "/org.hiero.block.api.ProofService/getBlockContentsProof";
    private const string StateSnapshotPath = "/org.hiero.block.api.StateService/stateSnapshot";

    private static BlockNodeClient CreateClient(FakeBlockNodeHandler handler, Action<IBlockNodeContext>? configure = null)
    {
        return new BlockNodeClient(_ => handler.CreateChannel(), ctx =>
        {
            ctx.Uri = new Uri("http://fake-block-node:40840");
            ctx.RetryDelay = TimeSpan.FromMilliseconds(1);
            configure?.Invoke(ctx);
        });
    }

    [Test]
    public async Task GetServerStatus_Maps_Response()
    {
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(ServerStatusPath, StatusCode.OK, new ServerStatusResponse
        {
            FirstAvailableBlock = 7,
            LastAvailableBlock = 4200,
            OnlyLatestState = true
        });
        await using var client = CreateClient(handler);
        var status = await client.GetServerStatusAsync();
        await Assert.That(status.FirstAvailableBlockNumber).IsEqualTo(7ul);
        await Assert.That(status.LastAvailableBlockNumber).IsEqualTo(4200ul);
        await Assert.That(status.OnlyLatestState).IsTrue();
    }

    [Test]
    public async Task GetServerStatusDetail_Maps_Response()
    {
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(ServerStatusDetailPath, StatusCode.OK, new ServerStatusDetailResponse
        {
            VersionInformation = new BlockNodeVersions
            {
                BlockNodeVersion = new Proto.SemanticVersion { Major = 0, Minor = 37, Patch = 0 }
            }
        });
        await using var client = CreateClient(handler);
        var detail = await client.GetServerStatusDetailAsync();
        await Assert.That(detail.BlockNodeVersion).IsEqualTo(new SemanticVersion(0, 37, 0));
        await Assert.That(detail.TssData).IsNull();
    }

    [Test]
    public async Task GetBlock_Success_Returns_Block()
    {
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(GetBlockPath, StatusCode.OK, new BlockResponse
        {
            Status = BlockResponse.Types.Code.Success,
            Block = BlockDomainTests.CreateBlockProto(123)
        });
        await using var client = CreateClient(handler);
        var block = await client.GetBlockAsync(123);
        await Assert.That(block).IsNotNull();
        await Assert.That(block!.Number).IsEqualTo(123ul);
        await Assert.That(block.Proof).IsNotNull();
    }

    [Test]
    public async Task GetBlock_NotFound_Returns_Null()
    {
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(GetBlockPath, StatusCode.OK, new BlockResponse
        {
            Status = BlockResponse.Types.Code.NotFound
        });
        await using var client = CreateClient(handler);
        var block = await client.GetBlockAsync(999_999_999);
        await Assert.That(block).IsNull();
    }

    [Test]
    [Arguments(BlockResponse.Types.Code.NotAvailable, BlockNodeExceptionCode.NotAvailable)]
    [Arguments(BlockResponse.Types.Code.InvalidRequest, BlockNodeExceptionCode.InvalidRequest)]
    [Arguments(BlockResponse.Types.Code.Error, BlockNodeExceptionCode.ServerError)]
    [Arguments(BlockResponse.Types.Code.Unknown, BlockNodeExceptionCode.ServerError)]
    public async Task GetBlock_Fault_Codes_Throw(BlockResponse.Types.Code wireCode, BlockNodeExceptionCode expected)
    {
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(GetBlockPath, StatusCode.OK, new BlockResponse { Status = wireCode });
        await using var client = CreateClient(handler);
        var exception = await Assert.ThrowsAsync<BlockNodeException>(async () => await client.GetBlockAsync(1));
        await Assert.That(exception!.Code).IsEqualTo(expected);
    }

    [Test]
    public async Task GetLatestBlock_NotFound_Throws_NotAvailable()
    {
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(GetBlockPath, StatusCode.OK, new BlockResponse
        {
            Status = BlockResponse.Types.Code.NotFound
        });
        await using var client = CreateClient(handler);
        var exception = await Assert.ThrowsAsync<BlockNodeException>(async () => await client.GetLatestBlockAsync());
        await Assert.That(exception!.Code).IsEqualTo(BlockNodeExceptionCode.NotAvailable);
    }

    [Test]
    public async Task GetBlockContentsProof_Success_Maps_Proof()
    {
        var handler = new FakeBlockNodeHandler();
        var responseProof = new Org.Hiero.Block.Api.BlockContentsProof
        {
            BlockItem = BlockDomainTests.CreateTransactionItem([1, 2, 3]),
            BlockSignature = Google.Protobuf.ByteString.CopyFrom(BlockDomainTests.RandomHash())
        };
        handler.Enqueue(GetProofPath, StatusCode.OK, new BlockContentsProofResponse
        {
            Status = BlockContentsProofResponse.Types.Code.Success,
            BlockContentProof = responseProof
        });
        await using var client = CreateClient(handler);
        var proof = await client.GetBlockContentsProofAsync(new GetBlockContentsProofParams
        {
            BlockNumber = 55,
            ItemIndex = 2
        });
        await Assert.That(proof).IsNotNull();
        await Assert.That(proof!.Item.Kind).IsEqualTo(BlockItemKind.SignedTransaction);
    }

    [Test]
    public async Task GetBlockContentsProof_ItemNotFound_Returns_Null()
    {
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(GetProofPath, StatusCode.OK, new BlockContentsProofResponse
        {
            Status = BlockContentsProofResponse.Types.Code.ItemNotFound
        });
        await using var client = CreateClient(handler);
        var proof = await client.GetBlockContentsProofAsync(new GetBlockContentsProofParams
        {
            BlockNumber = 55,
            ItemHash = BlockDomainTests.RandomHash()
        });
        await Assert.That(proof).IsNull();
    }

    [Test]
    public async Task GetBlockContentsProof_Duplicate_Hash_Throws_InvalidRequest()
    {
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(GetProofPath, StatusCode.OK, new BlockContentsProofResponse
        {
            Status = BlockContentsProofResponse.Types.Code.DuplicateHashItemFound
        });
        await using var client = CreateClient(handler);
        var exception = await Assert.ThrowsAsync<BlockNodeException>(async () => await client.GetBlockContentsProofAsync(new GetBlockContentsProofParams
        {
            BlockNumber = 55,
            ItemHash = BlockDomainTests.RandomHash()
        }));
        await Assert.That(exception!.Code).IsEqualTo(BlockNodeExceptionCode.InvalidRequest);
    }

    [Test]
    public async Task GetBlockContentsProof_Requires_Exactly_One_Specifier()
    {
        var handler = new FakeBlockNodeHandler();
        await using var client = CreateClient(handler);
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await client.GetBlockContentsProofAsync(null!));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await client.GetBlockContentsProofAsync(new GetBlockContentsProofParams
        {
            BlockNumber = 1
        }));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await client.GetBlockContentsProofAsync(new GetBlockContentsProofParams
        {
            BlockNumber = 1,
            ItemHash = BlockDomainTests.RandomHash(),
            ItemIndex = 3
        }));
    }

    [Test]
    public async Task GetStateSnapshot_Success_Maps_Response()
    {
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(StateSnapshotPath, StatusCode.OK, new StateSnapshotResponse
        {
            Status = StateSnapshotResponse.Types.Code.Success,
            LastBlockNumber = 4100,
            SnapshotReference = "node://snapshots/4100"
        });
        await using var client = CreateClient(handler);
        var snapshot = await client.GetLatestStateSnapshotAsync();
        await Assert.That(snapshot.LastBlockNumber).IsEqualTo(4100ul);
        await Assert.That(snapshot.SnapshotReference).IsEqualTo("node://snapshots/4100");
    }

    [Test]
    public async Task GetStateSnapshot_InvalidRequest_Throws()
    {
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(StateSnapshotPath, StatusCode.OK, new StateSnapshotResponse
        {
            Status = StateSnapshotResponse.Types.Code.InvalidRequest
        });
        await using var client = CreateClient(handler);
        var exception = await Assert.ThrowsAsync<BlockNodeException>(async () => await client.GetStateSnapshotAsync(1));
        await Assert.That(exception!.Code).IsEqualTo(BlockNodeExceptionCode.InvalidRequest);
    }

    [Test]
    public async Task Transient_Unavailable_Is_Retried_Then_Succeeds()
    {
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(ServerStatusPath, StatusCode.Unavailable);
        handler.Enqueue(ServerStatusPath, StatusCode.Unavailable);
        handler.Enqueue(ServerStatusPath, StatusCode.OK, new ServerStatusResponse { LastAvailableBlock = 10 });
        await using var client = CreateClient(handler);
        var status = await client.GetServerStatusAsync();
        await Assert.That(status.LastAvailableBlockNumber).IsEqualTo(10ul);
        await Assert.That(handler.CallCount(ServerStatusPath)).IsEqualTo(3);
    }

    [Test]
    public async Task Unavailable_Exhausts_Retries_And_Throws()
    {
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(ServerStatusPath, StatusCode.Unavailable);
        await using var client = CreateClient(handler, ctx => ctx.RetryCount = 2);
        var exception = await Assert.ThrowsAsync<BlockNodeException>(async () => await client.GetServerStatusAsync());
        await Assert.That(exception!.Code).IsEqualTo(BlockNodeExceptionCode.Unavailable);
        await Assert.That(handler.CallCount(ServerStatusPath)).IsEqualTo(3);
    }

    [Test]
    public async Task NonTransient_Rpc_Failure_Is_Not_Retried()
    {
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(ServerStatusPath, StatusCode.PermissionDenied);
        await using var client = CreateClient(handler);
        var exception = await Assert.ThrowsAsync<BlockNodeException>(async () => await client.GetServerStatusAsync());
        await Assert.That(exception!.Code).IsEqualTo(BlockNodeExceptionCode.CommunicationError);
        await Assert.That(handler.CallCount(ServerStatusPath)).IsEqualTo(1);
    }

    [Test]
    [Arguments(StatusCode.Unimplemented)]
    [Arguments(StatusCode.NotFound)]
    public async Task Absent_Service_Reports_NotImplemented(StatusCode transportStatus)
    {
        // Nodes serve different subsets of the Block Node APIs (proof and
        // state-snapshot services are absent from many deployments), and an
        // unimplemented service surfaces at the transport level rather than
        // through a response status.
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(StateSnapshotPath, transportStatus);
        await using var client = CreateClient(handler);
        var exception = await Assert.ThrowsAsync<BlockNodeException>(async () => await client.GetLatestStateSnapshotAsync());
        await Assert.That(exception!.Code).IsEqualTo(BlockNodeExceptionCode.NotImplemented);
        // Not retried: a node that lacks the service will never grow it.
        await Assert.That(handler.CallCount(StateSnapshotPath)).IsEqualTo(1);
    }

    [Test]
    public async Task Missing_Uri_Throws_InvalidOperation()
    {
        await using var client = new BlockNodeClient();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.GetServerStatusAsync());
        await Assert.That(exception!.Message).Contains("Block Node URL");
    }

    [Test]
    public async Task OnSendingRequest_Hook_Observes_Request()
    {
        var handler = new FakeBlockNodeHandler();
        handler.Enqueue(GetBlockPath, StatusCode.OK, new BlockResponse
        {
            Status = BlockResponse.Types.Code.Success,
            Block = BlockDomainTests.CreateBlockProto(77)
        });
        Google.Protobuf.IMessage? observed = null;
        await using var client = CreateClient(handler, ctx => ctx.OnSendingRequest = message => observed = message);
        await client.GetBlockAsync(77);
        await Assert.That(observed).IsNotNull();
        await Assert.That(((BlockRequest)observed!).BlockNumber).IsEqualTo(77ul);
    }

    [Test]
    public async Task Status_Queries_Use_StatusUri_And_Other_Apis_Do_Not()
    {
        var mainNode = new FakeBlockNodeHandler();
        var statusNode = new FakeBlockNodeHandler();
        statusNode.Enqueue(ServerStatusPath, StatusCode.OK, new ServerStatusResponse { FirstAvailableBlock = 3, LastAvailableBlock = 9 });
        statusNode.Enqueue(ServerStatusDetailPath, StatusCode.OK, new ServerStatusDetailResponse());
        mainNode.Enqueue(GetBlockPath, StatusCode.OK, new BlockResponse { Status = BlockResponse.Types.Code.NotFound });
        await using var client = new BlockNodeClient(uri => uri.Port == 40982 ? statusNode.CreateChannel() : mainNode.CreateChannel(), ctx =>
        {
            ctx.Uri = new Uri("http://block-node:40981");
            ctx.StatusUri = new Uri("http://block-node:40982");
        });
        var status = await client.GetServerStatusAsync();
        await client.GetServerStatusDetailAsync();
        await client.GetBlockAsync(5);
        await Assert.That(status.FirstAvailableBlockNumber).IsEqualTo(3ul);
        await Assert.That(statusNode.CallCount(ServerStatusPath)).IsEqualTo(1);
        await Assert.That(statusNode.CallCount(ServerStatusDetailPath)).IsEqualTo(1);
        await Assert.That(statusNode.CallCount(GetBlockPath)).IsEqualTo(0);
        await Assert.That(mainNode.CallCount(GetBlockPath)).IsEqualTo(1);
    }
}
