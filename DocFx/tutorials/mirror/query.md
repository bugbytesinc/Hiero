---
title: Query Historical State with the Mirror Node
---

# Query Historical State with the Mirror Node

The [`MirrorRestClient`](xref:Hiero.MirrorRestClient) wraps the Hedera Mirror Node REST API, giving you access to historical transactions, account state, token data, contract results, consensus messages, and more. Unlike `ConsensusClient` queries (which hit the gossip network and may cost HBAR), mirror queries are **free**.

## Setup

```csharp
var mirror = new MirrorRestClient(new HttpClient
{
    BaseAddress = new Uri("https://testnet.mirrornode.hedera.com")
});
```

`MirrorRestClient` takes a standard `HttpClient` — you can configure timeouts, default headers, or use `IHttpClientFactory` in DI scenarios.

## Accounts

```csharp
AccountData? account = await mirror.GetAccountAsync(new EntityId(0, 0, 98));

// List accounts (e.g. by shared public key)
await foreach (var a in mirror.GetAccountsAsync(
    AccountPublicKeyFilter.Is(endorsement),
    new PageLimit(50)))
{
    Console.WriteLine(a.Account);
}

// Allowances granted by an account
await foreach (var ha in mirror.GetAccountCryptoAllowancesAsync(account)) { }
await foreach (var ta in mirror.GetAccountTokenAllowancesAsync(account)) { }
await foreach (var na in mirror.GetAccountNftAllowancesAsOwnerAsync(account)) { }
await foreach (var na in mirror.GetAccountNftAllowancesAsSpenderAsync(account)) { }

// Staking rewards paid to an account
await foreach (var r in mirror.GetAccountStakingRewardsAsync(account)) { }

// Fungible token holdings and balances
await foreach (var h in mirror.GetAccountTokenHoldingsAsync(account)) { }
long? balance = await mirror.GetAccountTokenBalanceAsync(account, token);
```

## Tokens and NFTs

```csharp
TokenData? token = await mirror.GetTokenAsync(tokenId);

// Paged list of every token on the network, filterable by name/type/etc.
await foreach (var t in mirror.GetTokensAsync(
    TokenTypeFilter.NonFungible,
    TokenNameFilter.Contains("foo")))
{
    Console.WriteLine($"{t.Token}: {t.Name}");
}

// NFT lookups
NftData? nft = await mirror.GetNftAsync(new Nft(tokenId, serial));

// Every NFT held by a given account (across all collections or filtered to one)
await foreach (var n in mirror.GetAccountNftsAsync(account)) { }
await foreach (var n in mirror.GetAccountNftsAsync(account, TokenFilter.Is(tokenId))) { }

// Every NFT in a given collection
await foreach (var n in mirror.GetTokenNftsAsync(tokenId)) { }

// Full transaction history for a single NFT
await foreach (var h in mirror.GetNftTransactionHistoryAsync(new Nft(tokenId, serial))) { }

// Holders-of-token snapshot at a specific consensus time
await foreach (var holder in mirror.GetTokenHoldersSnapshotAsync(tokenId, asOf)) { }
```

## Airdrops

```csharp
await foreach (var a in mirror.GetAccountOutstandingAirdropsAsync(account)) { }
await foreach (var a in mirror.GetAccountPendingAirdropsAsync(account)) { }
```

## Transactions

```csharp
// A single transaction (all child/inner entries returned together)
TransactionDetailData[] group = await mirror.GetTransactionGroupAsync(txId);
TransactionDetailData?  one   = await mirror.GetTransactionAsync(consensusTimestamp);

// All transactions that credit or debit a given account
await foreach (var tx in mirror.GetTransactionsAsync(
    AccountFilter.Is(account),
    new PageLimit(25),
    OrderBy.Descending,
    TransferDirectionFilter.Credit,
    TransactionTypeFilter.CryptoTransfer,
    ResultFilter.Success))
{
    Console.WriteLine($"{tx.ConsensusTimestamp}: {tx.Name} — {tx.Result}");
}

// Same endpoint without the account narrowing — every transaction
// on the network matching the remaining filters
await foreach (var tx in mirror.GetTransactionsAsync(
    ResultFilter.Fail,
    TimestampFilter.After(since),
    OrderBy.Descending))
{
    Console.WriteLine($"{tx.ConsensusTimestamp}: {tx.Name} — {tx.Result}");
}

// Latest consensus timestamp observed by the mirror node
ConsensusTimeStamp latest = await mirror.GetLatestConsensusTimestampAsync();
```

