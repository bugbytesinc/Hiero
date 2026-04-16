---
title: Deploy and Call a Smart Contract
---

# Deploy and Call a Smart Contract

Hedera smart contracts are EVM-compatible. Deployment uploads bytecode and executes the constructor; after that you can call state-changing methods or query read-only view functions.

## Deploy from inline bytecode

For small contracts that fit in a single transaction:

```csharp
var receipt = await client.CreateContractAsync(new CreateContractParams
{
    ByteCode = contractBytecode,          // compiled Solidity output
    Gas = 300_000,
    Administrator = adminEndorsement,     // omit for immutable
    ConstructorArgs = Array.Empty<object>(),
    RenewPeriod = TimeSpan.FromDays(90),
    Memo = "My contract"
});
Console.WriteLine($"Contract: {receipt.Contract}");
```

For large contracts, upload the bytecode to the file service first, then reference it via `File`:

```csharp
var receipt = await client.CreateContractAsync(new CreateContractParams
{
    File = bytecodeFileId,                // EntityId of the uploaded file
    Gas = 500_000,
    ConstructorArgs = new object[] { "initial" },
    RenewPeriod = TimeSpan.FromDays(90)
});
```

## Call a state-changing method

```csharp
var receipt = await client.CallContractAsync(new CallContractParams
{
    Contract = contractId,
    Gas = 100_000,
    MethodName = "setMessage",
    MethodArgs = new object[] { "hello world" }
});
Console.WriteLine($"Status: {receipt.Status}");
```

The receipt confirms consensus but does not carry the method's return data. Fetch the detailed record from a mirror node if you need emitted logs or return values.

## Query a read-only method

View calls execute locally on the gateway node — no state change, no consensus, no receipt:

```csharp
ContractCallResult result = await client.QueryContractAsync(new QueryContractParams
{
    Contract = contractId,
    Gas = 50_000,
    MethodName = "getMessage"
});
string message = result.Result.As<string>();
```

`MethodArgs` are ABI-encoded automatically. Return values are decoded via `result.Result.As<T>()`.

## Native EVM transactions

Submit a pre-signed RLP-encoded Ethereum transaction (type 0, 1, or 2) through the HAPI Ethereum gateway:

```csharp
var receipt = await client.ExecuteEvmTransactionAsync(new EvmTransactionParams
{
    Transaction = signedRlpBytes,
    AdditionalGasAllowance = 100_000_000  // HAPI payer backstop
});
```

## See also

- [`CreateContractParams` API reference](~/api/Hiero.CreateContractParams.yml)
- [`CallContractParams` API reference](~/api/Hiero.CallContractParams.yml)
- [`QueryContractParams` API reference](~/api/Hiero.QueryContractParams.yml)
