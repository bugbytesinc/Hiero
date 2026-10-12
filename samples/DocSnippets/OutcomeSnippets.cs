// SPDX-License-Identifier: Apache-2.0
//
// Compile-backed doc snippets for telling whether a transaction executed.
//
// The regions are referenced by <example><code source region="..."/> in
// src/Hiero/ConsensusException.cs, and copied verbatim into
// docfx/tutorials/errorhandling.md, README.md and docfx/llms-full.txt. Keep
// those copies in step when a region changes.
//
// Never rename a #region without updating the matching <code source="..."
// region="..."/> reference. The scripts/verify-doc-snippets.sh check catches
// broken references before publish.

using Hiero;
using Hiero.Mirror;

namespace DocSnippets;

public static class OutcomeSnippets
{
    public static async Task SubmitAndClassify(
        ConsensusClient client,
        EntityId sender,
        EntityId receiver,
        long amount)
    {
        #region SubmitAndClassify
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
        #endregion
    }

    #region ResolveOutcome
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
    #endregion
}
