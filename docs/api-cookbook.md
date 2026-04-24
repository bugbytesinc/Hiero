# Hiero .NET SDK - API Cookbook

> Quick reference for all SDK operations. Every method listed below is an
> extension method on `ConsensusClient` unless noted otherwise.
> All methods accept an optional trailing `Action<IConsensusContext>? configure`
> parameter (omitted below for brevity).

## Setup

```csharp
// Consensus Client (transactions & queries to gossip nodes)
await using var client = new ConsensusClient(ctx =>
{
    ctx.Endpoint = new ConsensusNodeEndpoint(
        new EntityId(0, 0, 3),              // node account
        new Uri("https://2.testnet.hedera.com:50211"));
    ctx.Payer = new EntityId(0, 0, payerNum);
    ctx.Signatory = new Signatory(payerPrivateKey);
});

// Mirror REST Client (historical queries)
var mirror = new MirrorRestClient(new HttpClient
{
    BaseAddress = new Uri("https://testnet.mirrornode.hedera.com")
});

// Mirror gRPC Client (streaming subscriptions)
await using var stream = new MirrorGrpcClient(ctx =>
{
    ctx.Uri = new Uri("https://hcs.testnet.mirrornode.hedera.com:5600");
});
```

### Key Types

| Type | Description |
|------|-------------|
| `EntityId` | Hedera address `(shard, realm, num)`. Also supports EVM address and key alias forms. |
| `Endorsement` | Public key or N-of-M key list representing signing requirements. |
| `Signatory` | Private key, key list, or `Func<IInvoice, Task>` callback for signing. |
| `ConsensusTimeStamp` | Nanosecond-precision timestamp. Construct from `DateTime` or `decimal` seconds. |
| `Nft` | NFT identifier: `new Nft(tokenEntityId, serialNumber)`. |
| `Hex` | Utility: `Hex.ToBytes("302e...")` converts hex string to `ReadOnlyMemory<byte>`. |

### Context Configuration (`IConsensusContext`)

| Property | Type | Description |
|----------|------|-------------|
| `Endpoint` | `ConsensusNodeEndpoint?` | Target gossip node |
| `Payer` | `EntityId?` | Account paying fees |
| `Signatory` | `Signatory?` | Key(s) for signing |
| `FeeLimit` | `long` | Max fee in tinybars |
| `TransactionDuration` | `TimeSpan` | Tx validity window |
| `Memo` | `string?` | Transaction memo |
| `RetryCount` | `int` | Auto-retry on BUSY |
| `RetryDelay` | `TimeSpan` | Delay between retries |

---

## Cryptocurrency

### Query Balance
```csharp
ulong balance = await client.GetAccountBalanceAsync(account);
AccountBalances balances = await client.GetAccountBalancesAsync(account); // includes token balances
```

### Query Account Info
```csharp
AccountInfo info = await client.GetAccountInfoAsync(account);
// info.Address, info.Balance, info.Endorsement, info.Memo,
// info.EvmAddress, info.AutoRenewPeriod, info.ReceiveSignatureRequired, ...

AccountDetails detail = await client.GetAccountDetailsAsync(account);
// All AccountInfo fields + CryptoAllowances, TokenAllowances, NftAllowances
```

### Create Account
```csharp
var receipt = await client.CreateAccountAsync(new CreateAccountParams
{
    Endorsement = new Endorsement(publicKey),
    InitialBalance = 100_000_000   // 1 hbar in tinybars
});
EntityId newAccount = receipt.Address;
```

### Transfer Crypto (hbar)
```csharp
// Simple: one sender, one receiver
var receipt = await client.TransferAsync(from, to, amount);

// Multi-party: use TransferParams
var receipt = await client.TransferAsync(new TransferParams
{
    CryptoTransfers = new[]
    {
        new CryptoTransfer(sender1, -500),
        new CryptoTransfer(sender2, -500),
        new CryptoTransfer(receiver, 1000)
    },
    Signatory = new Signatory(sender1Key, sender2Key)
});
```

### Transfer Tokens
```csharp
// Simple fungible token transfer
var receipt = await client.TransferTokenAsync(token, from, to, amount);

// Simple NFT transfer
var receipt = await client.TransferNftAsync(new Nft(token, serial), from, to);
```

