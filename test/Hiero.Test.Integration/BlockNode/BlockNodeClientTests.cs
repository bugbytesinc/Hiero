// SPDX-License-Identifier: Apache-2.0
using Hiero.BlockNode;
using Hiero.Test.Integration.Fixtures;
using System.Threading.Channels;

namespace Hiero.Test.Integration.BlockNode;

[Category("Block Node")]
public class BlockNodeClientTests
{
    private static void RequireBlockNode()
    {
        if (TestNetwork.BlockNodeUri is null)
        {
            Skip.Test("Block Node Endpoint [BlockNodeEndpoint] is not configured.");
        }
    }

    [Test]
    public async Task Can_Get_Server_Status()
    {
        RequireBlockNode();
        await using var client = TestNetwork.CreateBlockNodeClient();
        var status = await client.GetServerStatusAsync();
        await Assert.That(status.LastAvailableBlockNumber).IsGreaterThanOrEqualTo(status.FirstAvailableBlockNumber);
    }

    [Test]
    public async Task Can_Get_Server_Status_Detail()
    {
        RequireBlockNode();
        await using var client = TestNetwork.CreateBlockNodeClient();
        var detail = await client.GetServerStatusDetailAsync();
        await Assert.That(detail.BlockNodeVersion).IsNotEqualTo(SemanticVersion.None);
        await Assert.That(detail.StreamProtoVersion).IsNotEqualTo(SemanticVersion.None);
    }

    [Test]
    public async Task Can_Get_Latest_Block()
    {
        RequireBlockNode();
        await using var client = TestNetwork.CreateBlockNodeClient();
        var status = await client.GetServerStatusAsync();
        var block = await client.GetLatestBlockAsync();
        await Assert.That(block.Number).IsGreaterThanOrEqualTo(status.LastAvailableBlockNumber);
        await Assert.That(block.Items.Count).IsGreaterThan(0);
        await Assert.That(block.Items[0].Kind).IsEqualTo(BlockItemKind.BlockHeader);
        await Assert.That(block.Footer).IsNotNull();
        await Assert.That(block.Proof).IsNotNull();
    }

    [Test]
    public async Task Can_Get_Block_By_Number()
    {
        RequireBlockNode();
        await using var client = TestNetwork.CreateBlockNodeClient();
        var latest = await client.GetLatestBlockAsync();
        var block = await client.GetBlockAsync(latest.Number);
        await Assert.That(block).IsNotNull();
        await Assert.That(block!.Number).IsEqualTo(latest.Number);
    }

    [Test]
    public async Task Beyond_Tip_Block_Is_Null_Or_Not_Available()
    {
        RequireBlockNode();
        await using var client = TestNetwork.CreateBlockNodeClient();
        var status = await client.GetServerStatusAsync();
        // The wire allows either NOT_FOUND (mapped to null) or
        // NOT_AVAILABLE (mapped to an exception carrying the range);
        // Solo answers NOT_AVAILABLE, other deployments may not.
        // (Absurd numbers like ulong.MaxValue draw INVALID_REQUEST
        // instead, so probe just beyond the tip.)
        try
        {
            var block = await client.GetBlockAsync(status.LastAvailableBlockNumber + 500_000);
            await Assert.That(block).IsNull();
        }
        catch (BlockNodeException ex)
        {
            await Assert.That(ex.Code).IsEqualTo(BlockNodeExceptionCode.NotAvailable);
        }
    }

    /// <summary>
    /// The proof service is optional — nodes advertise which Block Node
    /// APIs they serve, and many deployments (including Solo) omit it.
    /// Either a well-formed proof or a clean NotImplemented is correct;
    /// anything else is a defect.
    /// </summary>
    [Test]
    public async Task Block_Contents_Proof_Is_Well_Formed_Or_Unimplemented()
    {
        RequireBlockNode();
        await using var client = TestNetwork.CreateBlockNodeClient();
        var latest = await client.GetLatestBlockAsync();
        try
        {
            var proof = await client.GetBlockContentsProofAsync(new GetBlockContentsProofParams
            {
                BlockNumber = latest.Number - 1,
                ItemIndex = 0
            });
            if (proof is not null)
            {
                await Assert.That(proof.BlockSignature.IsEmpty).IsFalse();
                foreach (var sibling in proof.SiblingHashes)
                {
                    await Assert.That(sibling.Hash.IsEmpty).IsFalse();
                }
            }
        }
        catch (BlockNodeException ex) when (ex.Code == BlockNodeExceptionCode.NotImplemented)
        {
            Skip.Test("This block node does not implement the proof service.");
        }
    }

