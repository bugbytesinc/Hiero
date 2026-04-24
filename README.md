# Hiero

**.NET Client Library for the Hiero Network and Hedera Hashgraph**

[![NuGet](https://img.shields.io/nuget/v/Hiero.svg)](https://www.nuget.org/packages/Hiero/)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)
[![llms.txt](https://img.shields.io/badge/AI%20docs-llms.txt-blue)](https://hashgraph.bugbytes.com/llms.txt)

Hiero provides idiomatic .NET access to the full [Hedera](https://www.hedera.com/) public network — cryptocurrency, consensus messaging, tokens, NFTs, smart contracts, file storage, scheduled transactions, airdrops, and more.

## Quick Start

### Install

```sh
dotnet add package Hiero
```

**Requirements:** .NET 10 SDK

### Hello World — Query an Account Balance

```csharp
await using var client = new ConsensusClient(ctx =>
{
    ctx.Endpoint = new ConsensusNodeEndpoint(
        new EntityId(0, 0, 3),
        new Uri("https://2.testnet.hedera.com:50211"));
});

var balance = await client.GetAccountBalanceAsync(new EntityId(0, 0, 98));
Console.WriteLine($"Balance: {balance:#,#} tinybars");
```

### Transfer Crypto

```csharp
await using var client = new ConsensusClient(ctx =>
{
    ctx.Endpoint = new ConsensusNodeEndpoint(
        new EntityId(0, 0, 3),
        new Uri("https://2.testnet.hedera.com:50211"));
    ctx.Payer = new EntityId(0, 0, payerAccountId);
    ctx.Signatory = new Signatory(payerPrivateKey);
});

var receipt = await client.TransferAsync(
    new EntityId(0, 0, senderAccountId),
    new EntityId(0, 0, receiverAccountId),
    amount);
```

### Create a Token

```csharp
var receipt = await client.CreateTokenAsync(new CreateTokenParams
{
    Name = "My Token",
    Symbol = "MTK",
    Decimals = 8,
    Circulation = 1_000_000,
    Treasury = treasuryAccount,
    Administrator = new Endorsement(adminPublicKey),
    Signatory = new Signatory(adminPrivateKey)
});
Console.WriteLine($"Token ID: {receipt.Token}");
```

## Architecture

Hiero provides three client types, each targeting a different part of the Hedera network:

| Client | Purpose | Protocol |
|--------|---------|----------|
| **`ConsensusClient`** | Submit transactions and queries to gossip nodes | gRPC |
| **`MirrorRestClient`** | Query historical data and state from Mirror Nodes | REST/JSON |
| **`MirrorGrpcClient`** | Subscribe to real-time streams (e.g., HCS topics) | gRPC streaming |

### Transaction Pattern

All state-changing operations follow the same pattern:

```
Create *Params → Configure Client → ExecuteAsync → Receive *Receipt
```

Every transaction type has a dedicated `*Params` class, a typed `*Receipt`, and an optional detailed `*Record` available from the Mirror Node.

### Context Stack

Client configuration uses a hierarchical context stack. Child contexts (created via `Clone` or per-call `configure` callbacks) inherit from the parent but can override individual settings without affecting it:

```csharp
var child = client.Clone(ctx => ctx.FeeLimit = 500_000_000);
// child inherits parent's Endpoint, Payer, Signatory — but has its own FeeLimit
```

## Connecting to Testnet vs. Mainnet

| Network | Node address | Purpose |
|---------|-------------|---------|
| Testnet | `https://0.testnet.hedera.com:50211` (node `0.0.3`) | Development; free test HBAR from [portal.hedera.com](https://portal.hedera.com) |
| Testnet | `https://1.testnet.hedera.com:50211` (node `0.0.4`) | Development |
| Mainnet | `https://35.237.200.180:50211` (node `0.0.3`) | Production; real HBAR required |
| Mainnet | See [full node list](https://docs.hedera.com/hedera/networks/mainnet/mainnet-nodes) | Production |

Get a free testnet account and credentials at [portal.hedera.com](https://portal.hedera.com). See the [network configuration guide](https://hashgraph.bugbytes.com/tutorials/network.html) for node rotation patterns and mirror node endpoints.

## Supported Network Services

- **Cryptocurrency** — Create, transfer, update, and delete accounts; multi-party transfers; allowances
- **Consensus Service (HCS)** — Create topics, submit messages (including segmented large messages), subscribe to streams
- **Tokens** — Fungible token lifecycle: create, mint, burn, transfer, freeze, pause, KYC, royalties
- **NFTs** — Non-fungible token lifecycle: create, mint, burn, transfer, confiscate, update metadata
- **Smart Contracts** — Deploy, call, query contracts; native EVM transaction support (type 0/1/2)
- **File Service** — Create, append, update, and delete files on the network
- **Scheduled Transactions** — Wrap any transaction for deferred execution with multi-sig collection
- **Airdrops** — Distribute tokens to multiple recipients; pending airdrop claim/cancel workflow
- **Network Utilities** — Address book, fee schedules, exchange rates, version info, pseudo-random numbers

## Key Types

| Type | Description |
|------|-------------|
| `EntityId` | Hedera entity address (`shard.realm.num`), also supports key aliases and EVM addresses |
| `Endorsement` | Public key or N-of-M key list representing signing requirements |
| `Signatory` | Private key, key list, or async callback for signing transactions |
| `ConsensusNodeEndpoint` | Network node identity (account + gRPC URI) |
| `ConsensusTimeStamp` | Nanosecond-precision Hedera timestamp |

## Building from Source

```sh
dotnet restore Hiero.slnx
dotnet build Hiero.slnx
```

### Run Tests

Unit tests (no network required):

```sh
dotnet test --project test/Hiero.Test.Unit/
```

Integration tests against a local Solo network (requires Docker with 12GB+ RAM):

```sh
./solo/up.sh                # Start local Hiero network
./solo/test.sh              # Run integration tests
./solo/down.sh              # Tear down when done
```

See [`solo/`](solo/) for details.

### Build Documentation

```sh
dotnet tool restore
dotnet docfx docfx/docfx.json --serve
```

## Project Structure

```
src/
└── Hiero/                 Source library
    ├── Consensus/         HCS topic operations
    ├── Contract/          Smart contract operations
    ├── Crypto/            Account and transfer operations
    ├── Token/             Fungible token operations
    ├── Nft/               Non-fungible token operations
    ├── File/              File service operations
    ├── Schedule/          Scheduled transaction operations
    ├── AddressBook/       Consensus node management
    ├── Root/              System/network admin operations
    ├── Mirror/            Mirror Node REST query types
    ├── Utilities/         Network queries and helpers
    ├── Converters/        JSON serialization
    └── Implementation/    Internal machinery
test/                      Unit and integration tests (TUnit)
solo/                      Local Solo network for testing (Docker only)
reference/                 Upstream protobuf definitions (do not modify)
docfx/                     DocFX documentation site
docs/                      API cookbook and reference
samples/                   Runnable sample console apps
```

### Query Historical Data from the Mirror Node

```csharp
var mirror = new MirrorRestClient(new HttpClient
{
    BaseAddress = new Uri("https://testnet.mirrornode.hedera.com")
});

await foreach (var tx in mirror.GetTransactionsAsync(
    AccountFilter.Is(new EntityId(0, 0, 12345)),
    new PageLimit(10),
    OrderBy.Descending))
{
    Console.WriteLine($"{tx.ConsensusTimestamp}: {tx.Name}");
}
```

### Subscribe to a Real-Time HCS Topic Stream

```csharp
await using var stream = new MirrorGrpcClient(ctx =>
{
    ctx.Uri = new Uri("https://hcs.testnet.mirrornode.hedera.com:5600");
});

var channel = Channel.CreateUnbounded<TopicMessage>();
_ = stream.SubscribeTopicAsync(new SubscribeTopicParams
{
    Topic = new EntityId(0, 0, topicId),
    MessageWriter = channel.Writer,
    Starting = ConsensusTimeStamp.MinValue
});

await foreach (var msg in channel.Reader.ReadAllAsync())
{
    Console.WriteLine(Encoding.UTF8.GetString(msg.Message.Span));
}
```

## Dependencies

- [Google.Protobuf](https://www.nuget.org/packages/Google.Protobuf/) + [Grpc.Net.Client](https://www.nuget.org/packages/Grpc.Net.Client/) — Protocol Buffers and gRPC
- [Portable.BouncyCastle](https://www.nuget.org/packages/Portable.BouncyCastle/) — Ed25519 and ECDSA Secp256K1 cryptography

## Documentation

- **[API Reference](https://hashgraph.bugbytes.com/)** — Generated from XML doc comments
- **[Tutorials](https://hashgraph.bugbytes.com/tutorials/)** — Getting started guides with code examples
- **[API Cookbook](docs/api-cookbook.md)** — Quick reference for all SDK operations
- **[Samples](samples/)** — Runnable console apps for every major workflow

### AI and Code Assistant Support

Hiero provides machine-readable documentation for AI tools and code assistants:

- **[llms.txt](https://hashgraph.bugbytes.com/llms.txt)** — Structured index of documentation for AI agents (follows the [llms.txt standard](https://llmstxt.org/))
- **[llms-full.txt](https://hashgraph.bugbytes.com/llms-full.txt)** — Complete documentation in a single file, suitable for pasting into AI context windows

To use with Cursor, Windsurf, or similar editors: add `https://hashgraph.bugbytes.com/llms-full.txt` as a documentation source in your project settings.

## License

This project is licensed under the [Apache-2.0](LICENSE).

Copyright 2025-2026 [BugBytes, Inc.](https://bugbytes.com/) All Rights Reserved.
