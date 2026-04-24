---
title: Transfer an NFT
---

# Transfer an NFT

NFTs are transferred by specifying the collection token ID and serial number. The receiver must be associated with the NFT's collection (same rules as fungible tokens — see [Associate](../token/associate.md)).

## Simple transfer

```csharp
var nft = new Nft(nftCollection, serialNumber);
var receipt = await client.TransferNftAsync(nft, sender, receiver);
Console.WriteLine($"Status: {receipt.Status}");
```

## Atomic multi-NFT or mixed transfer

Use [`TransferParams`](xref:Hiero.TransferParams) for multiple NFTs, or a mix of HBAR, fungible tokens, and NFTs in one atomic transaction:

```csharp
var receipt = await client.TransferAsync(new TransferParams
{
    NftTransfers = new[]
    {
        new NftTransfer(new Nft(collection, 1), sender, receiver),
        new NftTransfer(new Nft(collection, 2), sender, receiver),
    },
    Signatory = new Signatory(senderKey)
});
```

## Allowance-based transfers

To transfer on behalf of the NFT owner (e.g., a marketplace contract acting as an approved operator), the owner must first grant an allowance:

```csharp
// Owner grants an NFT allowance for specific serials
await client.AllocateAllowanceAsync(new AllowanceParams
{
    NftAllowances = new[]
    {
        new NftAllowance(collection, owner, spender, new long[] { 1, 2 })
    }
});

// Spender (operator) executes the transfer using the delegated flag
await client.TransferAsync(new TransferParams
{
    NftTransfers = new[]
    {
        new NftTransfer(new Nft(collection, 1), owner, buyer, delegated: true)
    },
    Signatory = new Signatory(spenderKey)
});
```

## Key points

- The [`Nft`](xref:Hiero.Nft) type is a pair of `(EntityId Token, long SerialNumber)`. Format: `"0.0.token#serial"`.
- The sender's key (or an approved spender's key with `delegated: true`) must sign.
- If the receiver is not associated with the collection, the transfer fails with `TokenNotAssociatedToAccount`. Use [`AirdropNftAsync`](xref:Hiero.AirdropExtensions.AirdropNftAsync*) to create a pending airdrop instead.

## See also

- [Create an NFT collection](create.md)
- [Mint an NFT](mint.md)
- [Associate tokens](../token/associate.md)
- [`TransferParams` API reference](~/api/Hiero.TransferParams.yml)
