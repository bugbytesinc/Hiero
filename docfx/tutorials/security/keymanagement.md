---
title: Key Management
---

# Key Management

Private keys authorize every transaction on Hedera. This guide covers how to load them safely in development and production — and how to avoid ever having them in memory at all.

> [!WARNING]
> Never hardcode private keys as string literals. Every example in this documentation uses environment variables or configuration providers instead.

## Load from environment variables

The simplest approach for CI/CD, containers, and local dev:

```csharp
var privateKeyHex = Environment.GetEnvironmentVariable("HIERO_PAYER_KEY")
    ?? throw new InvalidOperationException("HIERO_PAYER_KEY is not set.");
var payerKey = Hex.ToBytes(privateKeyHex);

await using var client = new ConsensusClient(ctx =>
{
    ctx.Endpoint = new ConsensusNodeEndpoint(
        new EntityId(0, 0, 3),
        new Uri("https://0.testnet.hedera.com:50211"));
    ctx.Payer = new EntityId(0, 0, payerAccountNum);
    ctx.Signatory = new Signatory(payerKey);
});
```

## Load from `IConfiguration` (appsettings.json)

For ASP.NET Core or Generic Host applications:

```json
// appsettings.json (DO NOT commit real keys — use User Secrets or a vault)
{
  "Hedera": {
    "PayerAccountId": "0.0.12345",
    "PayerPrivateKey": "302e..."
  }
}
```

```csharp
var config = builder.Configuration;
var payerNum = long.Parse(config["Hedera:PayerAccountId"]!.Split('.')[2]);
var payerKey = Hex.ToBytes(config["Hedera:PayerPrivateKey"]!);
```

## Load from .NET User Secrets (development only)

User Secrets keep keys out of source control during local development:

```bash
dotnet user-secrets init
dotnet user-secrets set "Hedera:PayerPrivateKey" "302e..."
```

The value appears in `IConfiguration` identically to `appsettings.json` but is stored in your OS user profile, not in the project directory.

## Key formats

| Format | Description | Example prefix |
|--------|-------------|----------------|
| DER hex | Standard format from the Hedera portal | `302e0201...` (48+ hex chars) |
| Raw 32-byte hex | Just the private scalar | `a0b1c2...` (64 hex chars) |
| PEM | Base64-armored DER | `-----BEGIN PRIVATE KEY-----` |

The Hiero SDK's [`Hex.ToBytes`](xref:Hiero.Hex) accepts hex strings. Both DER and raw formats work with [`Signatory`](xref:Hiero.Signatory) — DER is preferred because it encodes the key type (Ed25519 vs ECDSA).

## Production: external signing via async callback

For production, avoid loading the private key into application memory at all. The [`Signatory`](xref:Hiero.Signatory) type accepts an `async` callback that signs externally — plug in Azure Key Vault, AWS KMS, HashiCorp Vault, or a hardware wallet:

```csharp
var signatory = new Signatory(async invoice =>
{
    // invoice.TxBytes contains the transaction bytes to sign
    var signature = await keyVault.SignAsync("hedera-payer-key", invoice.TxBytes);
    invoice.AddSignature(KeyType.Ed25519, publicKeyBytes, signature);
});

await using var client = new ConsensusClient(ctx =>
{
    ctx.Endpoint = endpoint;
    ctx.Payer = payerAccount;
    ctx.Signatory = signatory;  // key never leaves the vault
});
```

This pattern means the SDK never sees a private key byte — only the signed output.

## Multi-key scenarios

Some transactions require multiple signers (e.g., the sender and the payer are different accounts). Compose signatories:

```csharp
// Both keys sign every transaction
var combined = new Signatory(payerKey, senderKey);

// Mix local keys with external signers
var combined = new Signatory(
    new Signatory(localKey),
    new Signatory(async invoice => { /* vault sign */ }));
```

## Mnemonic / seed phrase derivation

Derive key pairs from a BIP-39 mnemonic:

```csharp
var mnemonic = new Mnemonic(wordList, passphrase);
var (publicKey, privateKey) = mnemonic.GenerateKeyPair(KeyDerivationPath.HashPack);
```

[`KeyDerivationPath`](xref:Hiero.KeyDerivationPath) presets include paths compatible with HashPack and other Hedera wallets.

## See also

- [Network configuration](../network.md) — where to point your client
- [Dependency injection](../di.md) — registering clients in ASP.NET Core
- [`Signatory` API reference](~/api/Hiero.Signatory.yml)
- [`Mnemonic` API reference](~/api/Hiero.Mnemonic.yml)
