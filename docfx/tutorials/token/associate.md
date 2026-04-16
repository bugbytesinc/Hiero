---
title: Associate and Dissociate Tokens
---

# Associate and Dissociate Tokens

Before an account can hold a token balance, it must **associate** with that token. Association provisions a storage slot on the account and increases its ongoing renewal cost, which is why the account itself (not the token admin) must sign.

Accounts with `AutoAssociationLimit > 0` can skip this step — the first incoming transfer auto-associates. But explicit association is more predictable and avoids surprise fees.

## Associate a single token

```csharp
// The account's signing key must authorize this.
// Argument order: (account, token) — NOT (token, account).
var receipt = await client.AssociateTokenAsync(account, token);
Console.WriteLine($"Status: {receipt.Status}");
```

> [!IMPORTANT]
> `AssociateTokenAsync` takes `(account, token)`. Its counterpart `DissociateTokenAsync` takes `(token, account)` — the argument order is **reversed**. This is a known inconsistency in the SDK API.

## Associate multiple tokens at once

```csharp
var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
{
    Account = account,
    Tokens = new[] { token1, token2, token3 }
});
```

One transaction, one fee — cheaper than three separate associations.

## Dissociate

Dissociating removes the token-balance storage slot. The account **must hold a zero balance** of the token before dissociating; otherwise the network rejects the transaction.

```csharp
// Argument order: (token, account) — opposite of AssociateTokenAsync.
var receipt = await client.DissociateTokenAsync(token, account);
```

To return a non-zero balance to the treasury before dissociating, use [`RelinquishTokenAsync`](xref:Hiero.RelinquishTokenExtensions.RelinquishTokenAsync*).

## Key points

- Association is per-account, per-token. An account associated with Token A can receive Token A but not Token B until it also associates with B.
- NFT collections use the same mechanism — associate with the collection's token ID to receive any serial from that collection.
- `AutoAssociationLimit` on [`CreateAccountParams`](xref:Hiero.CreateAccountParams) or [`UpdateAccountParams`](xref:Hiero.UpdateAccountParams) controls how many tokens can auto-associate without an explicit transaction.

## See also

- [Create a fungible token](create.md)
- [Transfer tokens](transfer.md)
- [`AssociateTokenParams` API reference](~/api/Hiero.AssociateTokenParams.yml)
- [`DissociateTokenParams` API reference](~/api/Hiero.DissociateTokenParams.yml)
