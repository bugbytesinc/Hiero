// SPDX-License-Identifier: Apache-2.0
using Grpc.Core;
using Hiero.BlockNode;
using Hiero.Implementation;
using Org.Hiero.Block.Api;

namespace Hiero;

public sealed partial class BlockNodeClient
{
    /// <summary>
    /// Subscribes to the block node's block stream, assembling item
    /// frames into whole blocks and placing each completed block
    /// into the provided .NET Channel.
    /// </summary>
    /// <remarks>
    /// The block stream is forwarded by the node <em>unverified</em>;
    /// gaps, resends and malformed blocks are protocol-normal.  This
    /// method owns those sharp edges: partial blocks superseded by a
    /// resend are discarded, duplicates are dropped, each completed
    /// block is validated per
    /// <see cref="SubscribeBlockStreamParams.VerificationLevel"/>,
    /// and gaps or transport drops are handled per
    /// <see cref="SubscribeBlockStreamParams.GapPolicy"/> — by
    /// default resubscribing from the last delivered block + 1 using
    /// the context's retry settings.  Only completed blocks are ever
    /// written to the channel.
    /// </remarks>
    /// <param name="subscribeParameters">
    /// The details of the subscription: block range, delivery channel,
    /// verification level and gap policy.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify the
    /// execution configuration for just this method call.
    /// </param>
    /// <returns>
    /// Returns only after one of the following conditions occur: the
    /// output channel is completed by calling code; the cancellation
    /// token provided in the params is signaled; the maximum number of
    /// blocks was delivered; the ending block was delivered (or the
    /// server ended the bounded stream); or the stream faulted beyond
    /// recovery, in which case a <see cref="BlockNodeException"/> is
    /// thrown.
    /// </returns>
    /// <exception cref="ArgumentNullException">If required arguments are missing.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// If the requested block range is inverted, or if
    /// <see cref="SubscribeBlockStreamParams.ResumeFromCheckpoint"/> is
    /// combined with a starting block number, used with a verification
    /// level below <see cref="BlockVerificationLevel.HashChain"/>, or
    /// carries a root hash that is not 48 bytes.
    /// </exception>
    /// <exception cref="InvalidOperationException">If the block node URL configuration is missing.</exception>
    /// <exception cref="BlockNodeException">If the block stream faulted beyond the configured recovery policy.</exception>
    /// <example>
    /// Tail live assembled blocks — reads keep pace with the incoming
    /// stream via a <c>Channel</c>, with gaps and reconnects handled
    /// automatically:
    /// <code source="../../samples/BlockSubscription/Program.cs" region="SubscribeBlockStream" language="csharp"/>
    /// </example>
    public async Task SubscribeBlockStreamAsync(SubscribeBlockStreamParams subscribeParameters, Action<IBlockNodeContext>? configure = null)
    {
        if (subscribeParameters is null)
        {
            throw new ArgumentNullException(nameof(subscribeParameters), "Block Stream Subscribe Parameters argument is missing. Please check that it is not null.");
        }
        if (subscribeParameters.BlockWriter is null)
        {
            throw new ArgumentNullException(nameof(subscribeParameters.BlockWriter), "The destination channel writer is missing. Please check that it is not null.");
        }
        if (subscribeParameters.StartingBlockNumber.HasValue && subscribeParameters.EndingBlockNumber.HasValue && subscribeParameters.EndingBlockNumber.Value < subscribeParameters.StartingBlockNumber.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(subscribeParameters.EndingBlockNumber), "The ending block number is less than the starting block number, no blocks can be returned.");
        }
        if (subscribeParameters.ResumeFromCheckpoint is not null)
        {
            if (subscribeParameters.StartingBlockNumber.HasValue)
            {
                throw new ArgumentOutOfRangeException(nameof(subscribeParameters.ResumeFromCheckpoint), "Specify either StartingBlockNumber or ResumeFromCheckpoint, not both.");
            }
            if (subscribeParameters.VerificationLevel < BlockVerificationLevel.HashChain)
            {
                throw new ArgumentOutOfRangeException(nameof(subscribeParameters.ResumeFromCheckpoint), "ResumeFromCheckpoint requires VerificationLevel HashChain; cryptographic continuity cannot be checked without root recomputation.");
            }
            if (subscribeParameters.ResumeFromCheckpoint.RootHash.Length != 48)
            {
                throw new ArgumentOutOfRangeException(nameof(subscribeParameters.ResumeFromCheckpoint), "The checkpoint root hash must be 48 bytes (SHA-384).");
            }
        }
        await using var context = CreateChildContext(configure);
        if (context.Uri is null)
        {
            throw new InvalidOperationException("The Block Node URL has not been configured. Please check that 'Uri' is set in the Block Node context.");
        }
        var writer = subscribeParameters.BlockWriter;
        using var cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(subscribeParameters.CancellationToken);
        var cancellationToken = cancelTokenSource.Token;
        var channel = context.GetChannel();
        var statusClient = new BlockNodeService.BlockNodeServiceClient(context.GetStatusChannel());
        var statusEndpoint = context.StatusUri ?? context.Uri;
        var streamClient = new BlockStreamSubscribeService.BlockStreamSubscribeServiceClient(channel);
        var onSendingRequest = context.InstantiateOnSendingRequestHandler();
        var retryCount = Math.Max(0, context.RetryCount);
        var retryDelay = context.RetryDelay;
        var gapPolicy = subscribeParameters.GapPolicy;
        var canRecover = gapPolicy == BlockStreamGapPolicy.Resubscribe || gapPolicy == BlockStreamGapPolicy.Deliver;
        var deliveredCount = 0UL;
        ulong? lastDelivered = null;
        ulong? resumeFrom = subscribeParameters.StartingBlockNumber;
        byte[]? lastComputedRoot = null;
        if (subscribeParameters.ResumeFromCheckpoint is not null)
        {
            lastDelivered = subscribeParameters.ResumeFromCheckpoint.BlockNumber;
            lastComputedRoot = subscribeParameters.ResumeFromCheckpoint.RootHash.ToArray();
            resumeFrom = subscribeParameters.ResumeFromCheckpoint.BlockNumber + 1;
        }
        var attempt = 0;
        try
        {
            while (true)
            {
                // Preflight: learn the node's available range so a request
                // the server would reject (or a pruned-past resume point)
                // fails fast with a useful message.
                ServerStatusResponse status;
                ulong? latestAvailable = null;
                try
                {
                    status = await statusClient.serverStatusAsync(new ServerStatusRequest(), new CallOptions(cancellationToken: cancellationToken)).ResponseAsync.ConfigureAwait(false);
                    if (!resumeFrom.HasValue)
                    {
                        latestAvailable = await GetLatestAvailableBlockAsync(statusClient, status, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.Unknown)
                {
                    if (attempt < retryCount)
                    {
                        attempt++;
                        await Task.Delay(retryDelay * attempt, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    throw new BlockNodeException("The Block Node is not available at this time.", BlockNodeExceptionCode.Unavailable, ex);
                }
                catch (RpcException ex) when (ex.StatusCode is StatusCode.Unimplemented or StatusCode.NotFound)
                {
                    // Block nodes serve different subsets of the Block Node
                    // APIs, and may publish each on its own endpoint; an
                    // absent service reports itself at the transport level
                    // rather than through a response status.
                    throw new BlockNodeException($"The Block Node does not serve the status API at {statusEndpoint}, which subscribing requires. If the node publishes its status API on a separate endpoint (the HIP-1137 registry lists a STATUS endpoint), set 'StatusUri' in the Block Node context.", BlockNodeExceptionCode.NotImplemented, ex);
                }
                catch (RpcException ex)
                {
                    throw new BlockNodeException($"Unable to communicate with the Block Node: {ex.StatusCode}", BlockNodeExceptionCode.CommunicationError, ex);
                }
                var startBlockNumber = resumeFrom ?? latestAvailable ?? throw new BlockNodeException("The block node reports no fixed latest block (it is newly installed or backfilling its history) and no available block ranges; specify a starting block number.", BlockNodeExceptionCode.NotAvailable, null);
                if (startBlockNumber < status.FirstAvailableBlock)
                {
                    throw new BlockNodeException($"Cannot {(lastDelivered.HasValue ? "resume" : "start")} the block stream at block {startBlockNumber}; the node's history begins at block {status.FirstAvailableBlock} (earlier blocks have been pruned or were never held).", BlockNodeExceptionCode.NotAvailable, null);
                }
                var request = new SubscribeStreamRequest
                {
                    StartBlockNumber = startBlockNumber,
                    EndBlockNumber = subscribeParameters.EndingBlockNumber ?? ulong.MaxValue
                };
                onSendingRequest(request);
                var assembler = new BlockStreamAssembler(subscribeParameters.VerificationLevel, gapPolicy, lastDelivered, lastComputedRoot);
                BlockStreamFault? fault = null;
                SubscribeStreamResponse.Types.Code? terminalCode = null;
                var streamEndedAbnormally = false;
                try
                {
                    using var call = streamClient.subscribeBlockStream(request, new CallOptions(cancellationToken: cancellationToken));
                    var stream = call.ResponseStream;
                    while (await stream.MoveNext().ConfigureAwait(false))
                    {
                        var step = assembler.Process(stream.Current);
                        if (step.Delivered is not null)
                        {
                            if (!await TryWriteToChannelAsync(step.Delivered).ConfigureAwait(false))
                            {
                                // The consuming side closed the channel; a
                                // normal completion of the subscription.
                                return;
                            }
                            deliveredCount++;
                            lastDelivered = assembler.LastDeliveredBlockNumber;
                            lastComputedRoot = assembler.LastComputedRootHash;
                            attempt = 0;
                            if (subscribeParameters.MaxCount > 0 && deliveredCount >= subscribeParameters.MaxCount)
                            {
                                return;
                            }
                            if (subscribeParameters.EndingBlockNumber.HasValue && step.Delivered.Number >= subscribeParameters.EndingBlockNumber.Value)
                            {
                                return;
                            }
                        }
                        if (step.Fault is not null)
                        {
                            fault = step.Fault;
                            break;
                        }
                        if (step.TerminalCode is not null)
                        {
                            terminalCode = step.TerminalCode;
                            break;
                        }
                    }
                    streamEndedAbnormally = fault is null && terminalCode is null;
                }
                catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && cancellationToken.IsCancellationRequested)
                {
                    // Cancelled is an expected closing condition, not an error.
                    return;
                }
                catch (RpcException ex) when (ex.StatusCode is StatusCode.Unavailable or StatusCode.Unknown or StatusCode.Cancelled)
                {
                    // A Cancelled status this side did not ask for (sent by
                    // the server or a proxy) is a dropped stream like any other.
                    if (canRecover && attempt < retryCount)
                    {
                        attempt++;
                        resumeFrom = lastDelivered.HasValue ? lastDelivered.Value + 1 : resumeFrom;
                        await Task.Delay(retryDelay * attempt, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    throw new BlockNodeException("The Block Node is not available at this time.", BlockNodeExceptionCode.Unavailable, ex);
                }
                catch (RpcException ex) when (ex.StatusCode is StatusCode.Unimplemented or StatusCode.NotFound)
                {
                    // Block nodes serve different subsets of the Block Node
                    // APIs; an absent service reports itself at the transport
                    // level rather than through a response status.
                    throw new BlockNodeException("The Block Node does not implement this API; check which Block Node APIs the endpoint advertises.", BlockNodeExceptionCode.NotImplemented, ex);
                }
                catch (RpcException ex)
                {
                    throw new BlockNodeException($"Block stream terminated with error: {ex.StatusCode}", BlockNodeExceptionCode.CommunicationError, ex);
                }
                if (terminalCode is not null)
                {
                    switch (terminalCode.Value)
                    {
                        case SubscribeStreamResponse.Types.Code.Success:
                            return;
                        case SubscribeStreamResponse.Types.Code.InvalidStartBlockNumber:
                            throw new BlockNodeException($"The block node rejected starting block number {startBlockNumber}.", BlockNodeExceptionCode.InvalidStartBlockNumber, null);
                        case SubscribeStreamResponse.Types.Code.InvalidEndBlockNumber:
                            throw new BlockNodeException($"The block node rejected ending block number {subscribeParameters.EndingBlockNumber}.", BlockNodeExceptionCode.InvalidEndBlockNumber, null);
                        case SubscribeStreamResponse.Types.Code.NotAvailable:
                            throw new BlockNodeException($"The requested blocks are outside the range available from this block node.", BlockNodeExceptionCode.NotAvailable, null);
                        case SubscribeStreamResponse.Types.Code.InvalidRequest:
                            throw new BlockNodeException("The block node rejected the subscribe request as invalid.", BlockNodeExceptionCode.InvalidRequest, null);
                        default:
                            if (canRecover && attempt < retryCount)
                            {
                                attempt++;
                                resumeFrom = lastDelivered.HasValue ? lastDelivered.Value + 1 : resumeFrom;
                                await Task.Delay(retryDelay * attempt, cancellationToken).ConfigureAwait(false);
                                continue;
                            }
                            throw new BlockNodeException($"The block node ended the stream reporting an error: {terminalCode.Value}", BlockNodeExceptionCode.ServerError, null);
                    }
                }
                if (fault is not null)
                {
                    if (fault.Kind == BlockStreamFaultKind.HashMismatch)
                    {
                        // Never resubscribed regardless of policy: the same
                        // node returns the same bytes, and unverifiable data
                        // must never be silently skipped.
                        throw new BlockNodeException(fault.Message, BlockNodeExceptionCode.VerificationFailed, null);
                    }
                    if (fault.Kind == BlockStreamFaultKind.Unverifiable)
                    {
                        // Likewise never resubscribed or skipped: the block
                        // is the same on every attempt, and delivering past
                        // it would break the chain the caller asked for.
                        throw new BlockNodeException(fault.Message, BlockNodeExceptionCode.Unverifiable, null);
                    }
                    if (gapPolicy == BlockStreamGapPolicy.Throw || attempt >= retryCount)
                    {
                        throw fault.Kind == BlockStreamFaultKind.Gap
                            ? new BlockNodeException(fault.Message, BlockNodeExceptionCode.StreamGapDetected, null)
                            : new BlockNodeException(fault.Message, BlockNodeExceptionCode.ServerError, null);
                    }
                    attempt++;
                    resumeFrom = lastDelivered.HasValue ? lastDelivered.Value + 1 : resumeFrom;
                    await Task.Delay(retryDelay * attempt, cancellationToken).ConfigureAwait(false);
                    continue;
                }
                if (streamEndedAbnormally)
                {
                    // The server closed the stream without a terminal status
                    // code; treat like a transport drop.
                    if (canRecover && attempt < retryCount)
                    {
                        attempt++;
                        resumeFrom = lastDelivered.HasValue ? lastDelivered.Value + 1 : resumeFrom;
                        await Task.Delay(retryDelay * attempt, cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    throw new BlockNodeException("The block stream ended without a terminal status code.", BlockNodeExceptionCode.CommunicationError, null);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancellation is an expected closing condition, not an error.
        }
        finally
        {
            if (subscribeParameters.CompleteChannelWhenFinished)
            {
                writer.TryComplete();
            }
        }

        async ValueTask<bool> TryWriteToChannelAsync(Block block)
        {
            if (await writer.WriteOnceAsync(block, cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
            cancelTokenSource.Cancel();
            return false;
        }
    }
    /// <summary>
    /// The block a subscription with no explicit start begins at: the
    /// node's latest available block.  A node that is newly installed
    /// or backfilling its history reports <code>uint64_max</code> there
    /// instead; the protocol then directs subscribers to the server
    /// status detail, whose available ranges give the real latest
    /// block.  Returns <code>null</code> when the node reports none.
    /// </summary>
    private static async Task<ulong?> GetLatestAvailableBlockAsync(BlockNodeService.BlockNodeServiceClient statusClient, ServerStatusResponse status, CancellationToken cancellationToken)
    {
        if (status.LastAvailableBlock != ulong.MaxValue)
        {
            return status.LastAvailableBlock;
        }
        var detail = await statusClient.serverStatusDetailAsync(new ServerStatusRequest(), new CallOptions(cancellationToken: cancellationToken)).ResponseAsync.ConfigureAwait(false);
        ulong? latest = null;
        foreach (var range in detail.AvailableRanges)
        {
            if (range.RangeEnd != ulong.MaxValue && (latest is null || range.RangeEnd > latest.Value))
            {
                latest = range.RangeEnd;
            }
        }
        return latest;
    }
}
