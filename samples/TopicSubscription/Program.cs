// SPDX-License-Identifier: Apache-2.0
using System.Text;
using System.Threading.Channels;
using Hiero;

// Usage: dotnet run -- <mirrorGrpcEndpoint> <topicNum> [maxMessages]
// Example: dotnet run -- https://hcs.testnet.mirrornode.hedera.com:5600 1234 10
//
// Subscribes to an HCS topic stream via the Mirror Node gRPC API
// and prints incoming messages. No payer account required.
// Press Ctrl+C to stop, or specify maxMessages to auto-stop.

var mirrorEndpoint = args[0];
var topicNum = long.Parse(args[1]);
var maxMessages = args.Length > 2 ? ulong.Parse(args[2]) : 0UL;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

#region SubscribeTopic
await using var stream = new MirrorGrpcClient(ctx =>
{
    ctx.Uri = new Uri(mirrorEndpoint);
});

var channel = Channel.CreateUnbounded<TopicMessage>();
var topic = new EntityId(0, 0, topicNum);

Console.WriteLine($"Subscribing to topic 0.0.{topicNum}...");
Console.WriteLine("Waiting for messages (Ctrl+C to stop)...");

// Start subscription in the background. Messages arrive via channel.Writer
// so this task runs independently of the consumer loop below.
var subscribeTask = stream.SubscribeTopicAsync(new SubscribeTopicParams
{
    Topic = topic,
    MessageWriter = channel.Writer,
    Starting = ConsensusTimeStamp.MinValue,
    MaxCount = maxMessages,
    CancellationToken = cts.Token
});

// Read messages as they arrive
try
{
    await foreach (var msg in channel.Reader.ReadAllAsync(cts.Token))
    {
        var text = Encoding.UTF8.GetString(msg.Message.Span);
        Console.WriteLine($"[Seq {msg.SequenceNumber}] {msg.Consensus}: {text}");
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Subscription cancelled.");
}

await subscribeTask;
#endregion
Console.WriteLine("Done.");
