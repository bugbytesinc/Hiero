# Trimmed protos and fields

This tree is upstream **minus** deprecated operations the SDK does not
implement, an internal-only node operation, a query the public networks have
retired, and protos that nothing in the tree or the SDK references — about
17,700 lines of generated C# that nothing used.

**This file is the checklist for re-applying the trims after a refresh.** It
survives re-vendoring: the script deletes `*.proto` and rewrites `README.md`,
but leaves everything else alone.

Rationale and analysis: `docs/proto-trim-plan.md`, `docs/blocknode-proto-audit.md`.

---

## What happens on the next refresh

`scripts/vendor-blocknode-protos.sh` excludes the files below from the copy
(`CN_TRIMMED_PROTOS`, `BN_TRIMMED_PROTOS`), but copies the files that *import*
them verbatim from upstream. So immediately after a refresh **the build fails
with unresolved imports.**

That failure is deliberate. It is the signal to redo the file edits in the
next section. Do not fix it by removing an exclusion — that silently restores
dead code, which is exactly what happened during the v0.74.0 refresh and went
unnoticed for weeks.

One edit has no excluded file behind it and so **no build failure** to announce
it: `LambdaSStore` in `basic_types.proto`. A refresh restores it silently — the
v0.74.0 refresh did — and only the unit tests notice.

`TrimmedProtobufTests` in `Hiero.Test.Unit` fails if any trimmed type, the
`LambdaSStore` enum value or the `hook_dispatch` oneof case comes back. It is
the backstop for all of the above.

---

## 1. Deleted files (21)

Excluded by the vendor script; nothing to do by hand.

**Deprecated operations** the SDK does not implement:

| File | Defines |
|---|---|
| `services/get_by_key.proto` | `GetByKeyQuery` / `GetByKeyResponse` |
| `services/get_by_solidity_id.proto` | `GetBySolidityIDQuery` / `…Response` |
| `services/crypto_get_live_hash.proto` | `CryptoGetLiveHashQuery` / `…Response` |
| `services/crypto_get_stakers.proto` | `CryptoGetStakersQuery` / `…Response` |
| `services/transaction_get_fast_record.proto` | `TransactionGetFastRecordQuery` / `…Response` |
| `services/network_get_execution_time.proto` | `NetworkGetExecutionTimeQuery` / `…Response` |
| `services/crypto_add_live_hash.proto` | `LiveHash`, `CryptoAddLiveHashTransactionBody` |
| `services/crypto_delete_live_hash.proto` | `CryptoDeleteLiveHashTransactionBody` |
| `services/unchecked_submit.proto` | `UncheckedSubmitBody` |

**Internal-only node operation** — upstream documents `HookDispatch` as
"(Internal-only) Dispatch a hook action"; clients never submit it:

| File | Defines |
|---|---|
| `services/hook_dispatch.proto` | `HookDispatchTransactionBody`, `HookExecution` |

**Retired by the public networks** — Hedera removed the free balance query
from testnet and mainnet in July 2026 (every node answers it `BUSY`), and the
SDK dropped `GetAccountBalanceAsync` and its siblings in 3.0.0. To bring it
back for a network that offers it again: drop the exclusion, uncomment the
`query.proto` / `response.proto` lines below, restore the `Response.cs`
header case, and remove the two names from `TrimmedProtobufTests`.

| File | Defines |
|---|---|
| `services/crypto_get_account_balance.proto` | `CryptoGetAccountBalanceQuery` / `…Response` |

**Unreferenced** — imported by no other vendored proto and used by no SDK,
test or sample code. Found by mapping each proto's generated types and walking
the compiled hand-written code's references plus the real import graph; confirmed
by building the solution and all samples without them (2026-10-06). The four
`block/stream/output/*_service.proto` files are orphaned upstream as well —
v0.74's `TransactionOutput` no longer references them — but the vendor script
always vendors `block/stream/**`, so they need the exclusion to stay out.

