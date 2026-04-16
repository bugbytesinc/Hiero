---
title: Create an HCS Topic
---

# Create an HCS Topic

Hedera Consensus Service (HCS) topics are append-only public message logs with network-agreed timestamps. Anyone can read; writing is optionally gated by a **submit key**. An **administrator key** allows updating or deleting the topic.

## Code

```csharp
var receipt = await client.CreateTopicAsync(new CreateTopicParams
{
    Memo = "My Application Topic",
    Administrator = adminEndorsement,
    Submitter = submitEndorsement,
    RenewPeriod = TimeSpan.FromDays(90),
    RenewAccount = payerAccount
});
Console.WriteLine($"Topic ID: {receipt.Topic}");
```

## What each parameter does

| Parameter | Description |
|-----------|-------------|
| `Memo` | Required. Short description of the topic. |
| `Administrator` | Optional. Key that can update or delete the topic. Without it the topic is immutable. |
| `Submitter` | Optional. Key required to submit messages. Without it, anyone can submit. |
| `RenewPeriod` | Auto-renewal interval (default 90 days). |
| `RenewAccount` | Account paying renewal fees. If set, `Administrator` must also be set. |

## Public vs. gated topics

- **Public topic** (no `Submitter`): any account can submit messages. Good for open audit logs.
- **Gated topic** (with `Submitter`): only messages signed by the submit key are accepted. Good for application-controlled event streams.

## See also

- [Submit a message](submit.md)
- [Subscribe to a topic](subscribe.md)
- [`CreateTopicParams` API reference](~/api/Hiero.CreateTopicParams.yml)
