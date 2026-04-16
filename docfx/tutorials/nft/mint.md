---
title: Mint an NFT
---

# Mint an NFT

Minting creates new NFT instances within an existing collection. Each NFT is assigned a sequential **serial number** starting at 1 and carries arbitrary **metadata bytes** (typically a JSON string or IPFS CID encoded as UTF-8).

## Mint a single NFT

```csharp
var metadata = Encoding.UTF8.GetBytes("{\"name\":\"Artifact #1\"}");
NftMintReceipt receipt = await client.MintNftAsync(nftCollection, metadata);
Console.WriteLine($"Serial: {receipt.SerialNumbers[0]}");
```

## Batch-mint several NFTs

Each entry in the `Metadata` array produces one NFT. The `SerialNumbers` in the receipt correspond 1:1 to the metadata entries:

```csharp
var receipt = await client.MintNftsAsync(new MintNftParams
{
    Token = nftCollection,
    Metadata = new[]
    {
        (ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes("{\"name\":\"NFT #1\"}"),
        (ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes("{\"name\":\"NFT #2\"}"),
        (ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes("{\"name\":\"NFT #3\"}")
    }
});
Console.WriteLine($"Serials: {string.Join(", ", receipt.SerialNumbers)}");
```

> [!NOTE]
> The single-NFT method is `MintNftAsync` (singular). The batch method is `MintNftsAsync` (plural with "s" before "Async"). The "s" is load-bearing.

## Key points

- The transaction must be signed by the collection's `SupplyEndorsement` key.
- Metadata is opaque to the network — it stores the bytes as-is. The convention is to use UTF-8 JSON or an IPFS CID string, but any byte sequence works.
- Minted NFTs are deposited into the collection's treasury account. Transfer them to other holders via [`TransferNftAsync`](xref:Hiero.TransferExtensions.TransferNftAsync*).
- If the collection has a `Ceiling`, minting beyond it fails.

## See also

- [Create an NFT collection](create.md)
- [Transfer an NFT](transfer.md)
- [`MintNftParams` API reference](~/api/Hiero.MintNftParams.yml)
