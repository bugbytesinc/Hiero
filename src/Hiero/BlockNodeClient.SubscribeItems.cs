// SPDX-License-Identifier: Apache-2.0
using Grpc.Core;
using Hiero.BlockNode;
using Hiero.Implementation;
using Org.Hiero.Block.Api;
using ProtoBlockStream = Com.Hedera.Hapi.Block.Stream;

namespace Hiero;

public sealed partial class BlockNodeClient
{
    /// <summary>
    /// Subscribes to the block node's block stream at the item level,
    /// placing each block item into the provided .NET Channel the
    /// moment its frame arrives — before the containing block is
    /// complete or verified.
    /// </summary>
    /// <remarks>
    /// This is the low-latency mode: an item can be delivered a second
    /// or more before its block's boundary marker, which matters for
    /// consumers reacting to events (e.g. EVM contract outputs) where
    /// milliseconds count.  In exchange, every safety of the assembled
    /// mode is traded away: no buffering, no duplicate dropping, no
    /// structural verification — a resent block is delivered again
    /// under the same number.  See <see cref="StreamedBlockItem"/> for
    /// the reconciliation contract.  Gaps in completed block numbers
    /// and transport drops are still recovered per
    /// <see cref="SubscribeBlockItemStreamParams.GapPolicy"/>,
    /// resuming from the last completed block + 1.
    /// </remarks>
    /// <param name="subscribeParameters">
    /// The details of the subscription: block range, delivery channel
    /// and gap policy.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify the
    /// execution configuration for just this method call.
    /// </param>
    /// <returns>
    /// Returns only after one of the following conditions occur: the
    /// output channel is completed by calling code; the cancellation
    /// token provided in the params is signaled; the server ended a
    /// bounded stream; or the stream faulted beyond recovery, in which
    /// case a <see cref="BlockNodeException"/> is thrown.
    /// </returns>
    /// <exception cref="ArgumentNullException">If required arguments are missing.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the requested block range is inverted.</exception>
    /// <exception cref="InvalidOperationException">If the block node URL configuration is missing.</exception>
    /// <exception cref="BlockNodeException">If the block stream faulted beyond the configured recovery policy.</exception>
    /// <example>
    /// Observe items mid-block, before verification — the low-latency
    /// mode for event monitors that reconcile on block completion:
    /// <code source="../../samples/BlockSubscription/Program.cs" region="SubscribeBlockItemStream" language="csharp"/>
    /// </example>
    public async Task SubscribeBlockItemStreamAsync(SubscribeBlockItemStreamParams subscribeParameters, Action<IBlockNodeContext>? configure = null)
    {
        if (subscribeParameters is null)
        {
            throw new ArgumentNullException(nameof(subscribeParameters), "Block Item Stream Subscribe Parameters argument is missing. Please check that it is not null.");
        }
        if (subscribeParameters.ItemWriter is null)
        {
            throw new ArgumentNullException(nameof(subscribeParameters.ItemWriter), "The destination channel writer is missing. Please check that it is not null.");
        }
        if (subscribeParameters.StartingBlockNumber.HasValue && subscribeParameters.EndingBlockNumber.HasValue && subscribeParameters.EndingBlockNumber.Value < subscribeParameters.StartingBlockNumber.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(subscribeParameters.EndingBlockNumber), "The ending block number is less than the starting block number, no items can be returned.");
        }
        await using var context = CreateChildContext(configure);
        if (context.Uri is null)
        {
            throw new InvalidOperationException("The Block Node URL has not been configured. Please check that 'Uri' is set in the Block Node context.");
        }
        var writer = subscribeParameters.ItemWriter;
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
        ulong? lastCompleted = null;
        ulong? resumeFrom = subscribeParameters.StartingBlockNumber;
        var attempt = 0;
        try
        {
            while (true)
            {
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
                    throw new BlockNodeException($"Cannot {(lastCompleted.HasValue ? "resume" : "start")} the block stream at block {startBlockNumber}; the node's history begins at block {status.FirstAvailableBlock} (earlier blocks have been pruned or were never held).", BlockNodeExceptionCode.NotAvailable, null);
                }
                var request = new SubscribeStreamRequest
                {
                    StartBlockNumber = startBlockNumber,
                    EndBlockNumber = subscribeParameters.EndingBlockNumber ?? ulong.MaxValue
                };
                onSendingRequest(request);
                var currentBlockNumber = 0UL;
                BlockStreamFault? fault = null;
                SubscribeStreamResponse.Types.Code? terminalCode = null;
                var streamEndedAbnormally = false;
                try
                {
                    using var call = streamClient.subscribeBlockStream(request, new CallOptions(cancellationToken: cancellationToken));
                    var stream = call.ResponseStream;
                    while (fault is null && await stream.MoveNext().ConfigureAwait(false))
                    {
                        var response = stream.Current;
                        switch (response.ResponseCase)
                        {
                            case SubscribeStreamResponse.ResponseOneofCase.BlockItems:
                                foreach (var protoItem in response.BlockItems.BlockItems)
                                {
                                    if (protoItem.ItemCase == ProtoBlockStream.BlockItem.ItemOneofCase.BlockHeader)
                                    {
                                        var headerNumber = protoItem.BlockHeader.Number;
                                        if (gapPolicy != BlockStreamGapPolicy.Deliver && lastCompleted.HasValue && headerNumber > lastCompleted.Value + 1)
                                        {
                                            fault = new BlockStreamFault(BlockStreamFaultKind.Gap, lastCompleted.Value + 1, headerNumber, $"The block stream skipped ahead: expected block {lastCompleted.Value + 1} but received the header of block {headerNumber}.");
                                            break;
                                        }
                                        currentBlockNumber = headerNumber;
                                    }
                                    var item = new StreamedBlockItem
                                    {
                                        BlockNumber = currentBlockNumber,
                                        Item = new BlockItem(protoItem),
                                        IsBlockComplete = protoItem.ItemCase == ProtoBlockStream.BlockItem.ItemOneofCase.BlockProof
                                    };
                                    if (!await TryWriteToChannelAsync(item).ConfigureAwait(false))
                                    {
                                        return;
                                    }
                                    attempt = 0;
                                }
                                break;
                            case SubscribeStreamResponse.ResponseOneofCase.EndOfBlock:
                                var completedNumber = response.EndOfBlock.BlockNumber;
                                if (!lastCompleted.HasValue || completedNumber > lastCompleted.Value)
                                {
                                    lastCompleted = completedNumber;
                                }
                                break;
                            case SubscribeStreamResponse.ResponseOneofCase.Status:
                                terminalCode = response.Status;
                                break;
                        }
                        if (terminalCode is not null)
                        {
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
                        resumeFrom = lastCompleted.HasValue ? lastCompleted.Value + 1 : resumeFrom;
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
                            throw new BlockNodeException("The requested blocks are outside the range available from this block node.", BlockNodeExceptionCode.NotAvailable, null);
                        case SubscribeStreamResponse.Types.Code.InvalidRequest:
                            throw new BlockNodeException("The block node rejected the subscribe request as invalid.", BlockNodeExceptionCode.InvalidRequest, null);
                        default:
                            if (canRecover && attempt < retryCount)
                            {
                                attempt++;
                                resumeFrom = lastCompleted.HasValue ? lastCompleted.Value + 1 : resumeFrom;
                                await Task.Delay(retryDelay * attempt, cancellationToken).ConfigureAwait(false);
                                continue;
                            }
                            throw new BlockNodeException($"The block node ended the stream reporting an error: {terminalCode.Value}", BlockNodeExceptionCode.ServerError, null);
                    }
                }
                if (fault is not null)
                {
                    if (gapPolicy == BlockStreamGapPolicy.Throw || attempt >= retryCount)
                    {
                        throw new BlockNodeException(fault.Message, BlockNodeExceptionCode.StreamGapDetected, null);
                    }
                    attempt++;
                    resumeFrom = lastCompleted.HasValue ? lastCompleted.Value + 1 : resumeFrom;
                    await Task.Delay(retryDelay * attempt, cancellationToken).ConfigureAwait(false);
                    continue;
                }
                if (streamEndedAbnormally)
                {
                    if (canRecover && attempt < retryCount)
                    {
                        attempt++;
                        resumeFrom = lastCompleted.HasValue ? lastCompleted.Value + 1 : resumeFrom;
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

        async ValueTask<bool> TryWriteToChannelAsync(StreamedBlockItem item)
        {
            if (await writer.WriteOnceAsync(item, cancellationToken).ConfigureAwait(false))
            {
                return true;
            }
            cancelTokenSource.Cancel();
            return false;
        }
    }
}
