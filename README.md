# Hiero

**.NET Client Library for the Hiero Network and Hedera Hashgraph**

[![NuGet](https://img.shields.io/nuget/v/Hiero.svg)](https://www.nuget.org/packages/Hiero/)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)

Hiero provides idiomatic .NET access to the full [Hedera](https://www.hedera.com/) public network — cryptocurrency, consensus messaging, tokens, NFTs, smart contracts, file storage, scheduled transactions, airdrops, and more.

> **Alpha Software** — This library is under active development. The API surface is subject to change in later versions without notice. Use at your own risk.

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
    InitialSupply = 1_000_000,
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

### Build Documentation

```sh
dotnet tool restore
dotnet docfx DocFx/docfx.json --serve
```

## Project Structure

```
Hiero/                     Source library
├── Consensus/             HCS topic operations
├── Contract/              Smart contract operations
├── Crypto/                Account and transfer operations
├── Token/                 Fungible token operations
├── Nft/                   Non-fungible token operations
├── File/                  File service operations
├── Schedule/              Scheduled transaction operations
├── AddressBook/           Consensus node management
├── Root/                  System/network admin operations
├── Mirror/                Mirror Node REST query types
├── Utilities/             Network queries and helpers
├── Converters/            JSON serialization
└── Implementation/        Internal machinery
Reference/                 Upstream protobuf definitions (do not modify)
DocFx/                     DocFX documentation site
docs/                      API cookbook and reference
samples/                   Runnable sample console apps
```

## Dependencies

- [Google.Protobuf](https://www.nuget.org/packages/Google.Protobuf/) + [Grpc.Net.Client](https://www.nuget.org/packages/Grpc.Net.Client/) — Protocol Buffers and gRPC
- [Portable.BouncyCastle](https://www.nuget.org/packages/Portable.BouncyCastle/) — Ed25519 and ECDSA Secp256K1 cryptography

## Documentation

- **[API Reference](https://hashgraph.bugbytes.com/)** — Generated from XML doc comments
- **[Tutorials](https://hashgraph.bugbytes.com/tutorials/)** — Getting started guides with code examples
- **[API Cookbook](docs/api-cookbook.md)** — Quick reference for all SDK operations
- **[Samples](samples/)** — Runnable console apps for every major workflow

## License

This project is licensed under the [Apache-2.0](LICENSE).

Copyright 2025 [BugBytes, Inc.](https://bugbytes.com/) All Rights Reserved.
