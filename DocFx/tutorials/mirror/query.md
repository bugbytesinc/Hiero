---
title: Query Historical State with the Mirror Node
---

# Query Historical State with the Mirror Node

The [`MirrorRestClient`](xref:Hiero.MirrorRestClient) wraps the Hedera Mirror Node REST API, giving you access to historical transactions, account state, token data, contract results, and more. Unlike `ConsensusClient` queries (which hit the gossip network and may cost HBAR), mirror queries are **free**.

## Setup

```csharp
var mirror = new MirrorRestClient(new HttpClient
{
    BaseAddress = new Uri("https://testnet.mirrornode.hedera.com")
});
```

`MirrorRestClient` takes a standard `HttpClient` — you can configure timeouts, default headers, or use `IHttpClientFactory` in DI scenarios.

## Query account data

```csharp
var account = await mirror.GetAccountAsync(new EntityId(0, 0, 98));
if (account is not null)
{
    Console.WriteLine($"Balance: {account.Balance?.Balance}");
    Console.WriteLine($"Key: {account.Key}");
}
```

## Query transaction history

```csharp
await foreach (var tx in mirror.GetAccountTransactionsAsync(
    accountId,
    new LimitFilter(25),
    new OrderByFilter("desc")))
{
    Console.WriteLine($"{tx.ConsensusTimestamp}: {tx.Name} — {tx.Result}");
}
```

Most mirror query methods that return collections use `IAsyncEnumerable<T>`, automatically paging through results.

## Query token info

```csharp
var token = await mirror.GetTokenAsync(tokenId);
Console.WriteLine($"Name: {token?.Name}, Supply: {token?.TotalSupply}");
```

## Query NFT info

```csharp
var nft = await mirror.GetNftAsync(new Nft(collection, serialNo));
Console.WriteLine($"Owner: {nft?.AccountId}, Metadata: {nft?.Metadata}");
```

## Query HCS messages

```csharp
await foreach (var msg in mirror.GetTopicMessagesAsync(
    topicId,
    new TimestampAfterFilter(since)))
{
    Console.WriteLine($"[{msg.SequenceNumber}] {msg.Message}");
}
```

## Filters

Mirror queries accept optional [`IMirrorQueryFilter`](xref:Hiero.Mirror.Filters.IMirrorQueryFilter) parameters:

| Filter | Purpose |
|--------|---------|
| `LimitFilter(n)` | Cap results to `n` entries per page |
| `OrderByFilter("asc"\|"desc")` | Sort direction |
| `TimestampAfterFilter(ts)` | Only results after this timestamp |
| `TimestampOnOrBeforeFilter(ts)` | Only results at or before this timestamp |
| `AccountIsFilter(id)` | Filter by account |
| `TokenIsFilter(id)` | Filter by token |

## Consensus queries vs. mirror queries

| | `ConsensusClient` | `MirrorRestClient` |
|-|-------|--------|
| **Data freshness** | Current state | Near-real-time (seconds lag) |
| **Cost** | HBAR (paid query) | Free |
| **Richness** | Balances, info | Full history, transfers, logs |
| **Best for** | Balance checks before a transfer | Dashboards, auditing, analytics |

## See also

- [Subscribe to an HCS topic](../consensus/subscribe.md) — real-time streaming via `MirrorGrpcClient`
- [`MirrorRestClient` API reference](~/api/Hiero.MirrorRestClient.yml)
