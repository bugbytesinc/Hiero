// SPDX-License-Identifier: Apache-2.0
//
// Compile-backed doc snippets for the Utilities domain (atomic batches,
// externally-constructed transactions, pseudo-random numbers). See
// CryptoSnippets.cs for the authoring convention.

using Hiero;

namespace DocSnippets;

public static class UtilitiesSnippets
{
    public static async Task ExecuteBatchedTransactions(
        ConsensusClient client,
        EntityId sender1, EntityId sender2, EntityId receiver)
    {
        #region BatchedExecute
        // Atomic batch: every inner transaction either all succeeds or all
        // fails. Pass any combination of TransactionParams subclasses. The
        // batch is signed by every inner transaction's signatory plus the
        // batch-level Signatory if set.
        var receipt = await client.ExecuteAsync(new BatchedTransactionParams
        {
            TransactionParams = new TransactionParams[]
            {
                new TransferParams
                {
                    CryptoTransfers = new[]
                    {
                        new CryptoTransfer(sender1, -100_000_000),
                        new CryptoTransfer(receiver, 100_000_000)
                    }
                },
                new TransferParams
                {
                    CryptoTransfers = new[]
                    {
                        new CryptoTransfer(sender2, -50_000_000),
                        new CryptoTransfer(receiver, 50_000_000)
                    }
                }
            }
        });
        Console.WriteLine($"Batch status: {receipt.Status}");
        #endregion
    }

    public static async Task SubmitBatchPrecheckOnly(
        ConsensusClient client, EntityId sender, EntityId receiver)
    {
        #region BatchedSubmit
        // Send the batch and return as soon as the gateway precheck responds,
        // without waiting for consensus. Useful for fire-and-forget workloads
        // that recover via mirror-node polling for the final receipt.
        var precheckCode = await client.SubmitAsync(new BatchedTransactionParams
        {
            TransactionParams = new TransactionParams[]
            {
                new TransferParams
                {
                    CryptoTransfers = new[]
                    {
                        new CryptoTransfer(sender, -1_000_000),
                        new CryptoTransfer(receiver, 1_000_000)
                    }
                }
            }
        });
        Console.WriteLine($"Precheck code: {precheckCode}");
        #endregion
    }

    public static async Task SendExternalRaw(
        ConsensusClient client,
        ReadOnlyMemory<byte> preBuiltSignedTransactionBytes)
    {
        #region SendExternal
        // Forward a transaction built entirely by external tooling (e.g. a
        // hardware wallet). Send returns the precheck ResponseCode without
        // waiting for consensus. Use when you've already externally signed
        // bytes and just need them relayed to a gossip node.
        var code = await client.SendExternalTransactionAsync(preBuiltSignedTransactionBytes);
        Console.WriteLine($"Precheck: {code}");
        #endregion
    }

    public static async Task SubmitExternalRaw(
        ConsensusClient client,
        ReadOnlyMemory<byte> preBuiltSignedTransactionBytes)
    {
        #region SubmitExternal
        // Same as SendExternalTransactionAsync but waits for consensus and
        // returns a full TransactionReceipt. Use when the caller needs the
        // typed receipt (not just the precheck code).
        var receipt = await client.SubmitExternalTransactionAsync(preBuiltSignedTransactionBytes);
        Console.WriteLine($"Status: {receipt.Status}");
        #endregion
    }

    public static async Task SubmitExternalWithExtraSignatory(
        ConsensusClient client,
        ReadOnlyMemory<byte> signedBytes,
        Signatory extraSignatory)
    {
        #region SubmitExternalWithParams
        // Use the params overload to layer additional signatures on top of
        // an externally-signed transaction without re-building the body.
        // Any Signatory in the client's context is also applied.
        var receipt = await client.SubmitExternalTransactionAsync(new ExternalTransactionParams
        {
            SignedTransactionBytes = signedBytes,
            Signatory = extraSignatory
        });
        Console.WriteLine($"Status: {receipt.Status}");
        #endregion
    }

    public static async Task GeneratePrng(ConsensusClient client)
    {
        #region GeneratePrng
        // Request 48 bytes of network-generated pseudo-random data. The
        // receipt itself only confirms consensus; fetch the transaction
        // record from a mirror node to read the value out
        // (BytesPseudoRandomNumberRecord when MaxValue is null, or
        // RangedPseudoRandomNumberRecord with an int when MaxValue is set).
        var receipt = await client.GeneratePseudoRandomNumberAsync(new PseudoRandomNumberParams());
        Console.WriteLine($"PRNG tx id: {receipt.TransactionId}");
        #endregion
    }

    public static async Task GenerateBoundedPrng(ConsensusClient client)
    {
        #region GenerateBoundedPrng
        // With MaxValue set, the network returns an int in [0, MaxValue).
        // Mirror-node record type will be RangedPseudoRandomNumberRecord.
        var receipt = await client.GeneratePseudoRandomNumberAsync(new PseudoRandomNumberParams
        {
            MaxValue = 100
        });
        Console.WriteLine($"PRNG tx id: {receipt.TransactionId}");
        #endregion
    }
}