### Update Account
```csharp
var receipt = await client.UpdateAccountAsync(new UpdateAccountParams
{
    Account = account,
    Memo = "Updated memo",
    Signatory = new Signatory(accountKey)
});
```

### Delete Account
```csharp
var receipt = await client.DeleteAccountAsync(new DeleteAccountParams
{
    Account = accountToDelete,
    FundsReceiver = receiverAccount,
    Signatory = new Signatory(accountKey)
});
```

### Allowances
```csharp
// Grant allowances
var receipt = await client.AllocateAllowanceAsync(new AllowanceParams
{
    CryptoAllowances = new[] { new CryptoAllowance(owner, spender, amount) },
    TokenAllowances = new[] { new TokenAllowance(token, owner, spender, amount) },
    NftAllowances = new[] { new NftAllowance(token, owner, spender, serialNumbers) },
    Signatory = new Signatory(ownerKey)
});

// Revoke NFT allowances
var receipt = await client.RevokeNftAllowanceAsync(new RevokeNftAllowanceParams
{
    Owner = owner, Token = token, SerialNumbers = new long[] { 1, 2, 3 },
    Signatory = new Signatory(ownerKey)
});
```

---

## Fungible Tokens

### Create Token
```csharp
var receipt = await client.CreateTokenAsync(new CreateTokenParams
{
    Name = "My Token",
    Symbol = "MTK",
    Circulation = 1_000_000,          // initial supply (smallest unit)
    Decimals = 8,
    Ceiling = 10_000_000,             // max supply (0 = unlimited)
    Treasury = treasuryAccount,
    Administrator = new Endorsement(adminKey),
    SupplyEndorsement = new Endorsement(supplyKey),
    Memo = "My fungible token",
    Signatory = new Signatory(adminKey, treasuryKey)
});
EntityId tokenId = receipt.Token;
```

### Mint / Burn
```csharp
TokenReceipt r = await client.MintTokenAsync(token, 500_000);
TokenReceipt r = await client.BurnTokenAsync(token, 100_000);
```

### Associate / Dissociate
```csharp
await client.AssociateTokenAsync(token, account);
await client.DissociateTokenAsync(token, account);
```

### Suspend / Resume (Freeze)
```csharp
await client.SuspendTokenAsync(token, holder);
await client.ResumeTokenAsync(token, holder);
```

### Pause / Continue (Global)
```csharp
await client.PauseTokenAsync(token);
await client.ContinueTokenAsync(token);
```

### KYC
```csharp
await client.GrantTokenKycAsync(token, holder);
await client.RevokeTokenKycAsync(token, holder);
```

### Confiscate (Wipe)
```csharp
TokenReceipt r = await client.ConfiscateTokenAsync(token, holder, amount);
```

### Update Token
```csharp
await client.UpdateTokenAsync(new UpdateTokenParams
{
    Token = token,
    Name = "Renamed Token",
    Memo = "Updated description"
});
```

### Update Royalties (Custom Fees)
```csharp
await client.UpdateRoyaltiesAsync(token, new IRoyalty[]
{
    new TokenRoyalty(feeReceiver, numerator: 25, denominator: 1000,
                     minimum: 0, maximum: 0)
});
```

### Delete Token
```csharp
await client.DeleteTokenAsync(token);
```

### Query Token Info
```csharp
TokenInfo info = await client.GetTokenInfoAsync(token);
// info.Name, info.Symbol, info.Circulation, info.Decimals, info.Treasury, ...
```

---

## Non-Fungible Tokens (NFTs)

### Create NFT Collection
```csharp
var receipt = await client.CreateNftAsync(new CreateNftParams
{
    Name = "My NFT Collection",
    Symbol = "MNFT",
    Ceiling = 1000,                   // max mintable NFTs
    Treasury = treasuryAccount,
    Administrator = new Endorsement(adminKey),
    SupplyEndorsement = new Endorsement(supplyKey),
    Signatory = new Signatory(adminKey, treasuryKey)
});
EntityId nftToken = receipt.Token;
```

### Mint NFTs
```csharp
// Single NFT
NftMintReceipt r = await client.MintNftAsync(nftToken, metadata);

// Batch mint
NftMintReceipt r = await client.MintNftsAsync(new MintNftParams
{
    Token = nftToken,
    Metadata = new[] { metadata1, metadata2, metadata3 },
    Signatory = new Signatory(supplyKey)
});
IReadOnlyList<long> serialNumbers = r.SerialNumbers;
```

