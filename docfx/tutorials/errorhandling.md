---
title: Error Handling
---

# Error Handling

Hiero throws typed exceptions that tell you exactly what went wrong, whether to retry, and what to fix.

## Exception hierarchy

| Exception | When thrown | Transient? |
|-----------|------------|------------|
| [`PrecheckException`](xref:Hiero.PrecheckException) | Gateway node rejects the transaction *before* consensus | Some codes (see below) |
| [`TransactionException`](xref:Hiero.TransactionException) | Transaction reached consensus but the result is non-success | No — fees already charged |
| [`ConsensusException`](xref:Hiero.ConsensusException) | Transaction didn't reach consensus within `TransactionDuration` | Always — safe to retry |
| [`ContractException`](xref:Hiero.ContractException) | Smart contract call/query failed (EVM revert, out-of-gas) | Rarely — usually permanent |
| [`MirrorGrpcException`](xref:Hiero.MirrorGrpcException) | Mirror gRPC stream terminated unexpectedly | `Unavailable` / `CommunicationError` yes; `TopicNotFound` no |
| [`MirrorException`](xref:Hiero.Mirror.MirrorException) | Mirror REST API returned an HTTP error | 429/503/504 yes; 400/404 no |

## Basic error handling

```csharp
try
{
    var receipt = await client.TransferAsync(from, to, amount);
}
catch (PrecheckException ex) when (ex.Status == ResponseCode.InsufficientTxFee)
{
    // The fee was too low — increase FeeLimit and retry
    Console.WriteLine($"Need at least {ex.RequiredFee} tinybars");
}
catch (PrecheckException ex) when (IsTransient(ex.Status))
{
    // Gateway is busy — back off and retry
    await Task.Delay(TimeSpan.FromSeconds(2));
}
catch (PrecheckException ex)
{
    // Permanent precheck failure — fix the request
    Console.WriteLine($"Precheck failed: {ex.Status}");
}
catch (TransactionException ex)
{
    // Reached consensus but failed — do NOT retry
    Console.WriteLine($"Transaction failed: {ex.Status}");
    Console.WriteLine($"Receipt: {ex.Receipt}");
}
catch (ConsensusException)
{
    // Timed out — always safe to retry
    Console.WriteLine("Consensus timeout, retrying...");
}

static bool IsTransient(ResponseCode code) =>
    code is ResponseCode.Busy
        or ResponseCode.PlatformTransactionNotCreated;
```

## Suppress `TransactionException`

For workflows where you want to inspect the receipt yourself rather than catching exceptions:

```csharp
var receipt = await client.TransferAsync(from, to, amount, ctx =>
{
    ctx.ThrowIfNotSuccess = false;
});

if (receipt.Status != ResponseCode.Success)
{
    Console.WriteLine($"Non-success: {receipt.Status}");
    // handle without an exception
}
```

## Retry with resilience pipelines

```csharp
builder.Services.AddResiliencePipeline("hedera", pipeline =>
{
    pipeline.AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1),
        BackoffType = DelayBackoffType.Exponential,
        ShouldHandle = new PredicateBuilder()
            .Handle<PrecheckException>(ex =>
                ex.Status is ResponseCode.Busy
                    or ResponseCode.PlatformTransactionNotCreated)
            .Handle<ConsensusException>()
    });
});
```

## Timeouts with `CancellationToken`

Most SDK methods accept a `CancellationToken` (either directly or on the `*Params` object):

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var receipt = await client.TransferAsync(from, to, amount, ctx =>
{
    // The per-call configure callback can also set transaction-level timeouts
    ctx.TransactionDuration = TimeSpan.FromSeconds(60);
});
```

## Mirror node error handling

```csharp
try
{
    var data = await mirror.GetAccountAsync(account);
}
catch (MirrorException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
{
    // Rate limited — back off and retry
    await Task.Delay(TimeSpan.FromSeconds(5));
}
catch (MirrorException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
{
    // Entity doesn't exist (or hasn't propagated to the mirror yet)
}
```

## See also

- [`PrecheckException` API reference](~/api/Hiero.PrecheckException.yml) — full `<remarks>` with code-by-code guidance
- [`TransactionException` API reference](~/api/Hiero.TransactionException.yml)
- [`ResponseCode` enum](~/api/Hiero.ResponseCode.yml) — every Hedera status code