## HCS (Consensus Service)

```csharp
TopicData? topic = await mirror.GetTopicAsync(topicId);

// Single message by sequence number or by consensus timestamp
TopicMessageData? m1 = await mirror.GetTopicMessageAsync(topicId, sequenceNumber);
TopicMessageData? m2 = await mirror.GetTopicMessageAsync(consensusTimestamp);

// Stream of all messages, optionally filtered
await foreach (var msg in mirror.GetTopicMessagesAsync(
    topicId,
    TimestampFilter.After(since),
    SequenceNumberFilter.OnOrAfter(100),
    OrderBy.Ascending))
{
    Console.WriteLine($"[{msg.SequenceNumber}] {msg.Message}");
}
```

## Contracts

```csharp
// Contract metadata; listing returns a summary view
ContractData? contract = await mirror.GetContractAsync(contractId);
await foreach (var c in mirror.GetContractsAsync(new PageLimit(50))) { }

// Storage state at a specific slot
ContractStateData? state = await mirror.GetContractStateAsync(
    contractId, position, new IMirrorQueryParameter[] { TimestampFilter.OnOrBefore(asOf) });

// Results — by contract, by timestamp, by tx, by block, or global
await foreach (var r in mirror.GetContractResultsAsync(contractId)) { }
ContractResultData? a = await mirror.GetContractResultByTimestampAsync(contractId, ts);
ContractResultData? b = await mirror.GetContractResultByTransactionHashAsync(evmHash);
ContractResultData? c = await mirror.GetContractResultByTransactionIdAsync(txId);
ContractResultData? d = await mirror.GetContractResultByBlockAndPositionAsync(blockHash, idx);
await foreach (var r in mirror.GetContractResultsByBlockHashAsync(blockHash)) { }
await foreach (var r in mirror.GetAllContractResultsAsync()) { }

// Logs
await foreach (var log in mirror.GetContractLogEventsAsync(contractId)) { }
await foreach (var log in mirror.GetAllContractLogEventsAsync()) { }

// Internal actions (CALL / DELEGATECALL / CREATE trace) for a single EVM tx
await foreach (var act in mirror.GetContractActionsByTransactionHashAsync(evmHash)) { }
await foreach (var act in mirror.GetContractActionsByTransactionIdAsync(txId)) { }

// Opcode-level trace for an EVM tx (optional memory / stack / storage projections)
OpcodesData? byHash = await mirror.GetContractOpcodesByTransactionHashAsync(
    evmHash,
    OpcodeMemoryProjectionFilter.Include,
    OpcodeStackProjectionFilter.Include,
    OpcodeStorageProjectionFilter.Include);
OpcodesData? byId   = await mirror.GetContractOpcodesByTransactionIdAsync(txId);

// EVM-eth-call passthrough and gas estimation
EncodedParams result = await mirror.CallEvmAsync(evmCallData);
long gas             = await mirror.EstimateGasAsync(fromEvmAddress, callParams);
BigInteger chainId   = await mirror.GetChainIdAsync();
```

## Blocks

```csharp
BlockData? byNumber = await mirror.GetBlockAsync(blockNumber);
BlockData? byHash   = await mirror.GetBlockAsync(blockhash);
BlockData? latest   = await mirror.GetLatestBlockAsync();
BlockData? asOf     = await mirror.GetLatestBlockBeforeConsensusAsync(consensus);

// Paged list
await foreach (var b in mirror.GetBlocksAsync(OrderBy.Descending, new PageLimit(50))) { }
```

## Schedules

```csharp
ScheduleData? schedule = await mirror.GetScheduleAsync(scheduleId);

await foreach (var s in mirror.GetSchedulesAsync(
    AccountFilter.Is(creatorAccount),
    OrderBy.Descending))
{
    Console.WriteLine($"{s.Schedule}: executed={s.ExecutedTimestamp}");
}
```

## Network

```csharp
// Consensus-node roster
await foreach (var n in mirror.GetConsensusNodesAsync()) { }
IReadOnlyDictionary<ConsensusNodeEndpoint, long> active =
    await mirror.GetActiveConsensusNodesAsync(timeoutMs);

// Network-wide state
NetworkStakeData?  stake  = await mirror.GetNetworkStakeAsync();
NetworkSupplyData? supply = await mirror.GetNetworkSupplyAsync();
ExchangeRateData?  rate   = await mirror.GetExchangeRateAsync();
NetworkFeesData?   fees   = await mirror.GetLatestNetworkFeesAsync();
```

