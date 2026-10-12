// SPDX-License-Identifier: Apache-2.0
using Grpc.Core;
using Hiero.Implementation;
using Org.Hiero.Block.Api;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace Hiero.BlockNode;

/// <summary>
/// An open block stream publish session with a block node — the
/// advanced, producer-side surface used by block-producing tooling,
/// simulators and repeaters.
/// </summary>
/// <remarks>
/// <para>
/// The publish protocol is a genuine state machine: the node may order
/// the publisher to skip a block, resend one, or declare the publisher
/// ahead of it, and acknowledgements arrive only after the node has
/// verified and persisted a block.  This session deliberately performs
/// <em>no</em> automatic skip/resend handling — the caller drives, by
/// consuming <see cref="Events"/> (which it MUST do) and reacting to
/// directives; the only automation provided is
/// <see cref="WaitForAcknowledgementAsync"/> resolving off cumulative
/// acknowledgements.  Publishers must retain each written block until
/// it is acknowledged, in case the node requests a resend.
/// </para>
/// <para>
/// When the node ends the stream with an end-of-stream response, a
/// final <see cref="PublishStreamEventKind.StreamEnded"/> event
/// carrying the reason is emitted, and a non-success reason faults
/// pending and subsequent writes with a
/// <see cref="BlockNodeException"/> carrying the mapped reason.  If
/// the stream instead drops (a transport failure, or a close without
/// an end-of-stream response), no
/// <see cref="PublishStreamEventKind.StreamEnded"/> event is emitted:
/// <see cref="Events"/> simply completes, and writes fault with a
/// <see cref="BlockNodeExceptionCode.CommunicationError"/> or
/// <see cref="BlockNodeExceptionCode.Unavailable"/> code.
/// End the session gracefully with <see cref="EndStreamAsync"/>;
/// disposing without it aborts the underlying call.
/// </para>
/// </remarks>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed class BlockStreamPublisher : IAsyncDisposable
{
    private readonly IPublishConnection _connection;
    private readonly BlockNodeContextStack _context;
    private readonly Channel<PublishStreamEvent> _events = Channel.CreateUnbounded<PublishStreamEvent>();
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly object _ackLock = new();
    private readonly List<(ulong BlockNumber, TaskCompletionSource Completion)> _ackWaiters = [];
    private readonly Task _pumpTask;
    private ulong? _lastAcknowledged;
    private BlockNodeException? _fault;
    // Written by the caller's thread, read by the response pump: both must
    // be volatile so the pump does not act on a stale value and report a
    // graceful shutdown as an abnormal stream drop.
    private volatile bool _ending;
    private volatile bool _disposed;
    private int _disposeStarted;
    /// <summary>
    /// Set, under <see cref="_ackLock"/>, once the response pump has
    /// finished: no acknowledgement can arrive after that.
    /// </summary>
    private bool _pumpEnded;

    internal BlockStreamPublisher(IPublishConnection connection, BlockNodeContextStack context)
    {
        _connection = connection;
        _context = context;
        _pumpTask = PumpResponsesAsync();
    }
    /// <summary>
    /// The highest cumulatively acknowledged (verified and persisted)
    /// block number, if any acknowledgement has arrived yet.
    /// </summary>
    public ulong? LastAcknowledgedBlockNumber
    {
        get { lock (_ackLock) { return _lastAcknowledged; } }
    }
    /// <summary>
    /// Node directives and acknowledgements in arrival order.  The
    /// caller MUST consume this stream: skip, resend and behind
    /// directives require action before or during subsequent writes.
    /// The channel completes when the stream ends.
    /// </summary>
    public ChannelReader<PublishStreamEvent> Events => _events.Reader;
    /// <summary>
    /// Sends a complete block: its items batched into a block item
    /// set, followed by the block boundary marker.  Returns when the
    /// frames are written — NOT when the block is acknowledged; use
    /// <see cref="WaitForAcknowledgementAsync"/> for that.
    /// </summary>
    /// <param name="block">
    /// The complete block to publish (header first, proof last).
    /// </param>
    /// <param name="cancellationToken">
    /// An optional token that can interrupt the write.
    /// </param>
    /// <exception cref="ArgumentNullException">If the block is null.</exception>
    /// <exception cref="BlockNodeException">If the stream has faulted or the write failed.</exception>
    public async Task WriteBlockAsync(Block block, CancellationToken cancellationToken = default)
    {
        if (block is null)
        {
            throw new ArgumentNullException(nameof(block), "Block is missing. Please check that it is not null.");
        }
        var itemSet = new BlockItemSet();
        itemSet.BlockItems.AddRange(block.ToProtobuf().Items);
        await WriteRequestsAsync(cancellationToken,
            new PublishStreamRequest { BlockItems = itemSet },
            new PublishStreamRequest { EndOfBlock = new BlockEnd { BlockNumber = block.Number } }).ConfigureAwait(false);
    }
    /// <summary>
    /// Streams items for the block currently being produced, before
    /// its proof exists — the shape a live block producer needs.  The
    /// first item of a new block must be its header; close the block
    /// with <see cref="CompleteBlockAsync"/> after writing its proof
    /// item, or supersede it by writing the next block's header.
    /// </summary>
    /// <param name="items">
    /// The items to send, in stream order, as one block item set;
    /// a set must never span two blocks.
    /// </param>
    /// <param name="cancellationToken">
    /// An optional token that can interrupt the write.
    /// </param>
    /// <exception cref="ArgumentNullException">If the item list is null or empty.</exception>
    /// <exception cref="BlockNodeException">If the stream has faulted or the write failed.</exception>
    public async Task WriteBlockItemsAsync(IReadOnlyList<BlockItem> items, CancellationToken cancellationToken = default)
    {
        if (items is null || items.Count == 0)
        {
            throw new ArgumentNullException(nameof(items), "The list of block items is missing or empty.");
        }
        var itemSet = new BlockItemSet();
        foreach (var item in items)
        {
            itemSet.BlockItems.Add(item.ToProtobuf());
        }
        await WriteRequestsAsync(cancellationToken, new PublishStreamRequest { BlockItems = itemSet }).ConfigureAwait(false);
    }
    /// <summary>
    /// Sends the block boundary marker for the in-progress block
    /// started via <see cref="WriteBlockItemsAsync"/>.
    /// </summary>
    /// <param name="blockNumber">
    /// The number of the block being completed.
    /// </param>
    /// <param name="cancellationToken">
    /// An optional token that can interrupt the write.
    /// </param>
    /// <exception cref="BlockNodeException">If the stream has faulted or the write failed.</exception>
    public async Task CompleteBlockAsync(ulong blockNumber, CancellationToken cancellationToken = default)
    {
        await WriteRequestsAsync(cancellationToken, new PublishStreamRequest { EndOfBlock = new BlockEnd { BlockNumber = blockNumber } }).ConfigureAwait(false);
    }
    /// <summary>
    /// Returns a task that completes when the cumulative
    /// acknowledgement covers the given block number (immediately if
    /// it already does), or faults if the stream ends first.
    /// </summary>
    /// <param name="blockNumber">
    /// The block number to await acknowledgement of.
    /// </param>
    /// <param name="cancellationToken">
    /// An optional token that can abandon the wait (the stream is
    /// unaffected).
    /// </param>
    /// <exception cref="BlockNodeException">If the stream ended before the acknowledgement arrived.</exception>
    public async Task WaitForAcknowledgementAsync(ulong blockNumber, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource completion;
        lock (_ackLock)
        {
            if (_lastAcknowledged.HasValue && _lastAcknowledged.Value >= blockNumber)
            {
                return;
            }
            if (_fault is not null)
            {
                throw _fault;
            }
            if (_pumpEnded)
            {
                throw new BlockNodeException("The publish stream ended before the block was acknowledged.", BlockNodeExceptionCode.CommunicationError, null);
            }
            completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _ackWaiters.Add((blockNumber, completion));
        }
        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        try
        {
            await completion.Task.ConfigureAwait(false);
        }
        catch
        {
            // Abandoned waits must not accumulate: a caller that gives up
            // on (or cancels) many waits would otherwise grow the waiter
            // list for the life of the session.
            lock (_ackLock)
            {
                _ackWaiters.RemoveAll(waiter => ReferenceEquals(waiter.Completion, completion));
            }
            throw;
        }
    }
    /// <summary>
    /// Gracefully ends the publish stream, telling the node why and
    /// what range of blocks this publisher holds, then closes the
    /// sending side and waits for the node to close its side.
    /// </summary>
    /// <param name="code">
    /// The reason for ending the stream.
    /// </param>
    /// <param name="earliestBlockNumber">
    /// The earliest block this publisher holds.
    /// </param>
    /// <param name="latestBlockNumber">
    /// The latest block acknowledged by the node (or held, if none
    /// were acknowledged).
    /// </param>
    /// <param name="cancellationToken">
    /// An optional token that can interrupt the write and the wait for
    /// the node to close its side; without a cancellable token, that
    /// wait lasts until the node closes.
    /// </param>
    /// <exception cref="BlockNodeException">If the stream has faulted or the write failed.</exception>
    public async Task EndStreamAsync(PublishStreamEndCode code, ulong earliestBlockNumber, ulong latestBlockNumber, CancellationToken cancellationToken = default)
    {
        var request = new PublishStreamRequest
        {
            EndStream = new PublishStreamRequest.Types.EndStream
            {
                EndCode = (PublishStreamRequest.Types.EndStream.Types.Code)code,
                EarliestBlockNumber = earliestBlockNumber,
                LatestBlockNumber = latestBlockNumber
            }
        };
        ThrowIfFaulted();
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfFaulted();
            if (_ending)
            {
                throw new BlockNodeException("The publish stream has already been ended.", BlockNodeExceptionCode.InvalidRequest, null);
            }
            // Marked before completing the send side so a concurrent node
            // close is recognized as a graceful end, not an abnormal drop.
            _ending = true;
            await _connection.WriteAsync(request, cancellationToken).ConfigureAwait(false);
            await _connection.CompleteRequestsAsync().ConfigureAwait(false);
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
            await Task.WhenAny(_pumpTask, Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None)).ConfigureAwait(false);
            ThrowIfFaulted();
            throw new BlockNodeException($"Unable to end the publish stream: {ex.StatusCode}", BlockNodeExceptionCode.CommunicationError, ex);
        }
        finally
        {
            _writeLock.Release();
        }
        // Wait for the node to close its side so late directives and
        // acknowledgements are not lost.  Only a cancellable token from the
        // caller bounds this wait; with the default token it lasts until
        // the node closes.
        await _pumpTask.WaitAsync(cancellationToken).ConfigureAwait(false);
    }
    /// <summary>
    /// .NET Asynchronous dispose method.
    /// </summary>
    /// <remarks>
    /// Aborts the underlying call if the session was not gracefully
    /// ended via <see cref="EndStreamAsync"/>.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
        {
            return;
        }
        _disposed = true;
        await _connection.DisposeAsync().ConfigureAwait(false);
        try
        {
            await _pumpTask.ConfigureAwait(false);
        }
        catch
        {
            // The pump's failure modes were already surfaced via
            // Events and the fault state; disposal must not throw.
        }
        // The write lock is deliberately not disposed: a writer still
        // finishing would then fail releasing it, hiding its real error,
        // and a SemaphoreSlim whose wait handle was never requested holds
        // nothing that needs releasing.
        await _context.DisposeAsync().ConfigureAwait(false);
    }
    /// <summary>
    /// Sends one or more requests to the node under the write lock,
    /// translating failures into <see cref="BlockNodeException"/>.
    /// </summary>
    private async Task WriteRequestsAsync(CancellationToken cancellationToken, params PublishStreamRequest[] requests)
    {
        ThrowIfFaulted();
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfFaulted();
            if (_ending)
            {
                throw new BlockNodeException("The publish stream has already been ended.", BlockNodeExceptionCode.InvalidRequest, null);
            }
            foreach (var request in requests)
            {
                await _connection.WriteAsync(request, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (RpcException ex)
        {
            // The node may have terminated the stream concurrently;
            // prefer surfacing the terminal reason gathered by the pump.
            await Task.WhenAny(_pumpTask, Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None)).ConfigureAwait(false);
            ThrowIfFaulted();
            throw new BlockNodeException($"Unable to publish to the Block Node: {ex.StatusCode}", BlockNodeExceptionCode.CommunicationError, ex);
        }
        finally
        {
            _writeLock.Release();
        }
    }
    private void ThrowIfFaulted()
    {
        var fault = _fault;
        if (fault is not null)
        {
            throw fault;
        }
    }
    /// <summary>
    /// The background response pump: translates node responses into
    /// events, resolves acknowledgement waiters, and records the
    /// terminal fault when the node ends the stream abnormally.
    /// </summary>
    private async Task PumpResponsesAsync()
    {
        var endReasonSeen = false;
        try
        {
            while (true)
            {
                var response = await _connection.ReadAsync().ConfigureAwait(false);
                if (response is null)
                {
                    break;
                }
                switch (response.ResponseCase)
                {
                    case PublishStreamResponse.ResponseOneofCase.Acknowledgement:
                        var acknowledged = response.Acknowledgement.BlockNumber;
                        ResolveAcknowledgements(acknowledged);
                        _events.Writer.TryWrite(new PublishStreamEvent { Kind = PublishStreamEventKind.BlockAcknowledged, BlockNumber = acknowledged });
                        break;
                    case PublishStreamResponse.ResponseOneofCase.SkipBlock:
                        _events.Writer.TryWrite(new PublishStreamEvent { Kind = PublishStreamEventKind.SkipBlock, BlockNumber = response.SkipBlock.BlockNumber });
                        break;
                    case PublishStreamResponse.ResponseOneofCase.ResendBlock:
                        _events.Writer.TryWrite(new PublishStreamEvent { Kind = PublishStreamEventKind.ResendBlock, BlockNumber = response.ResendBlock.BlockNumber });
                        break;
                    case PublishStreamResponse.ResponseOneofCase.NodeBehindPublisher:
                        _events.Writer.TryWrite(new PublishStreamEvent { Kind = PublishStreamEventKind.BehindPublisher, BlockNumber = response.NodeBehindPublisher.BlockNumber });
                        break;
                    case PublishStreamResponse.ResponseOneofCase.EndStream:
                        endReasonSeen = true;
                        var status = response.EndStream.Status;
                        var reason = Enum.IsDefined(typeof(PublishStreamEndReason), (int)status) ? (PublishStreamEndReason)status : PublishStreamEndReason.Unknown;
                        if (reason != PublishStreamEndReason.Success)
                        {
                            RecordFault(new BlockNodeException($"The block node ended the publish stream: {reason}", MapEndReason(reason), null));
                        }
                        _events.Writer.TryWrite(new PublishStreamEvent { Kind = PublishStreamEventKind.StreamEnded, BlockNumber = response.EndStream.BlockNumber, EndReason = reason });
                        break;
                }
            }
            if (!endReasonSeen && !_ending && !_disposed)
            {
                RecordFault(new BlockNodeException("The publish stream ended without a terminal status.", BlockNodeExceptionCode.CommunicationError, null));
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && (_disposed || _ending))
        {
            // Expected when the session is being torn down.
        }
        catch (RpcException ex)
        {
            RecordFault(new BlockNodeException($"The publish stream terminated with error: {ex.StatusCode}", ex.StatusCode == StatusCode.Unavailable ? BlockNodeExceptionCode.Unavailable : BlockNodeExceptionCode.CommunicationError, ex));
        }
        catch (OperationCanceledException) when (_disposed || _ending)
        {
            // Expected when the session is being torn down.
        }
        finally
        {
            FaultRemainingWaiters();
            _events.Writer.TryComplete();
        }
    }
    private void ResolveAcknowledgements(ulong acknowledged)
    {
        List<TaskCompletionSource>? resolved = null;
        lock (_ackLock)
        {
            if (!_lastAcknowledged.HasValue || acknowledged > _lastAcknowledged.Value)
            {
                _lastAcknowledged = acknowledged;
            }
            for (var i = _ackWaiters.Count - 1; i >= 0; i--)
            {
                if (_ackWaiters[i].BlockNumber <= acknowledged)
                {
                    (resolved ??= []).Add(_ackWaiters[i].Completion);
                    _ackWaiters.RemoveAt(i);
                }
            }
        }
        resolved?.ForEach(completion => completion.TrySetResult());
    }
    private void RecordFault(BlockNodeException fault)
    {
        lock (_ackLock)
        {
            _fault ??= fault;
        }
    }
    private void FaultRemainingWaiters()
    {
        List<(ulong BlockNumber, TaskCompletionSource Completion)> pending;
        BlockNodeException fault;
        lock (_ackLock)
        {
            pending = [.. _ackWaiters];
            _ackWaiters.Clear();
            _pumpEnded = true;
            fault = _fault ?? new BlockNodeException("The publish stream ended before the block was acknowledged.", BlockNodeExceptionCode.CommunicationError, null);
        }
        foreach (var (_, completion) in pending)
        {
            completion.TrySetException(fault);
        }
    }
    private static BlockNodeExceptionCode MapEndReason(PublishStreamEndReason reason)
    {
        return reason switch
        {
            PublishStreamEndReason.InvalidRequest => BlockNodeExceptionCode.InvalidRequest,
            PublishStreamEndReason.Timeout => BlockNodeExceptionCode.Timeout,
            PublishStreamEndReason.DuplicateBlock => BlockNodeExceptionCode.DuplicateBlock,
            PublishStreamEndReason.BadBlockProof => BlockNodeExceptionCode.BadBlockProof,
            PublishStreamEndReason.PersistenceFailed => BlockNodeExceptionCode.PersistenceFailed,
            _ => BlockNodeExceptionCode.ServerError
        };
    }
}