| File | Defines |
|---|---|
| `block/stream/output/consensus_service.proto` | `CreateTopicOutput`, `SubmitMessageOutput`, … |
| `block/stream/output/file_service.proto` | `CreateFileOutput`, `AppendFileOutput`, … |
| `block/stream/output/network_service.proto` | `FreezeOutput`, `UpdateNodeStakeOutput` |
| `block/stream/output/token_service.proto` | 17 `*TokenOutput` messages |
| `platform/event/event_consensus_data.proto` | `EventConsensusData` |
| `platform/event/gossip_event.proto` | `GossipEvent` |
| `services/auxiliary/tss/tss_encryption_key.proto` | `TssEncryptionKeyTransactionBody` |
| `services/throttle_definitions.proto` | `ThrottleDefinitions`, `ThrottleBucket`, `ThrottleGroup` |
| `../hiero-block-node/block-node/api/network-data.proto` | `NetworkData`, `NetworkConnection`, … |
| `../hiero-block-node/block-node/api/reconnect_service.proto` | `ReconnectService` (the SDK's reconnect support is deferred) |

## 2. Edits to re-apply by hand (5 files)

Convention, matching what the repo used before v0.74.0: **delete** the `import`
lines; **comment out** the field lines with a bare `//`, preserving indentation.
Commented fields keep the field number visible as a reservation and land where
upstream's line reappears, which makes the next diff readable.

### `services/query.proto`

Delete the 7 imports; comment out:

```protobuf
        //GetByKeyQuery getByKey = 1;
        //GetBySolidityIDQuery getBySolidityID = 2;
        //CryptoGetAccountBalanceQuery cryptogetAccountBalance = 7;
        //CryptoGetLiveHashQuery cryptoGetLiveHash = 10 [deprecated = true];
        //CryptoGetStakersQuery cryptoGetProxyStakers = 11 [deprecated = true];
        //TransactionGetFastRecordQuery transactionGetFastRecord = 16;
        //NetworkGetExecutionTimeQuery networkGetExecutionTime = 57 [deprecated = true];
```

### `services/response.proto`

Delete the 7 imports; comment out:

```protobuf
        //GetByKeyResponse getByKey = 1;
        //GetBySolidityIDResponse getBySolidityID = 2;
        //CryptoGetAccountBalanceResponse cryptogetAccountBalance = 7;
        //CryptoGetLiveHashResponse cryptoGetLiveHash = 10;
        //CryptoGetStakersResponse cryptoGetProxyStakers = 11;
        //TransactionGetFastRecordResponse transactionGetFastRecord = 16;
        //NetworkGetExecutionTimeResponse networkGetExecutionTime = 157;
```

> **The two files are not symmetric — do not copy between them.**
> `networkGetExecutionTime` is field **57** in `query.proto` but **157** in
> `response.proto`, and `query.proto` carries `[deprecated = true]` markers
> that `response.proto` does not.

### `services/transaction.proto`

Delete the 4 imports (the three live-hash/unchecked-submit ones and
`services/hook_dispatch.proto`); comment out, inside `TransactionBody`:

```protobuf
        //CryptoAddLiveHashTransactionBody cryptoAddLiveHash = 10 [deprecated = true];
        //CryptoDeleteLiveHashTransactionBody cryptoDeleteLiveHash = 13 [deprecated = true];
        //UncheckedSubmitBody uncheckedSubmit = 28;
        //com.hedera.hapi.node.hooks.HookDispatchTransactionBody hook_dispatch = 76;
```

`schedulable_transaction_body.proto` references none of these — no parallel
edit needed. `HookDispatch = 110` stays in `HederaFunctionality`: an enum value
costs nothing, and the network may report it.

### `services/crypto_get_info.proto`

Delete the `crypto_add_live_hash.proto` import; comment out, inside
`CryptoGetInfoResponse.AccountInfo`:

```protobuf
        //repeated LiveHash liveHashes = 14;
```

This is the only edit touching a message the SDK deserializes. It is safe:
protobuf skips unknown fields, live hashes were never enabled on a public
network so the field is never populated, and `AccountInfo` has never surfaced
it. The SDK shipped this way before v0.74.0.

### `services/basic_types.proto`

Comment out, inside `enum HederaFunctionality`:

```protobuf
    //LambdaSStore = 109;
```

Upstream marks it "(DEPRECATED) Remove once no production throttle assets
reference it." **This is the one trim a refresh reverts without a build
failure** — no excluded file stands behind it — so check for it explicitly;
`Trimmed_Proto_Lines_Stay_Trimmed` fails if it is back. The only SDK-visible
effect: `FeeSchedule` keys entries by `HederaFunctionality.ToString()`, so a
fee schedule that still lists function 109 shows it under `"109"` rather than
`"LambdaSstore"`.

---

## 3. Unused imports — automatic, nothing to do by hand

Upstream leaves behind imports it no longer uses, usually after removing the
field that needed them (`event_core.proto`'s `reserved 17` was the creator's
software version, a `SemanticVersion` from `basic_types.proto`). protoc
reports each as an "unused import" notice. The vendor script disables them —
`// import "…"; // unused upstream; disabled by scripts/vendor-blocknode-protos.sh`
— from its `UNUSED_IMPORTS` list, so the build emits **zero** protoc notices
and a new one stands out.

| File | Disabled imports |
|---|---|
| `block/stream/record_file_item.proto` | `services/transaction_contents.proto`, `services/transaction_record.proto` |
| `block/stream/state_proof.proto` | `block/stream/record_file_item.proto`, `services/timestamp.proto` |
| `platform/event/event_core.proto` | `services/basic_types.proto` |
| `../hiero-block-node/block-node/api/proof_service.proto` | `block/stream/block_proof.proto` |

Unlike the field edits above, these are re-applied by the script on every
refresh and then **checked with protoc** (the version the build uses, from
the NuGet cache). The run fails, after replacing the tree, if an entry no
longer matches (upstream dropped the import), if protoc finds an unused
import that is not listed (it prints the exact entry to add — it never adds
one itself), or if protoc errors (typically: upstream now uses a listed
import, so that entry must go). The check resolves the protos excluded in
section 1 from the upstream clone, so it works before the section 2 edits are
redone.

## 4. Verify

```sh
dotnet build Hiero.slnx                 # must succeed, with no protoc notices
dotnet test --project test/Hiero.Test.Unit/
```

`TrimmedProtobufTests` covers the trimmed types, the `LambdaSStore` enum value
and the `hook_dispatch` oneof case. If a
refresh changes `crypto_get_info.proto`, also run the account integration
tests, since that is the one parsed message involved.

## What is deliberately NOT trimmed

- **`ResponseCode`'s `EMPTY_LIVE_HASH_*` members** must stay. They are wire
  codes still present upstream, and `ResponseCode_Has_All_Protobuf_Values`
  enforces parity with `ResponseCodeEnum`.
- **`services/state/**`** — 31 of `StateChanges`' 39 imports.
- **`streams/**`** — needed by `RecordFileItem`.
- **`TokenBalance` / `TokenBalances`** (`services/basic_types.proto`). Only the
  retired balance response used them, but trimming them means two more message
  edits inside `basic_types.proto` that a refresh would restore without a build
  failure — the `LambdaSStore` hazard again — for about 600 generated lines.
  `HederaFunctionality.CryptoGetAccountBalance` stays too: an enum value costs
  nothing, and fee schedules still list it.
- **`HistoryProofConstruction`** (`services/state/history/history_types.proto`).
  It was commented out before v0.74.0, when nothing referenced it. Since then
  `state_changes.proto` uses it (`MapChangeValue.history_proof_construction_value
  = 16`), so trimming it would mean hand-editing the block stream's
  highest-churn file, inside a message that is hashed into every block root.
