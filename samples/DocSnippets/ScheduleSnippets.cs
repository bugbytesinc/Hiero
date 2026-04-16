// SPDX-License-Identifier: Apache-2.0
//
// Compile-backed doc snippets for the Schedule domain. See CryptoSnippets.cs
// for the authoring convention. The full ScheduleParams-based flow lives in
// samples/ScheduleTransaction/Program.cs — this file covers the convenience
// overload and adding signatures to an existing schedule.

using Hiero;

namespace DocSnippets;

public static class ScheduleSnippets
{
    public static async Task ScheduleConvenience(
        ConsensusClient client,
        EntityId sender,
        EntityId recipient,
        long amount)
    {
        #region ScheduleConvenience
        // Convenience overload: schedule any transaction directly without
        // wrapping it in ScheduleParams. Use the full ScheduleParams overload
        // if you need an Administrator key, Payer override, Expiration, or
        // DelayExecution.
        var receipt = await client.ScheduleAsync(new TransferParams
        {
            CryptoTransfers = new[]
            {
                new CryptoTransfer(sender, -amount),
                new CryptoTransfer(recipient, amount)
            }
        });
        Console.WriteLine($"Schedule id: {receipt.Schedule}");
        #endregion
    }

    public static async Task SignSchedule(
        ConsensusClient client,
        EntityId scheduleId)
    {
        #region SignSchedule
        // Add a signature to a pending schedule. The schedule executes
        // automatically once every signing requirement of the inner
        // transaction is satisfied — no explicit "execute" call is needed.
        // The client context's Signatory provides the key being added.
        var receipt = await client.SignScheduleAsync(scheduleId);
        Console.WriteLine($"Sign status: {receipt.Status}");
        #endregion
    }

    public static async Task DeleteSchedule(
        ConsensusClient client, EntityId scheduleId)
    {
        #region DeleteSchedule
        // Cancel a pending schedule before it executes. Only schedules
        // created with an Administrator key can be deleted — and only by
        // that admin. Once the schedule's inner transaction has executed
        // or expired, deletion is not meaningful.
        var receipt = await client.DeleteScheduleAsync(scheduleId);
        Console.WriteLine($"Delete status: {receipt.Status}");
        #endregion
    }

    public static async Task SignScheduleWithCoSigner(
        ConsensusClient client,
        EntityId scheduleId,
        Signatory coSignerKey)
    {
        #region SignScheduleCoSigner
        // Add a co-signer's signature via params. Use this when the required
        // key is not already in the client's context — e.g., a multi-party
        // transfer where an independent signer submits their signature from
        // a service that does not hold the primary payer's key.
        var receipt = await client.SignScheduleAsync(new SignScheduleParams
        {
            Schedule = scheduleId,
            Signatory = coSignerKey
        });
        Console.WriteLine($"Sign status: {receipt.Status}");
        #endregion
    }
}