### Burn / Confiscate NFTs
```csharp
TokenReceipt r = await client.BurnNftAsync(new Nft(nftToken, serialNo));
TokenReceipt r = await client.ConfiscateNftAsync(new Nft(nftToken, serialNo), holder);
```

### Update NFT Metadata
```csharp
await client.UpdateNftMetadataAsync(new Nft(nftToken, serialNo), newMetadata);
```

### Query NFT Info
```csharp
NftInfo info = await client.GetNftInfoAsync(new Nft(nftToken, serialNo));
```

---

## Airdrops

### Airdrop Fungible Tokens
```csharp
// Simple: single token, one sender, one receiver
await client.AirdropTokenAsync(token, from, to, amount);

// Batch: multiple transfers
await client.AirdropAsync(new AirdropParams
{
    TokenTransfers = new[]
    {
        new TokenTransfer(token, sender, -500),
        new TokenTransfer(token, receiver1, 250),
        new TokenTransfer(token, receiver2, 250)
    },
    Signatory = new Signatory(senderKey)
});
```

### Airdrop NFTs
```csharp
await client.AirdropNftAsync(new Nft(nftToken, serial), from, to);
```

### Claim / Cancel Pending Airdrops
```csharp
await client.ClaimAirdropAsync(new Airdrop(token, sender, receiver));
await client.CancelAirdropAsync(new Airdrop(token, sender, receiver));
```

### Relinquish Tokens (Return to Treasury)
```csharp
await client.RelinquishTokenAsync(token);          // fungible: return full balance
await client.RelinquishNftAsync(new Nft(nft, 1));  // NFT: return one serial
```

---

## Consensus Service (HCS)

### Create Topic
```csharp
var receipt = await client.CreateTopicAsync(new CreateTopicParams
{
    Memo = "My Topic",
    Administrator = new Endorsement(adminKey),
    Submitter = new Endorsement(submitKey),      // key required to submit messages
    RenewPeriod = TimeSpan.FromDays(90),
    RenewAccount = renewAccount
});
EntityId topicId = receipt.Topic;
```

### Submit Message
```csharp
// Simple
var receipt = await client.SubmitMessageAsync(topic,
    Encoding.UTF8.GetBytes("Hello HCS!"));

// With full params
var receipt = await client.SubmitMessageAsync(new SubmitMessageParams
{
    Topic = topic,
    Message = Encoding.UTF8.GetBytes("Hello HCS!"),
    Signatory = new Signatory(submitKey)
});

// Large message (auto-segmented)
var receipts = await client.SubmitLargeMessageAsync(
    topic, largePayload, segmentSize: 1024, signatory: new Signatory(submitKey));
```

### Subscribe to Topic (MirrorGrpcClient)
```csharp
var channel = Channel.CreateUnbounded<TopicMessage>();
_ = stream.SubscribeTopicAsync(new SubscribeTopicParams
{
    Topic = topic,
    MessageWriter = channel.Writer,
    Starting = ConsensusTimeStamp.MinValue  // from beginning
});

await foreach (var msg in channel.Reader.ReadAllAsync())
{
    Console.WriteLine(Encoding.UTF8.GetString(msg.Message.Span));
}
```

### Update / Delete Topic
```csharp
await client.UpdateTopicAsync(new UpdateTopicParams { Topic = topic, Memo = "New memo" });
await client.DeleteTopicAsync(topic);
```

### Query Topic Info
```csharp
TopicInfo info = await client.GetTopicInfoAsync(topic);
```

---

## Smart Contracts

### Create Contract (from file on network)
```csharp
var receipt = await client.CreateContractAsync(new CreateContractParams
{
    File = fileWithBytecode,
    Gas = 200_000,
    Administrator = new Endorsement(adminKey),
    RenewPeriod = TimeSpan.FromDays(90),
    ConstructorArgs = new object[] { arg1, arg2 }
});
EntityId contractId = receipt.Contract;
```

### Call Contract (state-changing)
```csharp
var receipt = await client.CallContractAsync(new CallContractParams
{
    Contract = contractId,
    Gas = 100_000,
    MethodName = "transfer",
    MethodArgs = new object[] { toAddress, amount }
});
```

