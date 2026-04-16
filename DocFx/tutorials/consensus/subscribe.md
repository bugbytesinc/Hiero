---
title: Subscribe to an HCS Topic
---

# Subscribe to an HCS Topic

Subscribing to a topic streams messages in real time from a **mirror node** via gRPC. This is the only tutorial that uses [`MirrorGrpcClient`](xref:Hiero.MirrorGrpcClient) — all other operations go through [`ConsensusClient`](xref:Hiero.ConsensusClient).

## Architecture note

Hedera has two communication paths:

| Client | Purpose |
|--------|---------|
| `ConsensusClient` | Submit transactions and queries to gossip nodes (gRPC) |
| `MirrorGrpcClient` | Subscribe to real-time HCS streams from mirror nodes (gRPC streaming) |

Subscription is free — no payer account is needed.

## Code

```csharp
await using var stream = new MirrorGrpcClient(ctx =>
{
    ctx.Uri = new Uri("https://hcs.testnet.mirrornode.hedera.com:5600");
});

var channel = Channel.CreateUnbounded<TopicMessage>();
var topic = new EntityId(0, 0, topicNum);

// Start the subscription in the background — messages arrive via the channel
var subscribeTask = stream.SubscribeTopicAsync(new SubscribeTopicParams
{
    Topic = topic,
    MessageWriter = channel.Writer,
    Starting = ConsensusTimeStamp.MinValue,  // from the beginning
    CancellationToken = cts.Token
});

// Consume messages as they arrive
await foreach (var msg in channel.Reader.ReadAllAsync(cts.Token))
{
    Console.WriteLine(
        $"[{msg.SequenceNumber}] {msg.Consensus}: " +
        Encoding.UTF8.GetString(msg.Message.Span));
}

await subscribeTask;
```

## What each parameter does

| Parameter | Description |
|-----------|-------------|
| `Topic` | The topic to subscribe to. |
| `MessageWriter` | A `ChannelWriter<TopicMessage>` that receives messages. Use `Channel.CreateUnbounded<TopicMessage>()` to create one. |
| `Starting` | Optional. Only return messages with a consensus timestamp at or after this value. `ConsensusTimeStamp.MinValue` means "from the beginning". |
| `Ending` | Optional. Stop when the consensus timestamp exceeds this value. Useful for bounded replays. |
| `MaxCount` | Optional. Stop after this many messages. 0 = stream indefinitely. |
| `CompleteChannelWhenFinished` | Default `true`. Closes the channel when the stream ends, letting `ReadAllAsync` complete. |
| `CancellationToken` | Cancels the stream gracefully. |

## Bounded historical replay

Subscribe to a specific time window (e.g., backfill yesterday's messages) rather than tailing live:

```csharp
await stream.SubscribeTopicAsync(new SubscribeTopicParams
{
    Topic = topic,
    MessageWriter = channel.Writer,
    Starting = yesterday,
    Ending = today
});
```

The call returns once `Ending` is reached.

## Key points

- `SubscribeTopicAsync` returns a `Task` that completes when the stream ends (via cancellation, `MaxCount`, `Ending`, or `CompleteChannelWhenFinished`).
- Messages delivered to the channel before a fault are still valid.
- If the mirror node disconnects, a [`MirrorGrpcException`](xref:Hiero.MirrorGrpcException) is thrown. Retry by calling `SubscribeTopicAsync` again, passing `Starting = lastReceivedTimestamp` to resume.

## See also

- [Create an HCS topic](createtopic.md)
- [Submit a message](submit.md)
- [`SubscribeTopicParams` API reference](~/api/Hiero.SubscribeTopicParams.yml)
- [`MirrorGrpcClient` API reference](~/api/Hiero.MirrorGrpcClient.yml)
