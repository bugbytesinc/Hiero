// SPDX-License-Identifier: Apache-2.0
using Hiero.BlockNode;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.BlockNode;

[Category("Block Node")]
public class BlockStreamPublisherTests
{
    private static void RequireBlockNode()
    {
        if (TestNetwork.BlockNodeUri is null)
        {
            Skip.Test("Block Node Endpoint [BlockNodeEndpoint] is not configured.");
        }
    }

    /// <summary>
    /// The safe live publish handshake: a block node already fed by the
    /// consensus node cannot accept a second publisher appending blocks,
    /// so re-publishing an already-persisted block exercises the real
    /// bidi wire protocol with zero risk to the stream.  The node must
    /// respond per protocol (a duplicate/behind terminal), which the
    /// publisher surfaces as a StreamEnded event and a faulting write.
    /// </summary>
    [Test]
    public async Task Publishing_An_Existing_Block_Draws_A_Protocol_Termination()
    {
        RequireBlockNode();
        await using var client = TestNetwork.CreateBlockNodeClient();
        var status = await client.GetServerStatusAsync();
        if (status.LastAvailableBlockNumber < status.FirstAvailableBlockNumber + 5)
        {
            Skip.Test("The block node does not hold enough history for this test.");
        }
        var existing = await client.GetBlockAsync(status.LastAvailableBlockNumber - 3);
        await Assert.That(existing).IsNotNull();

        await using var publisher = await client.CreateBlockStreamPublisherAsync();
        var events = new List<PublishStreamEvent>();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var pump = Task.Run(async () =>
        {
            await foreach (var evt in publisher.Events.ReadAllAsync(timeout.Token))
            {
                events.Add(evt);
                if (evt.Kind == PublishStreamEventKind.StreamEnded)
                {
                    break;
                }
            }
        }, timeout.Token);

        try
        {
            await publisher.WriteBlockAsync(existing!, timeout.Token);
        }
        catch (BlockNodeException)
        {
            // The node may terminate the stream fast enough that the write
            // itself observes the fault; that is an acceptable outcome.
        }

        try
        {
            await pump;
        }
        catch (OperationCanceledException)
        {
            // Fall through to the assertion below for a clear failure.
        }

        await Assert.That(events).IsNotEmpty();
        var ended = events.Find(e => e.Kind == PublishStreamEventKind.StreamEnded);
        await Assert.That(ended).IsNotNull();
        // The node rejects the duplicate; the exact terminal reason is
        // node-version dependent (Solo answers DuplicateBlock), so accept
        // any non-success protocol termination.
        await Assert.That(ended!.EndReason).IsNotEqualTo(PublishStreamEndReason.Success);
    }
}