### Query Contract (read-only)
```csharp
ContractCallResult result = await client.QueryContractAsync(new QueryContractParams
{
    Contract = contractId,
    Gas = 50_000,
    MethodName = "balanceOf",
    MethodArgs = new object[] { address }
});
```

### EVM Transaction
```csharp
var receipt = await client.ExecuteEvmTransactionAsync(new EvmTransactionParams { ... });
```

### Query Contract Info
```csharp
ContractInfo info = await client.GetContractInfoAsync(contractId);
ReadOnlyMemory<byte> bytecode = await client.GetContractBytecodeAsync(contractId);
```

---

## File Service

### Create File
```csharp
var receipt = await client.CreateFileAsync(new CreateFileParams
{
    Contents = Encoding.UTF8.GetBytes("file content"),
    Endorsements = new[] { new Endorsement(adminKey) },
    Memo = "My file"
});
EntityId fileId = receipt.File;
```

### Append / Update / Delete
```csharp
await client.AppendFileAsync(new AppendFileParams
{
    File = fileId,
    Contents = moreBytes
});

await client.UpdateFileAsync(new UpdateFileParams
{
    File = fileId,
    Contents = newBytes
});

await client.DeleteFileAsync(new DeleteFileParams { File = fileId });
```

### Query File
```csharp
FileInfo info = await client.GetFileInfoAsync(fileId);
ReadOnlyMemory<byte> content = await client.GetFileContentAsync(fileId);
```

---

## Scheduled Transactions

### Schedule Any Transaction
```csharp
// Wrap any transaction params for deferred execution
var receipt = await client.ScheduleAsync(new ScheduleParams
{
    Transaction = new TransferParams
    {
        CryptoTransfers = new[]
        {
            new CryptoTransfer(sender, -1000),
            new CryptoTransfer(receiver, 1000)
        }
    },
    Memo = "Scheduled transfer",
    Payer = payerForExecution,
    Expiration = new ConsensusTimeStamp(DateTime.UtcNow.AddHours(24)),
    DelayExecution = false  // execute as soon as all sigs collected
});
EntityId scheduleId = receipt.Schedule;
TransactionId executionTxId = receipt.ScheduledTransactionId; // id of the
// deferred tx — use with GetReceiptAsync/GetTransactionRecordAsync after execution

// Convenience: schedule any TransactionParams directly
var receipt = await client.ScheduleAsync(someTransactionParams);
```

### Sign / Delete Schedule
```csharp
await client.SignScheduleAsync(scheduleId);
await client.DeleteScheduleAsync(scheduleId);
```

### Query Schedule Info
```csharp
ScheduleInfo info = await client.GetScheduleInfoAsync(scheduleId);
```

---

## Network Utilities

### Query Network State
```csharp
ExchangeRates rates = await client.GetExchangeRatesAsync();
FeeSchedules fees = await client.GetFeeSchedulesAsync();
VersionInfo version = await client.GetVersionInfoAsync();
ConsensusNodeInfo[] book = await client.GetAddressBookAsync();
long pingMs = await client.PingAsync();
```

### Pseudo-Random Number
```csharp
// Unbounded: 48 bytes of randomness (retrieve via mirror record)
var receipt = await client.GeneratePseudoRandomNumberAsync(new PseudoRandomNumberParams());

// Bounded: integer in [0, maxValue)
var receipt = await client.GeneratePseudoRandomNumberAsync(new PseudoRandomNumberParams
{
    MaxValue = 100
});
```

### Atomic Batched Transactions
```csharp
// All-or-nothing: every inner transaction succeeds or they all revert
var receipt = await client.ExecuteAsync(new BatchedTransactionParams
{
    TransactionParams = new TransactionParams[]
    {
        new TransferParams { CryptoTransfers = new[] {
            new CryptoTransfer(sender, -100_000_000),
            new CryptoTransfer(receiver, 100_000_000)
        }},
        new CreateAccountParams {
            Endorsement = new Endorsement(publicKey),
            InitialBalance = 10_000_000
        }
    }
});
```

