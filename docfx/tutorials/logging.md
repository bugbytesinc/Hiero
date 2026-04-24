---
title: Logging Integration
---

# Logging Integration

The Hiero SDK provides hooks for inspecting every protobuf message sent to and received from the Hedera network. This gives you full visibility into HAPI request/response traffic without needing a network sniffer.

## HAPI request/response hooks

[`IConsensusContext`](xref:Hiero.IConsensusContext) exposes two callbacks:

| Property | Signature | When called |
|----------|-----------|-------------|
| [`OnSendingRequest`](xref:Hiero.IConsensusContext.OnSendingRequest) | `Action<IMessage>?` | Just before the protobuf is sent to the network |
| [`OnResponseReceived`](xref:Hiero.IConsensusContext.OnResponseReceived) | `Action<int, IMessage>?` | After receiving a response; the `int` is the retry attempt (0 = first try) |

Both callbacks receive `Google.Protobuf.IMessage` — the raw protobuf object. Use `JsonFormatter` to render it as readable JSON.

### Basic example: log all HAPI traffic

```csharp
using Google.Protobuf;

var formatter = new JsonFormatter(JsonFormatter.Settings.Default);

await using var client = new ConsensusClient(ctx =>
{
    ctx.Endpoint = new ConsensusNodeEndpoint(nodeAccount, new Uri(endpointUrl));
    ctx.Payer = payerAccount;
    ctx.Signatory = new Signatory(payerKey);

    ctx.OnSendingRequest = message =>
    {
        Console.WriteLine($"[TX] {message.Descriptor.Name}");
        Console.WriteLine(formatter.Format(message));
    };

    ctx.OnResponseReceived = (retry, message) =>
    {
        Console.WriteLine($"[RX attempt {retry}] {message.Descriptor.Name}");
        Console.WriteLine(formatter.Format(message));
    };
});

// Every call now logs the raw protobuf traffic
var balance = await client.GetAccountBalanceAsync(account);
```

### Integrate with `ILogger`

Wire the hooks into your application's logging pipeline:

```csharp
builder.Services.AddSingleton<ConsensusClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<ConsensusClient>>();
    var formatter = new JsonFormatter(JsonFormatter.Settings.Default);

    return new ConsensusClient(ctx =>
    {
        ctx.Endpoint = new ConsensusNodeEndpoint(/* ... */);
        ctx.Payer = /* ... */;
        ctx.Signatory = /* ... */;

        ctx.OnSendingRequest = message =>
            logger.LogDebug("HAPI TX: {Type} {Body}",
                message.Descriptor.Name,
                formatter.Format(message));

        ctx.OnResponseReceived = (retry, message) =>
            logger.LogDebug("HAPI RX (attempt {Retry}): {Type} {Body}",
                retry,
                message.Descriptor.Name,
                formatter.Format(message));
    });
});
```

### Per-call hooks via `configure` callback

Attach hooks to a single call without affecting the client-wide configuration:

```csharp
var receipt = await client.TransferAsync(from, to, amount, ctx =>
{
    ctx.OnSendingRequest = msg => Console.WriteLine($"Sending: {msg.Descriptor.Name}");
    ctx.OnResponseReceived = (r, msg) => Console.WriteLine($"Received ({r}): {msg.Descriptor.Name}");
});
```

### What the hooks reveal

The `OnSendingRequest` callback receives the full `Transaction` or `Query` protobuf, which contains:
- Transaction body (operation type, parameters, fee limit, memo)
- Signature map (which keys signed)
- Transaction ID and validity window

The `OnResponseReceived` callback receives the network's response, including:
- Precheck status code
- For queries: the full result payload
- The retry attempt number (useful for diagnosing `BUSY` retries)

## gRPC channel-level logging

For lower-level HTTP/2 and connection diagnostics, enable .NET's built-in gRPC logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Grpc": "Debug",
      "System.Net.Http": "Debug"
    }
  }
}
```

## Logging transaction IDs

Every SDK call returns a receipt with a `TransactionId`. Log it to correlate application operations with Hedera network state:

```csharp
var receipt = await client.TransferAsync(from, to, amount);
logger.LogInformation("Transfer {Status} -- TransactionId: {TransactionId}",
    receipt.Status, receipt.TransactionId);
```

## Logging mirror node requests

`MirrorRestClient` wraps a standard `HttpClient`. Enable HTTP-level logging through `IHttpClientFactory`:

```csharp
builder.Services.AddHttpClient<MirrorRestClient>((sp, http) =>
{
    http.BaseAddress = new Uri(config["Hedera:MirrorRestUrl"]!);
})
.AddStandardResilienceHandler();  // optional: adds retry + logging

builder.Logging.AddFilter("System.Net.Http.HttpClient.MirrorRestClient", LogLevel.Debug);
```

## See also

- [`IConsensusContext.OnSendingRequest`](~/api/Hiero.IConsensusContext.yml) — API reference
- [Dependency injection](di.md) — registering clients with `IHttpClientFactory`
- [Error handling](errorhandling.md) — catching and logging exceptions
