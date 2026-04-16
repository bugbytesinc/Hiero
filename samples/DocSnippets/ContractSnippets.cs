// SPDX-License-Identifier: Apache-2.0
//
// Compile-backed doc snippets for the Smart Contract domain. See
// CryptoSnippets.cs for the authoring convention. Query-only flows live in
// samples/SmartContract/Program.cs (region "QueryContract"); this file
// covers contract creation and state-changing calls.

using Hiero;

namespace DocSnippets;

public static class ContractSnippets
{
    public static async Task CreateFromByteCode(
        ConsensusClient client,
        ReadOnlyMemory<byte> bytecode,
        Endorsement adminEndorsement)
    {
        #region CreateContractInline
        // Deploy a small contract from inline bytecode. For contracts too
        // large to fit in a single transaction, upload the bytecode to the
        // file service first and set `File` instead of `ByteCode`. An
        // Administrator key makes the contract upgradable; omit it to make
        // the contract immutable.
        var receipt = await client.CreateContractAsync(new CreateContractParams
        {
            ByteCode = bytecode,
            Administrator = adminEndorsement,
            Gas = 300_000,
            InitialBalance = 0,
            ConstructorArgs = Array.Empty<object>(),
            RenewPeriod = TimeSpan.FromDays(90),
            Memo = "Deployed via Hiero SDK"
        });
        Console.WriteLine($"Contract: {receipt.Contract}");
        #endregion
    }

    public static async Task CreateFromFile(
        ConsensusClient client,
        EntityId bytecodeFile,
        Endorsement adminEndorsement)
    {
        #region CreateContractFromFile
        // Deploy from bytecode previously uploaded to the file service. Use
        // this path for large contracts that exceed the transaction size
        // limit. Bytecode must be stored as hex-encoded text, not raw bytes.
        var receipt = await client.CreateContractAsync(new CreateContractParams
        {
            File = bytecodeFile,
            Administrator = adminEndorsement,
            Gas = 500_000,
            ConstructorArgs = new object[] { "initial" },
            RenewPeriod = TimeSpan.FromDays(90)
        });
        Console.WriteLine($"Contract: {receipt.Contract}");
        #endregion
    }

    public static async Task CallContract(
        ConsensusClient client,
        EntityId contract)
    {
        #region CallContract
        // Call a state-changing method. The receipt confirms consensus but
        // does not carry the return data — fetch the CallContractRecord via
        // the mirror node if you need the emitted logs/returns. Arguments in
        // MethodArgs are ABI-encoded automatically from CLR types (string,
        // long, byte[], etc.).
        var receipt = await client.CallContractAsync(new CallContractParams
        {
            Contract = contract,
            Gas = 100_000,
            MethodName = "setMessage",
            MethodArgs = new object[] { "hello world" }
        });
        Console.WriteLine($"Call status: {receipt.Status}");
        #endregion
    }

    public static async Task CallPayableContract(
        ConsensusClient client,
        EntityId contract)
    {
        #region CallContractPayable
        // Payable calls include a tinybar amount debited from the Payer and
        // credited to the contract's crypto account. The contract method must
        // be marked payable or the call reverts.
        var receipt = await client.CallContractAsync(new CallContractParams
        {
            Contract = contract,
            Gas = 100_000,
            PayableAmount = 50_000_000, // 0.5 HBAR
            MethodName = "deposit",
            MethodArgs = Array.Empty<object>()
        });
        Console.WriteLine($"Payable call status: {receipt.Status}");
        #endregion
    }

    public static async Task UpdateContract(
        ConsensusClient client, EntityId contract, Endorsement newAdmin)
    {
        #region UpdateContract
        // Update mutable contract properties — admin key, memo, renew period,
        // auto-association limit. Null properties are left unchanged.
        // Contracts created without an Administrator are immutable.
        var receipt = await client.UpdateContractAsync(new UpdateContractParams
        {
            Contract = contract,
            Administrator = newAdmin,
            Memo = "Rotated admin 2026-Q2"
        });
        Console.WriteLine($"Update status: {receipt.Status}");
        #endregion
    }

    public static async Task DeleteContract(
        ConsensusClient client, EntityId contract, EntityId fundsReceiver)
    {
        #region DeleteContract
        // Delete a contract and sweep its remaining HBAR balance to the
        // specified recipient. Requires the contract's Administrator key.
        var receipt = await client.DeleteContractAsync(new DeleteContractParams
        {
            Contract = contract,
            FundsReceiver = fundsReceiver
        });
        Console.WriteLine($"Delete status: {receipt.Status}");
        #endregion
    }

    public static async Task SubmitEvmTransaction(
        ConsensusClient client, ReadOnlyMemory<byte> signedRlpTransaction)
    {
        #region EvmTransaction
        // Submit a native EIP-1559 / EIP-2930 / legacy Ethereum transaction
        // through the HAPI Ethereum gateway. `Transaction` is the full RLP
        // blob (type 0, 1, or 2), already signed. AdditionalGasAllowance
        // lets the HAPI payer cover fees if the eth sender runs short.
        var receipt = await client.ExecuteEvmTransactionAsync(new EvmTransactionParams
        {
            Transaction = signedRlpTransaction,
            AdditionalGasAllowance = 100_000_000 // 1 HBAR backstop
        });
        Console.WriteLine($"EVM tx status: {receipt.Status}");
        #endregion
    }

    public static async Task UpdateHookStorage(
        ConsensusClient client, Hook hook,
        ReadOnlyMemory<byte> slotKey, ReadOnlyMemory<byte> slotValue)
    {
        #region UpdateHookStorage
        // Rewrite one or more storage slots on a hook. Use this to maintain
        // state (e.g., allow/deny lists) consumed by a contract's hook logic.
        // The hook's owner account must sign.
        var receipt = await client.UpdateHookStorageAsync(new UpdateHookStorageParams
        {
            Hook = hook,
            StorageUpdates = new[]
            {
                new HookStorageEntry(slotKey, slotValue)
            }
        });
        Console.WriteLine($"Hook update status: {receipt.Status}");
        #endregion
    }

    public static async Task QueryContractWithArgs(
        ConsensusClient client,
        EntityId contract)
    {
        #region QueryContractWithArgs
        // Local view call with ABI arguments. Decode typed outputs via
        // result.Result.As<T>() — the ABI is inferred from the return type.
        // For heavy querying use the mirror node's EstimateGas/CallEvm
        // helpers instead — they are free.
        var result = await client.QueryContractAsync(new QueryContractParams
        {
            Contract = contract,
            Gas = 50_000,
            MethodName = "balanceOf",
            MethodArgs = new object[] { new EntityId(0, 0, 98) }
        });
        long balance = result.Result.As<long>();
        Console.WriteLine($"Balance: {balance}");
        #endregion
    }
}
