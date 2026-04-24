---
title: Transfer Tokens
---

# Transfer Tokens

Transferring moves fungible token units from one account to another. The **receiver must already be associated** with the token (see [Associate](associate.md)) or have automatic-association slots available on their account.

## Simple transfer

```csharp
// Transfer 1,000 units of a fungible token between two accounts.
var receipt = await client.TransferTokenAsync(token, sender, receiver, 1_000);
Console.WriteLine($"Status: {receipt.Status}");
```

> [!NOTE]
> The method name is `TransferTokenAsync` — not `TransferAsync`, which is reserved for HBAR transfers.

## Multi-party transfer

Use [`TransferParams`](xref:Hiero.TransferParams) for complex atomic transfers — multiple senders, multiple tokens, or a mix of HBAR and tokens in one transaction:

```csharp
var receipt = await client.TransferAsync(new TransferParams
{
    TokenTransfers = new[]
    {
        new TokenTransfer(token, sender,    -500),
        new TokenTransfer(token, receiver1,  250),
        new TokenTransfer(token, receiver2,  250),
    },
    Signatory = new Signatory(senderKey)
});
```

Per-token amounts must sum to zero, just like HBAR `CryptoTransfers`.

## Key points

- The sender's signing key must authorize the transaction (via the client context or `TransferParams.Signatory`).
- If the receiver has not associated with the token and has no auto-association slots, the transfer will fail with `TokenNotAssociatedToAccount`.
- Use [`AirdropTokenAsync`](xref:Hiero.AirdropExtensions.AirdropTokenAsync*) instead if you want the network to create a pending airdrop rather than fail when the receiver isn't associated.

## See also

- [Associate a token with an account](associate.md)
- [Airdrop tokens](../airdrop/send.md) — transfers that tolerate unassociated receivers
- [`TransferParams` API reference](~/api/Hiero.TransferParams.yml)
