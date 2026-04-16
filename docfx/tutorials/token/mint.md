---
title: Mint Tokens
---

# Mint Tokens

Minting adds new units to a fungible token's treasury. Only the holder of the token's **supply key** can mint. The new tokens are deposited directly into the treasury account, increasing `Circulation`.

## Code

```csharp
// Mint 500,000 additional tokens (smallest unit) into the treasury.
// The client's Signatory must satisfy the token's SupplyEndorsement.
TokenReceipt receipt = await client.MintTokenAsync(token, 500_000);
Console.WriteLine($"New circulation: {receipt.Circulation}");
```

If you need to attach an explicit supply-key signatory (because it differs from the payer), use the params overload:

```csharp
TokenReceipt receipt = await client.MintTokenAsync(new MintTokenParams
{
    Token = token,
    Amount = 500_000,
    Signatory = new Signatory(supplyKey)
});
```

## Key points

- **Amount** is in the smallest denomination. With 2 decimals, minting 500,000 adds 5,000.00 display tokens.
- The token must have been created with a `SupplyEndorsement`; tokens created without one cannot be minted.
- After minting, the new supply is reflected in `receipt.Circulation`.

## See also

- [Create a fungible token](create.md) — sets up the supply key
- [Burn tokens](~/api/Hiero.BurnTokenParams.yml) — the inverse of minting
- [`MintTokenParams` API reference](~/api/Hiero.MintTokenParams.yml)
