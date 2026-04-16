---
title: Airdrop Tokens
---

# Airdrop Tokens

Airdrops distribute tokens to recipients who may not have associated with the token yet. If the recipient is already associated (or has auto-association slots), the transfer completes immediately. Otherwise, a **pending airdrop** is created that the recipient can claim or the sender can cancel.

## Airdrop a fungible token

```csharp
var receipt = await client.AirdropTokenAsync(token, sender, receiver, 1_000);
Console.WriteLine($"Status: {receipt.Status}");
```

## Airdrop an NFT

```csharp
var receipt = await client.AirdropNftAsync(new Nft(collection, serial), sender, receiver);
```

## Airdrop to multiple recipients

```csharp
var receipt = await client.AirdropAsync(new AirdropParams
{
    TokenTransfers = new[]
    {
        new TokenTransfer(token, sender, -300),
        new TokenTransfer(token, recipient1, 100),
        new TokenTransfer(token, recipient2, 100),
        new TokenTransfer(token, recipient3, 100),
    }
});
```

Per-token amounts must sum to zero, just like regular transfers.

## Claim a pending airdrop

The receiver calls `ClaimAirdropAsync` to accept:

```csharp
// Constructor order: sender, receiver, token
var pending = new Airdrop(sender, receiver, token);
await client.ClaimAirdropAsync(pending);
```

## Cancel a pending airdrop

The original sender calls `CancelAirdropAsync` to take back:

```csharp
var pending = new Airdrop(sender, receiver, token);
await client.CancelAirdropAsync(pending);
```

## Key points

- The [`Airdrop`](xref:Hiero.Airdrop) record identifies a pending airdrop by `(Sender, Receiver, Token)` or `(Sender, Receiver, Nft)`.
- Airdrops that settle immediately (associated receivers) behave identically to a regular transfer — there is no "pending" state.
- Use [`RelinquishTokenAsync`](xref:Hiero.RelinquishTokenExtensions.RelinquishTokenAsync*) if a recipient later wants to return unwanted airdropped tokens to the treasury.

## See also

- [Associate tokens](../token/associate.md) — why association matters
- [Transfer tokens](../token/transfer.md) — for already-associated recipients
- [`AirdropParams` API reference](~/api/Hiero.AirdropParams.yml)
