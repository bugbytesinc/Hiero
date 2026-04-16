---
title: Connecting to the Hedera Network
---

# Connecting to the Hedera Network

Every Hiero client needs a network endpoint. This guide covers testnet (for development), mainnet (for production), and patterns for high availability.

## Testnet (development)

Testnet is free. Create a testnet account at [portal.hedera.com](https://portal.hedera.com) to get an account ID and private key.

```csharp
await using var client = new ConsensusClient(ctx =>
{
    ctx.Endpoint = new ConsensusNodeEndpoint(
        new EntityId(0, 0, 3),
        new Uri("https://0.testnet.hedera.com:50211"));
    ctx.Payer = new EntityId(0, 0, payerAccountNum);
    ctx.Signatory = new Signatory(payerPrivateKey);
});
```

### Testnet nodes

| Node | gRPC endpoint |
|------|---------------|
| `0.0.3` | `https://0.testnet.hedera.com:50211` |
| `0.0.4` | `https://1.testnet.hedera.com:50211` |
| `0.0.5` | `https://2.testnet.hedera.com:50211` |
| `0.0.6` | `https://3.testnet.hedera.com:50211` |

### Testnet mirror node

| Protocol | Endpoint |
|----------|----------|
| REST | `https://testnet.mirrornode.hedera.com` |
| gRPC | `https://hcs.testnet.mirrornode.hedera.com:5600` |

## Mainnet (production)

Mainnet transactions cost real HBAR. A few representative nodes:

| Node | gRPC endpoint |
|------|---------------|
| `0.0.3` | `https://35.237.200.180:50211` |
| `0.0.4` | `https://35.186.191.247:50211` |
| `0.0.5` | `https://35.192.2.25:50211` |

See the [full mainnet node list](https://docs.hedera.com/hedera/networks/mainnet/mainnet-nodes) for all available nodes, or query the live list programmatically:

```csharp
// Via mirror node (free, no payer needed)
var mirror = new MirrorRestClient(new HttpClient
{
    BaseAddress = new Uri("https://mainnet-public.mirrornode.hedera.com")
});

await foreach (var node in mirror.GetConsensusNodesAsync())
{
    Console.WriteLine($"Node {node.NodeId}: {node.Description}");
    foreach (var ep in node.ServiceEndpoints ?? [])
    {
        Console.WriteLine($"  {ep.IpAddressV4}:{ep.Port}");
    }
}
```

[`GetActiveConsensusNodesAsync`](xref:Hiero.Mirror.ConsensusNodeDataExtensions.GetActiveConsensusNodesAsync*) is a convenience that returns only reachable nodes as a `IReadOnlyDictionary<ConsensusNodeEndpoint, long>`, ready to plug into `ctx.Endpoint`.

### Mainnet mirror node

| Protocol | Endpoint |
|----------|----------|
| REST | `https://mainnet-public.mirrornode.hedera.com` |
| gRPC | `https://mainnet-public.mirrornode.hedera.com:443` |

## Node selection

Each [`ConsensusClient`](xref:Hiero.ConsensusClient) instance targets a single gossip node. For production applications that need high availability, create clients pointing to different nodes:

### Round-robin pattern

```csharp
private static int _nodeIndex = 0;
private static readonly ConsensusNodeEndpoint[] _nodes =
{
    new(new EntityId(0, 0, 3), new Uri("https://0.testnet.hedera.com:50211")),
    new(new EntityId(0, 0, 4), new Uri("https://1.testnet.hedera.com:50211")),
    new(new EntityId(0, 0, 5), new Uri("https://2.testnet.hedera.com:50211")),
};

private ConsensusClient CreateClient() => new ConsensusClient(ctx =>
{
    ctx.Endpoint = _nodes[Interlocked.Increment(ref _nodeIndex) % _nodes.Length];
    ctx.Payer = payerAccount;
    ctx.Signatory = signatory;
});
```

### Per-request override via Clone

Alternatively, keep a single client and override the endpoint per call using [`Clone`](xref:Hiero.ConsensusClient.Clone*):

```csharp
var child = client.Clone(ctx =>
    ctx.Endpoint = _nodes[Random.Shared.Next(_nodes.Length)]);

var receipt = await child.TransferAsync(from, to, amount);
```

## Balance-only queries (no payer needed)

Querying an account balance is free — you only need an `Endpoint`, no `Payer` or `Signatory`:

```csharp
await using var client = new ConsensusClient(ctx =>
{
    ctx.Endpoint = new ConsensusNodeEndpoint(
        new EntityId(0, 0, 3),
        new Uri("https://0.testnet.hedera.com:50211"));
});

var balance = await client.GetAccountBalanceAsync(new EntityId(0, 0, 98));
```

## See also

- [Hello World (query balance)](crypto/balance.md)
- [Transfer crypto](crypto/transfer.md)
- [Mirror Node queries](mirror/query.md) — free REST queries for historical data
