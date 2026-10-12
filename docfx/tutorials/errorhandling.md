---
title: Error Handling
---

# Error Handling

For a query, an exception tells you what went wrong. For a transaction, the
first question is a different one: **did it execute?** Some endings cannot
answer it, and code that moves value has to treat those differently from a
failure.

## Did it go through?

> Hedera finalises a transaction seconds after a node accepts it, sometimes
> longer. A client that stops waiting before the transaction's validity ends
> cannot tell "failed" from "still deciding". Treating the second as the first
> sends value twice or refunds a payment that happened.

The rules, for any code that moves value:

1. **Use the whole window.** The defaults do. A transaction is valid for 180
   seconds (`TransactionDuration`, the network's maximum), and the client
   follows its receipt for 189 seconds: 43 retries (`RetryCount`) whose wait
   grows by 200 ms each time (`RetryDelay`, so the n-th retry waits n × 200
   ms). If you change these, keep
   `RetryDelay × RetryCount × (RetryCount + 1) / 2` at least
   `TransactionDuration`.
2. **Choose the transaction id before submitting, and store it.** Create it
   with `client.CreateNewTransactionId()` and pass it with
   `ctx.TransactionId` for that one call. It is the idempotency key, the
   mirror lookup and the expiry clock.
3. **Only a receipt with `Status == Success` is a known success.**
4. **`TransactionException`**: the transaction reached consensus and failed,
   so nothing moved. When the node has no verdict to give, the SDK raises a
   `ConsensusException` instead. From a record lookup such as
   `GetTransactionRecordAsync`, it means the lookup failed: its `Status` is
   the lookup's, not the transaction's.
5. **`PrecheckException`**: the node refused the transaction, but read the
   code.
   - Codes about the transaction itself are definite: `InvalidSignature`,
     `InsufficientPayerBalance`, `InvalidAccountId`,
     `TokenNotAssociatedToAccount` and the like. Nothing executed.
   - Codes about the node, the network or the connection prove nothing:
     `Unknown` (for example a refused connection), `RpcError`, `Busy`,
     `PlatformTransactionNotCreated`, `PlatformNotActive`,
     `DuplicateTransaction` and `TransactionExpired`. The SDK may have sent
     the same transaction earlier in the call. The outcome is unknown.
   - Code that moves value is safest treating every `PrecheckException` as
     unknown.
6. **`ConsensusException`** means the outcome is unknown. So does any other
   exception once the transaction may have been sent, such as a transport
   failure or a cancellation.
7. **Never resubmit and never report "failed" on an unknown outcome.** A
   resubmission is a new transaction with a new id, so if the first one
   executed, the value moves twice. Report "pending" and settle it on a
   mirror node, as below. Send again, with a new id, only once the mirror
   says the first did not execute.

### Classifying the ending

```csharp
// Choose the transaction id first and store it with the payment: it is
// the idempotency key, the mirror lookup and the expiry clock.
var txId = client.CreateNewTransactionId();
try
{
    var receipt = await client.TransferAsync(sender, receiver, amount, ctx => ctx.TransactionId = txId);
    // Executed. Only a receipt with Status == Success is a known success.
}
catch (TransactionException)
{
    // Reached consensus and failed: nothing moved. Its Status says why.
}
catch (PrecheckException ex) when (ex.Status is ResponseCode.InvalidSignature
    or ResponseCode.InsufficientPayerBalance
    or ResponseCode.InvalidAccountId
    or ResponseCode.TokenNotAssociatedToAccount)
{
    // Refused for something wrong with the transaction itself: nothing ran.
}
catch (Exception ex) when (ex is not ArgumentException)
{
    // Every other ending -- ConsensusException, any other precheck code,
    // a transport failure, cancellation -- means the outcome is UNKNOWN:
    // it may have executed. Never resend it and never report it as
    // failed. Report "pending" and settle txId with ResolveOutcomeAsync.
}
```

`ArgumentException` is left to propagate: the SDK throws it while validating
the request, before anything is sent.

### Settling an unknown outcome on a mirror node

```csharp
// Settles a transaction whose outcome is unknown, on a mirror node. Returns
// Success if it executed; the failure code if it reached consensus and
// failed; TransactionExpired if it never executed and now never can; or
// null while it may still execute, so ask again later. validDuration is the
// TransactionDuration it was sent with (180 seconds by default).
public static async Task<ResponseCode?> ResolveOutcomeAsync(
    MirrorRestClient mirror,
    TransactionId txId,
    TimeSpan validDuration,
    CancellationToken cancellationToken = default)
{
    // Read the mirror's clock FIRST (never this machine's): the lookup
    // that follows then sees at least everything the mirror held at
    // that time, so absence below is judged against it.
    var mirrorNow = await mirror.GetLatestConsensusTimestampAsync(cancellationToken);
    // The records of the transaction itself: not its child records
    // (Nonce > 0), not the execution of a schedule it created, and not the
    // inner transactions of a batch.
    var records = (await mirror.GetTransactionGroupAsync(txId, cancellationToken))
        .Where(t => t.Nonce == 0 && !t.IsScheduled && t.TransactionId == txId)
        .ToArray();
    // A resend can leave a DUPLICATE_TRANSACTION record beside the one that
    // executed, so a success anywhere wins.
    if (records.Any(t => t.Status == ResponseCode.Success))
    {
        return ResponseCode.Success;
    }
    var failure = records.FirstOrDefault(t => t.Status != ResponseCode.DuplicateTransaction);
    if (failure is not null)
    {
        return failure.Status;
    }
    // Nothing decisive recorded. The transaction can no longer execute
    // once that clock is past its valid start plus its duration; the
    // 60-second margin covers mirror replicas that lag one another.
    var validStart = new ConsensusTimeStamp(txId.ValidStartSeconds, txId.ValidStartNanos);
    if (mirrorNow - validStart > (decimal)(validDuration + TimeSpan.FromSeconds(60)).TotalSeconds)
    {
        return ResponseCode.TransactionExpired;
    }
    return null;
}
```

`GetTransactionGroupAsync` returns an empty array when the mirror has no
record of the id. For a recent transaction that only means the mirror has not
caught up, which is why absence counts as "did not execute" only once the
mirror's own clock is past the transaction's expiry.

Settle it on a mirror node rather than with `GetReceiptAsync`: nodes keep
receipts for only about three minutes after consensus. Past that, or for an
id the node never saw, `GetReceiptAsync` raises a `ConsensusException` with
status `ReceiptNotFound`, which says nothing about whether it executed.

## Exception reference

| Exception | What it proves |
|-----------|----------------|
| [`PrecheckException`](xref:Hiero.PrecheckException) | The gateway node refused the transaction. Definite only for codes about the transaction itself; otherwise the outcome is unknown. |
| [`TransactionException`](xref:Hiero.TransactionException) | The transaction reached consensus and failed; fees were charged, nothing else changed. |
| [`ConsensusException`](xref:Hiero.ConsensusException) | Nothing: the outcome is unknown, and the transaction may have executed. |
| [`ContractException`](xref:Hiero.ContractException) | A smart contract call or query failed (EVM revert, out of gas). Usually permanent. |
| [`MirrorGrpcException`](xref:Hiero.MirrorGrpcException) | A mirror gRPC stream ended unexpectedly. `Unavailable` and `CommunicationError` are transient; `TopicNotFound` is not. |
| [`MirrorException`](xref:Hiero.Mirror.MirrorException) | The mirror REST API returned an HTTP error. 429, 503 and 504 are transient; 400 and 404 are not. |

## Answering quickly without cutting the window

With the defaults, an unknown ending can take about three minutes to surface,
and a node that is busy or unreachable takes as long to fail. Don't shorten
the window to answer sooner. Bound the wait instead, and let the submission
finish:

```csharp
var submission = client.TransferAsync(sender, receiver, amount, ctx => ctx.TransactionId = txId);
if (await Task.WhenAny(submission, Task.Delay(TimeSpan.FromSeconds(10))) != submission)
{
    // Answer "pending" now. The submission keeps its full window: record
    // its outcome when it completes, or settle txId on a mirror node.
}
```

## Inspecting receipts instead of catching `TransactionException`

For workflows where you want to inspect the receipt yourself:

```csharp
var receipt = await client.TransferAsync(from, to, amount, ctx =>
{
    ctx.ThrowIfNotSuccess = false;
});

if (receipt.Status != ResponseCode.Success)
{
    // Unknown or ReceiptNotFound: no verdict, settle it on a mirror node.
    // Any other status: reached consensus and failed.
}
```

A `PrecheckException` is still thrown with `ThrowIfNotSuccess = false`. A
receipt still pending when the retries run out comes back with status
`Unknown` instead of raising a `ConsensusException`. When the node has no
receipt to give, the SDK returns a stand-in carrying the node's answer,
`ReceiptNotFound` (or `Unknown`, if the node stayed too busy to answer).

## Cancellation

A transaction's parameters take an optional `CancellationToken`:

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var receipt = await client.TransferAsync(new TransferParams
{
    CryptoTransfers = [new CryptoTransfer(sender, -amount), new CryptoTransfer(receiver, amount)],
    CancellationToken = cts.Token
});
```

Cancelling stops the client from waiting; it cannot recall a transaction
already sent. If the token fires after submission, the outcome is unknown:
settle it on a mirror node as above.

Queries take the token as an argument: on `ConsensusClient` before the
`configure` callback, and on `MirrorRestClient` last or, when the query takes
filters, just before them. `ResolveOutcomeAsync` above takes one too, so a
settling loop stops when its caller does. A paged mirror query also honors
`WithCancellation`. Cancelling a query only stops the wait.

## Diagnosing

`OnSendingRequest` and `OnResponseReceived` show every message the client
exchanges with the network, and are the quickest way to see what it is doing:

```csharp
await using var client = new ConsensusClient(ctx =>
{
    ctx.Endpoint = endpoint;
    ctx.Payer = payer;
    ctx.Signatory = signatory;
    ctx.OnSendingRequest = msg =>
        Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} SEND {msg.Descriptor.Name}");
    ctx.OnResponseReceived = (attempt, msg) =>
        Console.WriteLine($"{DateTime.UtcNow:HH:mm:ss.fff} RECV #{attempt} {msg}");
});
```

`OnSendingRequest` fires once per request, not once per retry. A transfer
whose receipt is polled ten times logs two sends (the transaction and the
receipt query) and a numbered response for every attempt, so retries show up
on the receive side only.

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

- [`ConsensusException` API reference](~/api/Hiero.ConsensusException.yml) — settling an unknown outcome
- [`PrecheckException` API reference](~/api/Hiero.PrecheckException.yml) — which codes are definite
- [`TransactionException` API reference](~/api/Hiero.TransactionException.yml)
- [`IConsensusContext.RetryCount`](~/api/Hiero.IConsensusContext.yml) — the retry window
- [`ResponseCode` enum](~/api/Hiero.ResponseCode.yml) — every Hedera status code
