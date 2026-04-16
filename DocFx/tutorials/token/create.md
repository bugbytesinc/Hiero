---
title: Create a Fungible Token
---

# Create a Fungible Token

Fungible tokens on Hedera behave like ERC-20 tokens — every unit is identical and divisible. You configure the token's properties in a [`CreateTokenParams`](xref:Hiero.CreateTokenParams) object, then call [`CreateTokenAsync`](xref:Hiero.CreateTokenExtensions.CreateTokenAsync*) on a [`ConsensusClient`](xref:Hiero.ConsensusClient).

## Code

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        // Usage: dotnet run -- https://2.testnet.hedera.com:50211 0.0.5 0.0.1001 302e...
        var endpointUrl = args[0];
        EntityId.TryParseShardRealmNum(args[1], out var nodeAccount);
        EntityId.TryParseShardRealmNum(args[2], out var payerAccount);
        var payerKey = Hex.ToBytes(args[3]);          // DER-encoded Ed25519 private key hex

        await using var client = new ConsensusClient(ctx =>
        {
            ctx.Endpoint = new ConsensusNodeEndpoint(nodeAccount!, new Uri(endpointUrl));
            ctx.Payer = payerAccount;
            ctx.Signatory = new Signatory(payerKey);
        });

        var treasury = payerAccount!;
        var adminEndorsement = new Signatory(payerKey).GetEndorsements().First();

        var receipt = await client.CreateTokenAsync(new CreateTokenParams
        {
            Name = "My Token",
            Symbol = "MTK",
            Circulation = 1_000_000,
            Decimals = 2,
            Ceiling = 10_000_000,
            Treasury = treasury,
            Administrator = adminEndorsement,
            SupplyEndorsement = adminEndorsement,
            Memo = "Created via Hiero SDK"
        });

        Console.WriteLine($"Token ID: {receipt.Token}");
    }
}
```

## What each parameter does

| Parameter | Description |
|-----------|-------------|
| `Name` | Human-readable token name displayed in wallets and explorers. |
| `Symbol` | Ticker symbol (like a currency code), up to 100 characters. |
| `Circulation` | Initial supply in the smallest unit. With `Decimals = 2`, a circulation of 1,000,000 represents 10,000.00 tokens. |
| `Decimals` | Number of decimal places. Determines how the smallest unit maps to the display value. |
| `Ceiling` | Maximum supply the token can ever reach. Set to 0 for unlimited. |
| `Treasury` | The account that initially holds all the supply and receives minted tokens. |
| `Administrator` | Public key authorized to update or delete the token. Omit to make the token immutable. |
| `SupplyEndorsement` | Public key authorized to mint and burn. Separate from `Administrator` in production. |
| `Memo` | Optional short description (max 100 bytes). |

## See also

- [Mint additional tokens](mint.md)
- [Transfer tokens](transfer.md)
- [Associate a token with an account](associate.md)
- [`CreateTokenParams` API reference](~/api/Hiero.CreateTokenParams.yml)
