---
title: Submit an HCS Message
---

# Submit an HCS Message

Submitting a message appends bytes to a topic. Once the network reaches consensus on the message, it receives a **sequence number** and a **consensus timestamp** that are immutable and globally ordered.

## Simple message

```csharp
var message = Encoding.UTF8.GetBytes("Hello from Hiero SDK!");
SubmitMessageReceipt receipt = await client.SubmitMessageAsync(topic, message);
Console.WriteLine($"Sequence: {receipt.SequenceNumber}");
```

## Submit-key-gated topics

If the topic was created with a `Submitter` endorsement, the submit key must sign every message. Use the params overload to attach it:

```csharp
var receipt = await client.SubmitMessageAsync(new SubmitMessageParams
{
    Topic = topic,
    Message = Encoding.UTF8.GetBytes("Authorized event"),
    Signatory = new Signatory(submitKey)
});
```

## Large messages (segmentation)

Individual messages are limited to about 4 KB. For larger payloads, the SDK can automatically split the data into numbered segments that the mirror node reassembles:

```csharp
SubmitMessageReceipt[] receipts = await client.SubmitLargeMessageAsync(
    topic,
    largePayload,
    segmentSize: 4000,
    signatory: new Signatory(submitKey));
Console.WriteLine($"Sent {receipts.Length} segments");
```

Alternatively, manage segments manually with [`SubmitMessageParams.SegmentIndex`](xref:Hiero.SubmitMessageParams) and `TotalSegmentCount`.

## Key points

- The message payload is opaque bytes — the network does not interpret them.
- The receipt's `SequenceNumber` increments monotonically per topic. It is the primary ordering key.
- Subscription consumers on the mirror node receive messages in consensus-timestamp order.

## See also

- [Create an HCS topic](createtopic.md)
- [Subscribe to a topic](subscribe.md)
- [`SubmitMessageParams` API reference](~/api/Hiero.SubmitMessageParams.yml)
