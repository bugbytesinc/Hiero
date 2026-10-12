---
title: Block Node Streaming (Experimental)
---

# Block Node Streaming (Experimental)

> [!WARNING]
> The Block Node service and the `BlockNodeClient` API are **experimental**
> (`HIERO001`). Block Nodes are in preview on the Hedera networks and the
> upstream protocol is still evolving; this API may change in any minor
> release. Opt in per-project with
> `<NoWarn>$(NoWarn);HIERO001</NoWarn>` or per-file with
> `#pragma warning disable HIERO001`.

**Block Nodes** ([HIP-1081](https://hips.hedera.com/hip/hip-1081)) serve the
**block stream** ([HIP-1056](https://hips.hedera.com/hip/hip-1056)) — the
unified, cryptographically-provable stream of events, transactions, results,
state changes and traces that replaces the record stream.
[`BlockNodeClient`](xref:Hiero.BlockNodeClient) is the SDK's fourth client,
covering:

| Operation | Method |
|-----------|--------|
| Node status (available block range) | `GetServerStatusAsync`, `GetServerStatusDetailAsync` |
| Single block retrieval | `GetBlockAsync`, `GetLatestBlockAsync` |
| Whole-block streaming (assembled, verified, auto-recovering) | `SubscribeBlockStreamAsync` |
| Item-level streaming (low-latency, unverified) | `SubscribeBlockItemStreamAsync` |
| Block contents proofs | `GetBlockContentsProofAsync` |
| State snapshot references | `GetStateSnapshotAsync`, `GetLatestStateSnapshotAsync` |

No payer account is needed — block node access is read-only gRPC.

## Subscribing to assembled blocks

The default mode delivers whole, structurally-validated blocks and owns the
unverified stream's sharp edges (resends, duplicates, gaps, reconnects):

[!code-csharp[Program.cs](../../../samples/BlockSubscription/Program.cs#SubscribeBlockStream)]

Some operators serve each Block Node API on its own port — mainnet's
registered block nodes do, and the HIP-1137 registry lists one endpoint per
API. Subscribing calls the status API to choose and check its starting block,
so point `Uri` at the `SUBSCRIBE_STREAM` endpoint and `StatusUri` at the
`STATUS` endpoint:

```csharp
await using var client = new BlockNodeClient(ctx =>
{
    ctx.Uri = new Uri("http://block-node:40980");        // SUBSCRIBE_STREAM
    ctx.StatusUri = new Uri("http://block-node:40982");  // STATUS
});
```

Without `StatusUri`, such a node fails the subscription with
`BlockNodeExceptionCode.NotImplemented`.

## Item-level streaming (low latency)

When milliseconds matter — e.g. monitoring EVM contract events — the
item-level mode delivers each block item the moment its frame arrives,
potentially a second or more before the block completes. In exchange there
is no buffering, deduplication or verification; consumers treat items as
provisional and reconcile when `IsBlockComplete` signals:

[!code-csharp[Program.cs](../../../samples/BlockSubscription/Program.cs#SubscribeBlockItemStream)]

## Verification levels and checkpoints

The subscribe stream is forwarded by the node *unverified*. The client
offers three levels of scrutiny via
`SubscribeBlockStreamParams.VerificationLevel`:

| Level | What is checked |
|-------|-----------------|
| `None` | Nothing — deliver whatever completes. |
| `Structural` (default) | Header first, footer present, proof last, contiguous numbers. |
| `HashChain` | Structural **plus** SHA-384 merkle root recomputation: every block's `previous_block_root_hash` must match the root computed for the preceding block. |

Under `HashChain`, each delivered block carries its computed root in
`Block.VerifiedRootHash`. Persist it with the block number and resume
across restarts with **cryptographic** continuity:

```csharp
// While processing:
lastCheckpoint = new BlockStreamCheckpoint(block.Number, block.VerifiedRootHash);

// After a restart:
await client.SubscribeBlockStreamAsync(new SubscribeBlockStreamParams
{
    ResumeFromCheckpoint = lastCheckpoint,     // starts at BlockNumber + 1,
    VerificationLevel = BlockVerificationLevel.HashChain,
    BlockWriter = channel.Writer               // first block must chain from RootHash
});
```

A hash mismatch raises `BlockNodeException` with
`BlockNodeExceptionCode.VerificationFailed` and is never automatically
resubscribed — the same node would return the same bytes. A block this SDK
cannot hash with certainty — filtered or redacted items, or item types newer
than this build — raises `BlockNodeExceptionCode.Unverifiable` instead, under
the same no-resubscribe rule: it means *this SDK cannot check the block*, not
*the block is wrong*.

Roots are computed with the 16-branch block root tree introduced in consensus
node v0.77, which also covers mainnet's entire wrapped record-stream history.
Blocks produced by earlier consensus node releases — for example a local
network that has not been upgraded — do not verify. Single blocks
can also be verified on retrieval with
`await client.GetVerifiedBlockAsync(number)`, which recomputes the root
and confirms it against the following block (fetched as an anchor) —
so the block stream's tip cannot be verified this way yet, and there is
no verified-latest counterpart.

TSS signature verification (proving the root was signed by the network,
not just internally consistent) is planned as a separate optional
package once the upstream signature scheme stabilizes.

## The Block escape hatch

`Block` promotes the fields every consumer needs (number, timestamp,
items, footer, proof) into SDK types; everything else — transaction
results, state changes, traces — is reachable losslessly through
`Block.ToProtobuf()` / `BlockItem.ToProtobuf()` while the upstream
formats stabilize.

## Discovering block nodes (HIP-1137)

Block nodes register themselves on-chain; the mirror node exposes the
registry, so discovery rides on [`MirrorRestClient`](xref:Hiero.MirrorRestClient):

```csharp
var mirror = new MirrorRestClient(httpClient);
await foreach (var node in mirror.GetRegisteredBlockNodesAsync())
{
    // Operators may publish each API on its own endpoint; use the one
    // advertised for each API the client will call.
    Uri? Advertised(string api) => node.Endpoints.FirstOrDefault(
        e => e.BlockNode?.EndpointApis.Contains(api) == true)?.ToUri();
    if (Advertised("SUBSCRIBE_STREAM") is Uri subscribeUri)
    {
        await using var bn = new BlockNodeClient(ctx =>
        {
            ctx.Uri = subscribeUri;
            ctx.StatusUri = Advertised("STATUS");  // null: status shares Uri
        });
        var status = await bn.GetServerStatusAsync();
        // ...choose a node whose range covers what you need
    }
}
```

Operators register, update and retire nodes through `ConsensusClient`
(`CreateRegisteredNodeAsync`, `UpdateRegisteredNodeAsync`,
`DeleteRegisteredNodeAsync`); on mainnet creation is currently a
privileged operation.

## Publishing a block stream (advanced)

Producer-side tooling (simulators, block-node-to-block-node repeaters)
opens a publish session. The protocol is a state machine — the node can
skip, request a resend, or declare the publisher ahead — so the session
surfaces those directives on an `Events` channel the caller MUST consume;
it performs no automatic skip/resend handling.

[!code-csharp[Program.cs](../../../samples/BlockRepeater/Program.cs#BlockRepeater)]

Note the failover pattern the repeater uses: there is no multi-node
failover machinery in the client by design — a `BlockStreamCheckpoint`
plus discovery is all that is needed to switch nodes and resume with
cryptographic continuity, which keeps failover *policy* in the
application where it belongs.

## See also

- The runnable samples: `samples/BlockSubscription/`, `samples/BlockRepeater/`
- Design and roadmap: `docs/blocknode-client-plan.md` in the repository
