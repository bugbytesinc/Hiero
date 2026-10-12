// SPDX-License-Identifier: Apache-2.0
using Hiero;
using Hiero.BlockNode;
using System.Threading.Channels;

// Usage: dotnet run -- <blockNodeEndpoint> [maxBlocks] [--items] [--status <statusEndpoint>]
// Example: dotnet run -- http://localhost:40840 5
// Example: dotnet run -- http://localhost:40840 5 --items
// Example: dotnet run -- http://block-node:40980 5 --status http://block-node:40982
//
// Subscribes to a Hiero Block Node's block stream and prints incoming
// blocks (or, with --items, individual block items as they arrive
// mid-block — the low-latency mode). No payer account required.
// Press Ctrl+C to stop, or specify maxBlocks to auto-stop.
//
// Some operators — mainnet's registered block nodes among them — serve
// each Block Node API on its own port (the HIP-1137 registry lists one
// endpoint per API). For those, pass the SUBSCRIBE_STREAM endpoint first
// and the STATUS endpoint with --status.

var blockNodeEndpoint = args[0];
var maxBlocks = args.Length > 1 && ulong.TryParse(args[1], out var parsedMaxBlocks) ? parsedMaxBlocks : 0UL;
var itemMode = args.Contains("--items");
var statusIndex = Array.IndexOf(args, "--status");
Uri? statusEndpoint = statusIndex >= 0 && statusIndex + 1 < args.Length ? new Uri(args[statusIndex + 1]) : null;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

if (!itemMode)
{
    #region SubscribeBlockStream
    await using var client = new BlockNodeClient(ctx =>
    {
        ctx.Uri = new Uri(blockNodeEndpoint);
        // Only for nodes serving their status API on a separate endpoint;
        // when null, status calls share Uri.
        ctx.StatusUri = statusEndpoint;
    });

    var status = await client.GetServerStatusAsync();
    Console.WriteLine($"Block node serves blocks {status.FirstAvailableBlockNumber}..{status.LastAvailableBlockNumber}");
    Console.WriteLine("Tailing live blocks (Ctrl+C to stop)...");

    var channel = Channel.CreateBounded<Block>(32);

    // Start the subscription in the background. Assembled blocks arrive
    // via channel.Writer; gaps and reconnects are handled automatically
    // (GapPolicy.Resubscribe) and each block is structurally validated
    // (VerificationLevel.Structural) before delivery.
    var subscribeTask = client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
    {
        BlockWriter = channel.Writer,
        MaxCount = maxBlocks,
        CancellationToken = cts.Token
    });

    // Read blocks as they arrive.
    try
    {
        await foreach (var block in channel.Reader.ReadAllAsync(cts.Token))
        {
            var transactions = block.Items.Count(i => i.Kind == BlockItemKind.SignedTransaction);
            Console.WriteLine($"Block {block.Number} @ {block.Timestamp}: {block.Items.Count} items, {transactions} transactions, proof: {block.Proof?.Kind}");
        }
    }
    catch (OperationCanceledException)
    {
        // Ctrl+C — fall through to await the subscription's own shutdown.
    }
    await subscribeTask;
    #endregion
}
else
{
    #region SubscribeBlockItemStream
    await using var client = new BlockNodeClient(ctx =>
    {
        ctx.Uri = new Uri(blockNodeEndpoint);
        // Only for nodes serving their status API on a separate endpoint;
        // when null, status calls share Uri.
        ctx.StatusUri = statusEndpoint;
    });

    Console.WriteLine("Tailing live block items — delivered mid-block, before verification (Ctrl+C to stop)...");

    var channel = Channel.CreateBounded<StreamedBlockItem>(256);
    var completedBlocks = 0UL;

    // Item-level delivery: each item arrives the moment its frame does,
    // potentially a second or more before the block's proof — useful when
    // reacting to events (e.g. EVM contract outputs) where milliseconds
    // matter and provisional data can be reconciled later.
    var subscribeTask = client.SubscribeBlockItemStreamAsync(new SubscribeBlockItemStreamParams
    {
        ItemWriter = channel.Writer,
        CancellationToken = cts.Token
    });

    try
    {
        await foreach (var streamed in channel.Reader.ReadAllAsync(cts.Token))
        {
            if (streamed.Item.Kind == BlockItemKind.TransactionOutput || streamed.Item.Kind == BlockItemKind.SignedTransaction)
            {
                Console.WriteLine($"Block {streamed.BlockNumber} (in progress): {streamed.Item.Kind} observed");
            }
            if (streamed.IsBlockComplete)
            {
                Console.WriteLine($"Block {streamed.BlockNumber} complete");
                completedBlocks++;
                if (maxBlocks > 0 && completedBlocks >= maxBlocks)
                {
                    cts.Cancel();
                }
            }
        }
    }
    catch (OperationCanceledException)
    {
        // Ctrl+C or maxBlocks reached.
    }
    await subscribeTask;
    #endregion
}

Console.WriteLine("Done.");
