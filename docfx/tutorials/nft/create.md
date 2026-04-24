---
title: Create an NFT Collection
---

# Create an NFT Collection

An NFT collection (token class) defines the shared metadata — name, symbol, max supply, and management keys — for all NFTs minted under it. Individual NFTs are created later via [minting](mint.md). The `Administrator` key controls updates to the collection, while the `SupplyEndorsement` key controls minting and burning.

## Code

```csharp
var receipt = await client.CreateNftAsync(new CreateNftParams
{
    Name = "Sample NFTs",
    Symbol = "SNFT",
    Ceiling = 100,                         // max 100 NFTs in this collection
    Treasury = treasury,
    Administrator = adminEndorsement,
    SupplyEndorsement = supplyEndorsement,
    Memo = "My NFT collection"
});
Console.WriteLine($"Collection ID: {receipt.Token}");
```

## What each parameter does

| Parameter | Description |
|-----------|-------------|
| `Name` | Human-readable collection name. |
| `Symbol` | Short ticker (up to 100 characters). |
| `Ceiling` | Maximum number of NFTs that can be minted. Set to 0 for unlimited. |
| `Treasury` | Account that receives newly minted NFTs. |
| `Administrator` | Key authorized to update or delete the collection. Omit for immutable. |
| `SupplyEndorsement` | Key authorized to mint and burn. Required for minting. |
| `ConfiscateEndorsement` | Optional. Key authorized to forcibly reclaim NFTs from any holder. |
| `MetadataEndorsement` | Optional. Key authorized to update individual NFT metadata after minting. |

## See also

- [Mint an NFT](mint.md)
- [Transfer an NFT](transfer.md)
- [`CreateNftParams` API reference](~/api/Hiero.CreateNftParams.yml)
