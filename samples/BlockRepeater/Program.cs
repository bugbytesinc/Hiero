// SPDX-License-Identifier: Apache-2.0
using Hiero;
using Hiero.BlockNode;
using System.Threading.Channels;

// Usage: dotnet run -- <sourceBlockNodeUrl> <targetBlockNodeUrl> [startBlock] [--source-status <url>]
// Example: dotnet run -- http://source:40840 http://target:40840 1000
//
// --source-status names the source's STATUS endpoint, for a source that
// serves each Block Node API on its own port (see the HIP-1137 registry).
//
// A reference block-stream "repeater": it subscribes to one block node
// (the source), verifies each block's hash chain, and republishes the
// blocks to another block node (the target). It is the pattern block-node
// operators use to fan a stream out to additional nodes.
//
// Pointing both URLs at the same node demonstrates the publish handshake:
// the node answers the duplicate blocks with a DuplicateBlock termination.

if (args.Length < 2)
{
    Console.WriteLine("Usage: dotnet run -- <sourceBlockNodeUrl> <targetBlockNodeUrl> [startBlock] [--source-status <url>]");
    return;
}
var sourceUrl = new Uri(args[0]);
var targetUrl = new Uri(args[1]);
ulong? startBlock = args.Length > 2 && ulong.TryParse(args[2], out var parsedStart) ? parsedStart : null;
var sourceStatusIndex = Array.IndexOf(args, "--source-status");
Uri? sourceStatusUrl = sourceStatusIndex >= 0 && sourceStatusIndex + 1 < args.Length ? new Uri(args[sourceStatusIndex + 1]) : null;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await using var source = new BlockNodeClient(ctx =>
{
    ctx.Uri = sourceUrl;
    ctx.StatusUri = sourceStatusUrl;
});
await using var target = new BlockNodeClient(ctx => ctx.Uri = targetUrl);

#region BlockRepeater
// The target's publish session. The caller MUST consume Events to react
// to the node's directives; here we log skip/resend/behind and stop on a
// stream-ending directive.
await using var publisher = await target.CreateBlockStreamPublisherAsync(cts.Token);
var directives = Task.Run(async () =>
{
  try
  {
    await foreach (var evt in publisher.Events.ReadAllAsync(cts.Token))
    {
        switch (evt.Kind)
        {
            case PublishStreamEventKind.BlockAcknowledged:
                Console.WriteLine($"  target acknowledged through block {evt.BlockNumber}");
                break;
            case PublishStreamEventKind.SkipBlock:
                Console.WriteLine($"  target already has block {evt.BlockNumber} — skipping");
                break;
            case PublishStreamEventKind.ResendBlock:
                Console.WriteLine($"  target requested resend of block {evt.BlockNumber}");
                break;
            case PublishStreamEventKind.BehindPublisher:
                Console.WriteLine($"  target is behind at block {evt.BlockNumber}");
                break;
            case PublishStreamEventKind.StreamEnded:
                Console.WriteLine($"  target ended the stream: {evt.EndReason}");
                cts.Cancel();
                break;
        }
    }
  }
  catch (OperationCanceledException)
  {
      // Shutting down.
  }
});

// Subscribe to the source with hash-chain verification, and carry a
// checkpoint so a source failure can be resumed with cryptographic
// continuity (the failover-on-primitives pattern — no SDK failover
// machinery required).
BlockStreamCheckpoint? checkpoint = null;
var blocks = Channel.CreateBounded<Block>(16);

var pump = Task.Run(async () =>
{
    try
    {
        while (!cts.Token.IsCancellationRequested)
        {
            var subscribeParams = new SubscribeBlockStreamParams
            {
                VerificationLevel = BlockVerificationLevel.HashChain,
                BlockWriter = blocks.Writer,
                CompleteChannelWhenFinished = false,
                CancellationToken = cts.Token,
            };
            if (checkpoint is not null)
            {
                subscribeParams.ResumeFromCheckpoint = checkpoint;
            }
            else
            {
                subscribeParams.StartingBlockNumber = startBlock;
            }
            try
            {
                await source.SubscribeBlockStreamAsync(subscribeParams);
                break; // clean end (bounded range or channel closed)
            }
            catch (BlockNodeException ex) when (ex.Code is BlockNodeExceptionCode.Unavailable
                                                        or BlockNodeExceptionCode.CommunicationError)
            {
                // Source dropped. In a multi-node deployment, this is where
                // you would pick another node discovered via
                // MirrorRestClient.GetRegisteredBlockNodesAsync and resume
                // from the checkpoint. Here we simply retry the same source.
                Console.WriteLine($"  source unavailable ({ex.Code}); resuming from checkpoint...");
                await Task.Delay(1000, cts.Token);
            }
        }
    }
    catch (OperationCanceledException)
    {
        // Shutting down.
    }
    finally
    {
        blocks.Writer.TryComplete();
    }
});

// Republish each verified block to the target, retaining a checkpoint of
// the last verified block for resume. Both the block feed and the target
// can stop us: a canceled token (Ctrl+C or a stream-ending directive) or
// a publish fault (e.g. the target rejecting a duplicate block).
try
{
    await foreach (var block in blocks.Reader.ReadAllAsync(cts.Token))
    {
        Console.WriteLine($"repeating block {block.Number} ({block.Items.Count} items)");
        await publisher.WriteBlockAsync(block, cts.Token);
        checkpoint = new BlockStreamCheckpoint(block.Number, block.VerifiedRootHash);
    }
    await publisher.EndStreamAsync(PublishStreamEndCode.Reset,
        checkpoint?.BlockNumber ?? 0,
        checkpoint?.BlockNumber ?? 0);
}
catch (OperationCanceledException)
{
    // Ctrl+C or a stream-ending directive.
}
catch (BlockNodeException ex)
{
    Console.WriteLine($"publish stopped: {ex.Code} — {ex.Message}");
}
await cts.CancelAsync();
await pump;
await directives;
#endregion

Console.WriteLine("Repeater stopped.");