## Filters

Most list- and single-entity queries accept a `params IMirrorQueryParameter[]` tail. Four filter families — **predicate filters**, **projection filters**, **paging directives**, and the shared `IMirrorQueryParameter` root — live under [`Hiero.Mirror.Filters`](xref:Hiero.Mirror.Filters) and [`Hiero.Mirror.Paging`](xref:Hiero.Mirror.Paging).

### Paging directives

| Type | Construction | Purpose |
|------|--------------|---------|
| `PageLimit` | `new PageLimit(n)` | Caps results to `n` per page |
| `OrderBy`   | `OrderBy.Ascending` / `OrderBy.Descending` | Sort direction |

### Comparison-capable predicate filters (6-operator palette)

Each exposes `.Is`, `.After`, `.OnOrAfter`, `.Before`, `.OnOrBefore`, `.NotIs` factories.

```csharp
TimestampFilter.After(since)
SequenceNumberFilter.OnOrAfter(100)
SerialNumberFilter.Before(1_000)
AccountBalanceFilter.After(10_000_000_000)
BlockNumberFilter.Is(123_456)
TokenFilter.NotIs(tokenId)
AccountFilter.OnOrAfter(account)
SpenderFilter.Is(account)
ContractFilter.Is(contractId)
ContractActionIndexFilter.OnOrBefore(5)
ScheduleFilter.Is(scheduleId)
NodeFilter.After(nodeId)                 // no NotIs
ContractLogIndexFilter.OnOrAfter(10)     // no NotIs; requires TimestampFilter in same request
```

### Equality-only predicate filters

Exposed as `.Is(value)` factories only (the server accepts no other operators).

```csharp
AccountPublicKeyFilter.Is(endorsement)
PublicKeyFilter.Is(endorsement)
EvmSenderFilter.Is(evmAddress)
EvmTopicFilter.Is(topic0)
BlockHashFilter.Is(blockhash)
TransactionHashFilter.Is(evmHash)
SlotFilter.Is(slot)
FileFilter.Is(fileId)
TransactionIndexFilter.Is(0)
SenderFilter.Is(account)
ReceiverFilter.Is(account)
```

### Enum-like predicate filters

Exposed as static members (the value set is closed at compile time).

```csharp
ResultFilter.Success        // success | fail
ResultFilter.Fail
TokenTypeFilter.All         // ALL | FUNGIBLE_COMMON | NON_FUNGIBLE_UNIQUE
TokenTypeFilter.Fungible
TokenTypeFilter.NonFungible
TransferDirectionFilter.Credit  // credit | debit
TransferDirectionFilter.Debit
TransactionTypeFilter.CryptoTransfer  // ... full HAPI transaction enum
TokenNameFilter.Contains(fragment)     // 3–100 char substring
```

### Projection filters

Projection filters don't change *which* rows return — they change the *fields* included in each row. They implement `IMirrorProjection` and stack freely alongside predicate filters.

```csharp
BalanceProjectionFilter.Exclude              // drop per-token balances from AccountData
InternalProjectionFilter.Include              // include CryptoTransfer-child ledger entries
HbarTransferProjectionFilter.Include          // include hbar transfers alongside contract results
MessageEncodingProjectionFilter.Utf8          // ask mirror node to decode HCS payload
OpcodeMemoryProjectionFilter.Include          // bulk up opcode trace with memory
OpcodeStackProjectionFilter.Include
OpcodeStorageProjectionFilter.Include
```

## Consensus queries vs. mirror queries

| | `ConsensusClient` | `MirrorRestClient` |
|-|-------|--------|
| **Data freshness** | Current state | Near-real-time (seconds lag) |
| **Cost** | HBAR (paid query) | Free |
| **Richness** | Balances, info | Full history, transfers, logs, opcode traces |
| **Best for** | Balance checks before a transfer | Dashboards, auditing, analytics |

## See also

- [Subscribe to an HCS topic](../consensus/subscribe.md) — real-time streaming via `MirrorGrpcClient`
- [`MirrorRestClient` API reference](~/api/Hiero.MirrorRestClient.yml)
- [`Hiero.Mirror.Filters`](~/api/Hiero.Mirror.Filters.yml) — full filter catalog
- [`Hiero.Mirror.Paging`](~/api/Hiero.Mirror.Paging.yml) — paging directives
