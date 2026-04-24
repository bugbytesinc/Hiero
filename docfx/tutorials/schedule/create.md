---
title: Schedule a Transaction
---

# Schedule a Transaction

Scheduled transactions allow you to wrap *any* operation for deferred execution. The network holds the transaction until all required signatures are collected, then executes it automatically. This is the foundation for multi-party workflows where signers are not online at the same time.

## Schedule a transfer

```csharp
var receipt = await client.ScheduleAsync(new ScheduleParams
{
    Transaction = new TransferParams
    {
        CryptoTransfers = new[]
        {
            new CryptoTransfer(sender, -amount),
            new CryptoTransfer(receiver, amount)
        }
    },
    Memo = "Scheduled transfer",
    Expiration = new ConsensusTimeStamp(DateTime.UtcNow.AddHours(1))
});
Console.WriteLine($"Schedule ID: {receipt.Schedule}");
```

## Convenience overload

If you don't need expiration, admin key, or payer overrides, schedule any `TransactionParams` directly:

```csharp
var receipt = await client.ScheduleAsync(someTransferParams);
```

## Collect additional signatures

Other parties add their signatures via `SignScheduleAsync`. Once every required key has signed, the scheduled transaction executes automatically:

```csharp
// Party B signs from their own session
await client.SignScheduleAsync(scheduleId);
```

If the required key is not in the client's context, pass it explicitly:

```csharp
await client.SignScheduleAsync(new SignScheduleParams
{
    Schedule = scheduleId,
    Signatory = new Signatory(partyBKey)
});
```

## Delayed execution

Set `DelayExecution = true` to prevent execution even after all signatures are collected — the transaction waits until `Expiration`:

```csharp
var receipt = await client.ScheduleAsync(new ScheduleParams
{
    Transaction = transferParams,
    DelayExecution = true,
    Expiration = new ConsensusTimeStamp(DateTime.UtcNow.AddDays(7))
});
```

## Key points

- Any `TransactionParams` subclass can be wrapped in a schedule — transfers, token creates, contract calls, etc.
- If the schedule expires before all signatures are collected, it is deleted from state.
- Schedules created with an `Administrator` key can be cancelled via [`DeleteScheduleAsync`](xref:Hiero.DeleteScheduleExtensions.DeleteScheduleAsync*).
- Query schedule status with [`GetScheduleInfoAsync`](xref:Hiero.ScheduleInfoExtensions.GetScheduleInfoAsync*).

## See also

- [`ScheduleParams` API reference](~/api/Hiero.ScheduleParams.yml)
- [`SignScheduleParams` API reference](~/api/Hiero.SignScheduleParams.yml)
