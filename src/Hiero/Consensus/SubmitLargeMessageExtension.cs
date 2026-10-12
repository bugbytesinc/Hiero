// SPDX-License-Identifier: Apache-2.0
using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Extends the client functionality to include the 
/// orchestration of sending a large segmented
/// consensus message.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SubmitLargeMessageExtension
{
    /// <summary>
    /// Sends an HCS message of arbitrary size
    /// to the network by breaking the message
    /// into segments, submitting each segment
    /// in sequence.  Manages the segment metadata
    /// internally, returning an array of receipts
    /// representing the transactions required to
    /// upload the entire message.
    /// </summary>
    /// <remarks>
    /// The upload is not all-or-nothing.  Each segment is its own
    /// transaction, so when one fails or ends with an unknown outcome
    /// (the exception's <c>TransactionId</c> names that segment), every
    /// segment before it has already been recorded on the topic, and its
    /// receipt is lost with the exception.  Settle the failing segment on
    /// a mirror node before deciding to send the message again, and expect
    /// readers of the topic to see the partial upload either way.
    /// </remarks>
    /// <param name="client">
    /// A Hiero ConsensusClient instance.
    /// </param>
    /// <param name="topic">
    /// The address of the topic for the message.
    /// </param>
    /// <param name="message">
    /// The value of the message, may exceed the
    /// network limit size.
    /// </param>
    /// <param name="segmentSize">
    /// The maximum size of each segment.  Must
    /// be greater than zero and under the
    /// current allowed size for
    /// transactions currently supported by the 
    /// network.  The method will break the 
    /// message into as many segments as necessary
    /// to fulfill uploading the entire message.
    /// </param>
    /// <param name="signatory">
    /// The signatory containing any additional private keys or callbacks
    /// to meet the key signing requirements for participants.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// A <see cref="IConsensusContext.TransactionId"/> set here is used
    /// for the first segment only, whose id identifies the whole message;
    /// every later segment gets an id of its own.
    /// </param>
    /// <returns>
    /// An array of Submit Message Receipts indicating success, one for each
    /// segment uploaded.  The transaction ID of the first receipt matches
    /// the correlation transaction ID for the series of message segments
    /// as a whole.
    /// </returns>
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>

    public static async Task<SubmitMessageReceipt[]> SubmitLargeMessageAsync(this ConsensusClient client, EntityId topic, ReadOnlyMemory<byte> message, int segmentSize, Signatory? signatory = null, Action<IConsensusContext>? configure = null)
    {
        await using var configuredClient = client.Clone(configure);
        var segmentCount = (message.Length + segmentSize - 1) / segmentSize;
        var receipts = new SubmitMessageReceipt[segmentCount];
        receipts[0] = await configuredClient.SubmitMessageAsync(new SubmitMessageParams
        {
            Topic = topic,
            Message = segmentCount > 1 ? message.Slice(0, segmentSize) : message,
            SegmentIndex = 1,
            TotalSegmentCount = segmentCount,
            Signatory = signatory
        }).ConfigureAwait(false);
        var parentTx = receipts[0].TransactionId;
        for (int i = 1; i < segmentCount - 1; i++)
        {
            receipts[i] = await configuredClient.SubmitMessageAsync(new SubmitMessageParams
            {
                Topic = topic,
                Message = message.Slice(segmentSize * i, segmentSize),
                ParentTransactionId = parentTx,
                SegmentIndex = i + 1,
                TotalSegmentCount = segmentCount,
                Signatory = signatory
            }, NewTransactionId).ConfigureAwait(false);
        }
        if (segmentCount > 1)
        {
            receipts[segmentCount - 1] = await configuredClient.SubmitMessageAsync(new SubmitMessageParams
            {
                Topic = topic,
                Message = message.Slice(segmentSize * (segmentCount - 1)),
                ParentTransactionId = parentTx,
                SegmentIndex = segmentCount,
                TotalSegmentCount = segmentCount,
                Signatory = signatory
            }, NewTransactionId).ConfigureAwait(false);
        }
        return receipts;

        // A transaction id the caller configured belongs to the first
        // segment, whose id identifies the whole message; reusing it would
        // make every later segment a duplicate.
        static void NewTransactionId(IConsensusContext ctx)
        {
            ctx.TransactionId = null;
        }
    }
}