    /// <summary>
    /// The state service is likewise optional; snapshot references are an
    /// out-of-band locator, so only its presence can be asserted.
    /// </summary>
    [Test]
    public async Task State_Snapshot_Is_Well_Formed_Or_Unimplemented()
    {
        RequireBlockNode();
        await using var client = TestNetwork.CreateBlockNodeClient();
        try
        {
            var snapshot = await client.GetLatestStateSnapshotAsync();
            await Assert.That(snapshot.SnapshotReference).IsNotNull();
        }
        catch (BlockNodeException ex) when (ex.Code == BlockNodeExceptionCode.NotImplemented)
        {
            Skip.Test("This block node does not implement the state service.");
        }
    }

    [Test]
    public async Task Can_Subscribe_To_Historical_Block_Range()
    {
        RequireBlockNode();
        await using var client = TestNetwork.CreateBlockNodeClient();
        var status = await client.GetServerStatusAsync();
        if (status.LastAvailableBlockNumber < status.FirstAvailableBlockNumber + 10)
        {
            Skip.Test("The block node does not hold enough history for this test.");
        }
        var start = status.LastAvailableBlockNumber - 8;
        var channel = Channel.CreateUnbounded<Block>();
        var subscription = client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
        {
            StartingBlockNumber = start,
            EndingBlockNumber = start + 4,
            BlockWriter = channel.Writer
        });
        var blocks = new List<Block>();
        await foreach (var block in channel.Reader.ReadAllAsync())
        {
            blocks.Add(block);
        }
        await subscription;
        await Assert.That(blocks.Count).IsEqualTo(5);
        for (var i = 0; i < blocks.Count; i++)
        {
            await Assert.That(blocks[i].Number).IsEqualTo(start + (ulong)i);
            await Assert.That(blocks[i].Items[0].Kind).IsEqualTo(BlockItemKind.BlockHeader);
            await Assert.That(blocks[i].Proof).IsNotNull();
        }
    }

    [Test]
    public async Task Can_Tail_Live_Blocks()
    {
        RequireBlockNode();
        await using var client = TestNetwork.CreateBlockNodeClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var channel = Channel.CreateUnbounded<Block>();
        var subscription = client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
        {
            BlockWriter = channel.Writer,
            MaxCount = 2,
            CancellationToken = cts.Token
        });
        var blocks = new List<Block>();
        await foreach (var block in channel.Reader.ReadAllAsync(CancellationToken.None))
        {
            blocks.Add(block);
        }
        await subscription;
        await Assert.That(blocks.Count).IsEqualTo(2);
        await Assert.That(blocks[1].Number).IsEqualTo(blocks[0].Number + 1);
    }

    [Test]
    public async Task Item_Stream_Delivers_Items_Before_Block_Completes()
    {
        RequireBlockNode();
        await using var client = TestNetwork.CreateBlockNodeClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var channel = Channel.CreateUnbounded<StreamedBlockItem>();
        var subscription = client.SubscribeBlockItemStreamAsync(new SubscribeBlockItemStreamParams
        {
            ItemWriter = channel.Writer,
            CancellationToken = cts.Token
        });
        var itemsBeforeCompletion = 0;
        var sawCompletion = false;
        await foreach (var item in channel.Reader.ReadAllAsync(CancellationToken.None))
        {
            if (item.IsBlockComplete)
            {
                sawCompletion = true;
                cts.Cancel();
            }
            else
            {
                itemsBeforeCompletion++;
            }
            if (sawCompletion)
            {
                break;
            }
        }
        // Drain any remaining items so the writer can complete.
        while (channel.Reader.TryRead(out _)) { }
        await subscription;
        await Assert.That(sawCompletion).IsTrue();
        await Assert.That(itemsBeforeCompletion).IsGreaterThan(0);
    }
}