### External Transaction Relay
```csharp
// Forward a pre-built signed transaction to the network (precheck only)
ResponseCode code = await client.SubmitExternalTransactionAsync(signedTransactionBytes);

// Full flow — wait for receipt
TransactionReceipt receipt = await client.ExecuteExternalTransactionAsync(signedTransactionBytes);
```

### Mnemonic / Key Derivation
```csharp
var mnemonic = new Mnemonic(words, passphrase);
var (publicKey, privateKey) = mnemonic.GenerateKeyPair(KeyDerivationPath.HashPack);
```

---

## Address Book (Privileged)

```csharp
// Add a new consensus node (council-authorized payer required)
var receipt = await client.AddConsensusNodeAsync(new AddConsensusNodeParams
{
    Account = nodeAccount,
    Description = "Operator node",
    GossipEndpoints = new[] { new Uri("tcp://10.0.0.1:50111") },
    ServiceEndpoints = new[] { new Uri("https://rpc.example.com:50211") },
    GossipCaCertificate = caCertBytes,
    AdminKey = new Endorsement(adminKey)
});
ulong nodeId = receipt.NodeId;

// Update node
await client.UpdateConsensusNodeAsync(new UpdateConsensusNodeParams
{
    NodeId = nodeId, Description = "Renamed" });

// Remove node
await client.DeleteConsensusNodeAsync(nodeId);
```

---

## Receipts & Records

```csharp
// Get receipt for a known transaction ID
TransactionReceipt receipt = await client.GetReceiptAsync(txId);

// Get detailed record (includes transfers, fees, consensus timestamp)
TransactionRecord record = await client.GetTransactionRecordAsync(txId);

// Get all child receipts/records
IReadOnlyList<TransactionReceipt> all = await client.GetAllReceiptsAsync(txId);
TransactionRecord[] records = await client.GetAccountRecordsAsync(account);
```

### TransactionReceipt Properties
| Property | Type |
|----------|------|
| `TransactionId` | `TransactionId` |
| `Status` | `ResponseCode` |
| `CurrentExchangeRate` | `ExchangeRate?` |
| `NextExchangeRate` | `ExchangeRate?` |

### TransactionRecord extends TransactionReceipt
| Property | Type |
|----------|------|
| `Hash` | `ReadOnlyMemory<byte>` |
| `Consensus` | `ConsensusTimeStamp?` |
| `Memo` | `string` |
| `Fee` | `ulong` |
| `Transfers` | `ReadOnlyDictionary<EntityId, long>` |
| `TokenTransfers` | `IReadOnlyList<TokenTransfer>` |
| `NftTransfers` | `IReadOnlyList<NftTransfer>` |

---

## Mirror Node REST (MirrorRestClient)

```csharp
var mirror = new MirrorRestClient(new HttpClient
{
    BaseAddress = new Uri("https://testnet.mirrornode.hedera.com")
});
```

All list queries accept a `params IMirrorQueryParameter[]` tail; examples below
show representative filters only.

### Accounts
```csharp
AccountData? account = await mirror.GetAccountAsync(accountId);
IAsyncEnumerable<AccountData> accounts = mirror.GetAccountsAsync(
    AccountPublicKeyFilter.Is(endorsement),
    AccountBalanceFilter.After(100_000_000),
    new PageLimit(50));

IAsyncEnumerable<CryptoAllowanceData>  hbarAllowances  = mirror.GetAccountCryptoAllowancesAsync(account);
IAsyncEnumerable<TokenAllowanceData>   tokenAllowances = mirror.GetAccountTokenAllowancesAsync(account);
IAsyncEnumerable<NftAllowanceData>     nftAsOwner      = mirror.GetAccountNftAllowancesAsOwnerAsync(account);
IAsyncEnumerable<NftAllowanceData>     nftAsSpender    = mirror.GetAccountNftAllowancesAsSpenderAsync(account);
IAsyncEnumerable<StakingRewardData>    rewards         = mirror.GetAccountStakingRewardsAsync(account);
IAsyncEnumerable<TokenHoldingData>     holdings        = mirror.GetAccountTokenHoldingsAsync(account);
long?                                  balance         = await mirror.GetAccountTokenBalanceAsync(account, token);
```

