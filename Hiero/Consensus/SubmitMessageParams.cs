// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;

/// <summary>
/// Submit Message Parameters, optionally including
/// message segment information. 
/// </summary>
/// <remarks>
/// The hedera network does not validate
/// the segment information submitted to 
/// a consensus topic.  This metadata must
/// be validated upon consumption and there
/// can be gaps and inconsistencies in the
/// resulting mirror HCS stream for the
/// related topic.
/// </remarks>
public sealed class SubmitMessageParams : TransactionParams<SubmitMessageReceipt>, INetworkParams<SubmitMessageReceipt>
{
    /// <summary>
    /// The address of the topic for the message.
    /// </summary>
    public EntityId Topic { get; set; } = default!;
    /// <summary>
    /// The value of this message or segment of the message,
    /// limited to the 4K total network transaction size.
    /// </summary>
    public ReadOnlyMemory<byte> Message { get; set; }
    /// <summary>
    /// If this is a segment of a larger message,
    /// the transaction that created the first segment
    /// of the message.  This acts as a correlation
    /// identifier to coalesce the segments of the
    /// message into one.
    /// </summary>
    /// <remarks>
    /// This must be left to be null when sending
    /// the first segment of a message.  The 
    /// value of the transaction ID returned from
    /// the receipt or record will contain the value
    /// associated with this parameter for the first
    /// segment.  This value must be included in 
    /// subsequent segments for this message.
    /// </remarks>
    public TransactionId? ParentTransactionId { get; set; } = null;
    /// <summary>
    /// The index of this segment (one based).
    /// Leave as zero (0) to submit an un-segmented 
    /// message.
    /// </summary>
    public int SegmentIndex { get; set; } = 0;
    /// <summary>
    /// The total number of segments making up
    /// the whole of the message when assembled.
    /// Set to 0 to indicate that this is not a
    /// a segmented message.
    /// </summary>
    public int TotalSegmentCount { get; set; } = 0;
    /// <summary>
    /// The signatory containing any additional 
    /// private keys or callbacks to meet the key 
    /// signing requirements for participants.
    /// </summary>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional cancellation token that can interrupt the
    /// submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }

    INetworkTransaction INetworkParams<SubmitMessageReceipt>.CreateNetworkTransaction()
    {
        if (Message.IsEmpty)
        {
            throw new ArgumentOutOfRangeException(nameof(Message), "Topic Message can not be empty.");
        }
        if (SegmentIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SegmentIndex), "Segment index must be between one and the total segment count inclusively.");
        }
        return new ConsensusSubmitMessageTransactionBody()
        {
            TopicID = new TopicID(Topic),
            Message = ByteString.CopyFrom(Message.Span),
            ChunkInfo = SegmentIndex > 0 ? createChunkInfo(ParentTransactionId, SegmentIndex, TotalSegmentCount) : null
        };

        static ConsensusMessageChunkInfo createChunkInfo(TransactionId? parentTx, int segmentIndex, int segmentTotalCount)
        {
            if (segmentTotalCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(TotalSegmentCount), "Total Segment Count must be a positive number.");
            }
            if (segmentIndex > segmentTotalCount)
            {
                throw new ArgumentOutOfRangeException(nameof(SegmentIndex), "Segment index must be between one and the total segment count inclusively.");
            }
            if (parentTx is null)
            {
                throw new ArgumentNullException(nameof(ParentTransactionId), "The parent transaction id is required when segment index is greater than one.");
            }
            return new ConsensusMessageChunkInfo
            {
                Total = segmentTotalCount,
                Number = segmentIndex,
                InitialTransactionID = new TransactionID(parentTx)
            };
        }
    }
    SubmitMessageReceipt INetworkParams<SubmitMessageReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new SubmitMessageReceipt(transactionId, receipt);
    }
    string INetworkParams<SubmitMessageReceipt>.OperationDescription => "Submit Message";
    internal SubmitMessageParams CloneWithTransactionId(TransactionId initialChunkTransactionId)
    {
        return new SubmitMessageParams
        {
            Topic = Topic,
            Message = Message,
            ParentTransactionId = initialChunkTransactionId,
            SegmentIndex = SegmentIndex,
            TotalSegmentCount = TotalSegmentCount,
            Signatory = Signatory,
            CancellationToken = CancellationToken
        };
    }
}
/// <summary>
/// Extension methods for submitting messages to consensus topics.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SubmitMessageExtensions
{
    /// <summary>
    /// Sends a message to the network for a given consensus topic.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client receiving the message transaction.
    /// </param>
    /// <param name="topic">
    /// The address of the topic for the message.
    /// </param>
    /// <param name="message">
    /// The value of the message, limited to the 4K total network transaction size.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A Submit Message Receipt indicating success, includes information
    /// about the sequence number of the message and its running hash.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<SubmitMessageReceipt> SubmitMessageAsync(this ConsensusClient client, EntityId topic, ReadOnlyMemory<byte> message, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new SubmitMessageParams { Topic = topic, Message = message }, configure);
    }
    /// <summary>
    /// Sends a message or a segment of a message to the network for a given consensus topic.
    /// The caller of this method is responsible for managing the segment of the
    /// message and associated metadata.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client receiving the message transaction.
    /// </param>
    /// <param name="submitParams">
    /// Details of the message segment to upload, including the metadata
    /// corresponding to this segment.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A Submit Message Receipt indicating success, includes information
    /// about the sequence number of the message and its running hash.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    public static async Task<SubmitMessageReceipt> SubmitMessageAsync(this ConsensusClient client, SubmitMessageParams submitParams, Action<IConsensusContext>? configure = null)
    {
        // We have a special case when the segment index
        // is one (the first chunk of a series of message chunks).
        // If we're not using chunks, it is easy, and for other 
        // chunks we will have the receipt from this call to set
        // the "parent" transction id.
        if (submitParams.SegmentIndex == 1)
        {
            // For the first chunk, it is not valid to set the
            // ParentTransactionId value, it can be set in the
            // client context if the caller whishes to micromange
            // the value of the transaction id.
            if (submitParams.ParentTransactionId is not null)
            {
                throw new ArgumentOutOfRangeException(nameof(SubmitMessageParams.ParentTransactionId), "The Parent Transaction cannot be specified (must be null) when the segment index is one.");
            }
            // Smelly Workaround due to necessity to embed the
            // same transaction ID in the middle of the message
            // as the envelope for the case of the first segment
            // of a segmented message.
            //
            // First We need to apply the configure command, to 
            // create the correct context, before we can generate
            // the transaction ID.            
            await using var configuredClient = client.Clone(configure);
            // Generate the TransactionId manually (or extract it because
            // it was set by the caller), since we need it as
            // a part of the chunk payload and can't let things
            // just work automatically.
            var initialChunkTransactionId = client.CreateNewTransactionId();
            // This is smelly too, but we don't want to alter the original
            // submit params since we don't control how it was created or
            // how it might be re-used, so we work with a clone with the
            // embedded iniital transaction id for the message chunk.
            submitParams = submitParams.CloneWithTransactionId(initialChunkTransactionId);
            // We use our configured client, however we need to override the
            // configuration with one additional configuration rule that will
            // peg the transaction to our pre-computed value.
            return await configuredClient.ExecuteAsync(submitParams, ctx => ctx.TransactionId = initialChunkTransactionId).ConfigureAwait(false);
        }
        else
        {
            return await client.ExecuteAsync(submitParams, configure).ConfigureAwait(false);
        }
    }
}