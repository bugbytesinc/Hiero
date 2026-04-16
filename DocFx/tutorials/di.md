---
title: Dependency Injection
---

# Dependency Injection

Most .NET applications use `Microsoft.Extensions.DependencyInjection`. This guide shows how to register Hiero clients so they can be injected via constructors.

## Register `ConsensusClient` as a singleton

```csharp
builder.Services.AddSingleton<ConsensusClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new ConsensusClient(ctx =>
    {
        ctx.Endpoint = new ConsensusNodeEndpoint(
            new EntityId(0, 0, long.Parse(config["Hedera:NodeAccountId"]!.Split('.')[2])),
            new Uri(config["Hedera:NodeEndpoint"]!));
        ctx.Payer = new EntityId(0, 0, long.Parse(config["Hedera:PayerAccountId"]!.Split('.')[2]));
        ctx.Signatory = new Signatory(Hex.ToBytes(config["Hedera:PayerPrivateKey"]!));
    });
});
```

> [!NOTE]
> `ConsensusClient` implements `IAsyncDisposable`. When registered as a singleton, the DI container disposes it on application shutdown. For transient or scoped lifetimes, wrap usage in `await using`.

## Register `MirrorRestClient`

`MirrorRestClient` takes a standard `HttpClient`, so it works with `IHttpClientFactory`:

```csharp
builder.Services.AddHttpClient<MirrorRestClient>((sp, http) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    http.BaseAddress = new Uri(config["Hedera:MirrorRestUrl"]!);
});
```

This gives you connection pooling, automatic DNS refresh, and built-in resilience support for free.

## Register `MirrorGrpcClient`

```csharp
builder.Services.AddSingleton<MirrorGrpcClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new MirrorGrpcClient(ctx =>
    {
        ctx.Uri = new Uri(config["Hedera:MirrorGrpcUrl"]!);
    });
});
```

## Consume in a service

```csharp
public class TokenService
{
    private readonly ConsensusClient _client;

    public TokenService(ConsensusClient client) => _client = client;

    public async Task<EntityId> CreateTokenAsync(string name, string symbol)
    {
        var receipt = await _client.CreateTokenAsync(new CreateTokenParams
        {
            Name = name,
            Symbol = symbol,
            Circulation = 1_000_000,
            Decimals = 2,
            Treasury = /* treasury account */,
            // ...
        });
        return receipt.Token;
    }
}

builder.Services.AddScoped<TokenService>();
```

## Per-request overrides with `Clone`

The singleton client holds shared configuration (endpoint, payer, signatory). Use [`Clone`](xref:Hiero.ConsensusClient.Clone*) for request-scoped overrides without creating a new gRPC channel:

```csharp
public async Task TransferWithCustomFee(EntityId from, EntityId to, long amount)
{
    // child inherits the parent's endpoint, payer, and signatory
    // but has its own fee limit — no new connection overhead
    await using var child = _client.Clone(ctx => ctx.FeeLimit = 500_000_000);
    await child.TransferAsync(from, to, amount);
}
```

## Configuration shape

A minimal `appsettings.json` section:

```json
{
  "Hedera": {
    "NodeEndpoint": "https://0.testnet.hedera.com:50211",
    "NodeAccountId": "0.0.3",
    "PayerAccountId": "0.0.12345",
    "PayerPrivateKey": "302e...",
    "MirrorRestUrl": "https://testnet.mirrornode.hedera.com",
    "MirrorGrpcUrl": "https://hcs.testnet.mirrornode.hedera.com:5600"
  }
}
```

> [!WARNING]
> Do not commit real keys to `appsettings.json`. Use [User Secrets](security/keymanagement.md) for development and a vault for production.

## See also

- [Key management](security/keymanagement.md) — secure key loading
- [Network configuration](network.md) — choosing endpoints
- [`ConsensusClient` API reference](~/api/Hiero.ConsensusClient.yml)