### Tokens and NFTs
```csharp
TokenData?                       token  = await mirror.GetTokenAsync(tokenId);
IAsyncEnumerable<TokenSummaryData> tokens = mirror.GetTokensAsync(
    TokenTypeFilter.Fungible,
    TokenNameFilter.Contains("USD"));

NftData?                      nft          = await mirror.GetNftAsync(new Nft(tokenId, serial));
IAsyncEnumerable<NftData>     byAccount    = mirror.GetAccountNftsAsync(account);
IAsyncEnumerable<NftData>     byCollection = mirror.GetTokenNftsAsync(tokenId);
IAsyncEnumerable<NftTransactionData> history = mirror.GetNftTransactionHistoryAsync(new Nft(tokenId, serial));
IAsyncEnumerable<AccountBalanceData> holders = mirror.GetTokenHoldersSnapshotAsync(tokenId, asOf);
```

### Airdrops
```csharp
IAsyncEnumerable<TokenAirdropData> outstanding = mirror.GetAccountOutstandingAirdropsAsync(account);
IAsyncEnumerable<TokenAirdropData> pending     = mirror.GetAccountPendingAirdropsAsync(account);
```

### Transactions
```csharp
TransactionDetailData[]  group = await mirror.GetTransactionGroupAsync(txId);
TransactionDetailData?   one   = await mirror.GetTransactionAsync(consensusTimestamp);
IAsyncEnumerable<TransactionDetailData> byAccount = mirror.GetTransactionsAsync(
    AccountFilter.Is(account),
    ResultFilter.Success,
    TransferDirectionFilter.Credit,
    TransactionTypeFilter.CryptoTransfer,
    OrderBy.Descending,
    new PageLimit(25));
IAsyncEnumerable<TransactionDetailData> allFailed = mirror.GetTransactionsAsync(
    ResultFilter.Fail,
    TimestampFilter.After(since));
ConsensusTimeStamp latest = await mirror.GetLatestConsensusTimestampAsync();
```

### HCS
```csharp
TopicData?         topic     = await mirror.GetTopicAsync(topicId);
TopicMessageData?  bySeq     = await mirror.GetTopicMessageAsync(topicId, sequenceNumber);
TopicMessageData?  byTs      = await mirror.GetTopicMessageAsync(consensusTimestamp);
IAsyncEnumerable<TopicMessageData> messages = mirror.GetTopicMessagesAsync(
    topicId, TimestampFilter.After(since), SequenceNumberFilter.OnOrAfter(100));
```

### Contracts
```csharp
ContractData?                 contract  = await mirror.GetContractAsync(contractId);
IAsyncEnumerable<ContractData> contracts = mirror.GetContractsAsync();

// Results
IAsyncEnumerable<ContractResultData> byContract   = mirror.GetContractResultsAsync(contractId);
ContractResultData? byTimestamp = await mirror.GetContractResultByTimestampAsync(contractId, ts);
ContractResultData? byHash      = await mirror.GetContractResultByTransactionHashAsync(evmHash);
ContractResultData? byTxId      = await mirror.GetContractResultByTransactionIdAsync(txId);
ContractResultData? byBlockIdx  = await mirror.GetContractResultByBlockAndPositionAsync(blockHash, idx);
IAsyncEnumerable<ContractResultData> byBlock = mirror.GetContractResultsByBlockHashAsync(blockHash);
IAsyncEnumerable<ContractResultData> all     = mirror.GetAllContractResultsAsync();

// Logs, actions, opcode traces
IAsyncEnumerable<ExtendedContractLogData> logs    = mirror.GetContractLogEventsAsync(contractId);
IAsyncEnumerable<ExtendedContractLogData> allLogs = mirror.GetAllContractLogEventsAsync();
IAsyncEnumerable<ContractActionData>      actions = mirror.GetContractActionsByTransactionHashAsync(evmHash);
OpcodesData?                              opcodes = await mirror.GetContractOpcodesByTransactionHashAsync(
    evmHash,
    OpcodeMemoryProjectionFilter.Include,
    OpcodeStackProjectionFilter.Include,
    OpcodeStorageProjectionFilter.Include);

ContractStateData? state = await mirror.GetContractStateAsync(contractId, position, filters);
EncodedParams      call  = await mirror.CallEvmAsync(evmCallData);
long               gas   = await mirror.EstimateGasAsync(fromEvm, callParams);
BigInteger         chain = await mirror.GetChainIdAsync();
```

### Blocks
```csharp
BlockData?                  byNumber = await mirror.GetBlockAsync(blockNumber);
BlockData?                  byHash   = await mirror.GetBlockAsync(blockhash);
BlockData?                  latest   = await mirror.GetLatestBlockAsync();
BlockData?                  asOf     = await mirror.GetLatestBlockBeforeConsensusAsync(ts);
IAsyncEnumerable<BlockData> blocks   = mirror.GetBlocksAsync(OrderBy.Descending, new PageLimit(50));
```

### Schedules
```csharp
ScheduleData?                   schedule  = await mirror.GetScheduleAsync(scheduleId);
IAsyncEnumerable<ScheduleData>  schedules = mirror.GetSchedulesAsync(AccountFilter.Is(creator));
```

### Network
```csharp
IAsyncEnumerable<ConsensusNodeData> nodes = mirror.GetConsensusNodesAsync();
IReadOnlyDictionary<ConsensusNodeEndpoint, long> active =
    await mirror.GetActiveConsensusNodesAsync(timeoutMs);
NetworkStakeData?  stake    = await mirror.GetNetworkStakeAsync();
NetworkSupplyData? supply   = await mirror.GetNetworkSupplyAsync();
ExchangeRateData?  rate     = await mirror.GetExchangeRateAsync();
NetworkFeesData?   fees     = await mirror.GetLatestNetworkFeesAsync();
```

### Filter catalog (`Hiero.Mirror.Filters` / `Hiero.Mirror.Paging`)

| Category | Members |
|---------|---------|
| Paging | `PageLimit(n)`, `OrderBy.Ascending`, `OrderBy.Descending` |
| 6-operator (`.Is` `.After` `.OnOrAfter` `.Before` `.OnOrBefore` `.NotIs`) | `TimestampFilter`, `SequenceNumberFilter`, `SerialNumberFilter`, `AccountBalanceFilter`, `BlockNumberFilter`, `AccountFilter`, `TokenFilter`, `SpenderFilter`, `ContractFilter`, `ContractActionIndexFilter`, `ScheduleFilter` |
| 5-operator (no `NotIs`) | `NodeFilter`, `ContractLogIndexFilter` (latter requires `TimestampFilter` in same request) |
| Equality-only (`.Is`) | `AccountPublicKeyFilter`, `PublicKeyFilter`, `EvmSenderFilter`, `EvmTopicFilter`, `BlockHashFilter`, `TransactionHashFilter`, `SlotFilter`, `FileFilter`, `TransactionIndexFilter`, `SenderFilter`, `ReceiverFilter` |
| Enum-like (static members) | `ResultFilter`, `TokenTypeFilter`, `TransferDirectionFilter`, `TransactionTypeFilter` |
| Substring | `TokenNameFilter.Contains(fragment)` |
| Projections (`IMirrorProjection`) | `BalanceProjectionFilter`, `InternalProjectionFilter`, `HbarTransferProjectionFilter`, `MessageEncodingProjectionFilter`, `OpcodeMemoryProjectionFilter`, `OpcodeStackProjectionFilter`, `OpcodeStorageProjectionFilter` |

---

## Common Patterns

### Per-call configuration override
```csharp
var receipt = await client.TransferAsync(from, to, amount, ctx =>
{
    ctx.FeeLimit = 500_000_000;
    ctx.Memo = "special transfer";
});
```

### Clone client with different settings
```csharp
var child = client.Clone(ctx => ctx.FeeLimit = 500_000_000);
```

### Async signing callback
```csharp
var signatory = new Signatory(async invoice =>
{
    // sign externally (HSM, KMS, hardware wallet, etc.)
    var signature = await externalSigner.SignAsync(invoice.TxBytes);
    invoice.AddSignature(KeyType.Ed25519, publicKey, signature);
});
```

### Error handling
```csharp
try
{
    await client.TransferAsync(from, to, amount);
}
catch (PrecheckException ex)
{
    // Transaction rejected before consensus (e.g., insufficient fee)
    Console.WriteLine($"Precheck failed: {ex.Status}");
}
catch (TransactionException ex)
{
    // Transaction reached consensus but failed
    Console.WriteLine($"Transaction failed: {ex.Status}");
}
catch (ConsensusException ex)
{
    // Network communication error
}
```
