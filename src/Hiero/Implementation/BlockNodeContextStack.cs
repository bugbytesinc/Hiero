// SPDX-License-Identifier: Apache-2.0
#pragma warning disable CS8766
using Google.Protobuf;
using Grpc.Net.Client;

namespace Hiero.Implementation;

/// <summary>
/// Internal Implementation of the <see cref="IBlockNodeContext"/> used for configuring
/// <see cref="BlockNodeClient"/> objects.  Maintains a stack of parent objects
/// and coordinates values returned for various contexts.  Not intended for
/// public use.
/// </summary>
internal sealed class BlockNodeContextStack : ContextStack<BlockNodeContextStack, Uri>, IBlockNodeContext
{
    private ContextValue<Uri?> _uri;
    private ContextValue<Uri?> _statusUri;
    private ContextValue<int> _retryCount;
    private ContextValue<TimeSpan> _retryDelay;
    private ContextValue<Action<IMessage>?> _onSendingRequest;

    // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    public Uri? Uri { get => _uri.HasValue ? _uri.Value : _parent?.Uri; set => _uri.Set(value); }
    public Uri? StatusUri { get => _statusUri.HasValue ? _statusUri.Value : _parent?.StatusUri; set => _statusUri.Set(value); }
    public int RetryCount { get => _retryCount.HasValue ? _retryCount.Value : _parent?.RetryCount ?? default; set => _retryCount.Set(value); }
    public TimeSpan RetryDelay { get => _retryDelay.HasValue ? _retryDelay.Value : _parent?.RetryDelay ?? default; set => _retryDelay.Set(value); }
    public Action<IMessage>? OnSendingRequest { get => _onSendingRequest.HasValue ? _onSendingRequest.Value : _parent?.OnSendingRequest; set => _onSendingRequest.Set(value); }

    public BlockNodeContextStack(BlockNodeContextStack parent) : base(parent) { }
    public BlockNodeContextStack(Func<Uri, GrpcChannel> channelFactory) : base(channelFactory) { }

    public override void Reset(string name)
    {
        switch (name)
        {
            case nameof(Uri): _uri.Reset(); break;
            case nameof(StatusUri): _statusUri.Reset(); break;
            case nameof(RetryCount): _retryCount.Reset(); break;
            case nameof(RetryDelay): _retryDelay.Reset(); break;
            case nameof(OnSendingRequest): _onSendingRequest.Reset(); break;
            default: throw new ArgumentOutOfRangeException($"'{name}' is not a valid property to reset.");
        }
    }
    protected override Uri GetChannelKey()
    {
        return Uri ?? throw new InvalidOperationException("The Block Node Url has not been configured.");
    }
    /// <summary>
    /// The channel for the status API: <see cref="StatusUri"/> when
    /// configured, otherwise the same channel as every other API.
    /// </summary>
    public GrpcChannel GetStatusChannel()
    {
        return GetChannel(StatusUri ?? GetChannelKey());
    }
    /// <summary>
    /// Combines the <see cref="OnSendingRequest"/> handlers configured on
    /// this context and all of its parents into a single action.  The
    /// handlers run child first, root last: the innermost (per-call)
    /// handler sees the request before the client-level handler does.
    /// </summary>
    public Action<IMessage> InstantiateOnSendingRequestHandler()
    {
        var count = 0;
        Action<IMessage>? single = null;
        for (var ctx = this; ctx is not null; ctx = ctx._parent)
        {
            if (ctx._onSendingRequest.HasValue)
            {
                var handler = ctx._onSendingRequest.Value;
                if (handler is not null)
                {
                    single = handler;
                    count++;
                }
            }
        }
        if (count == 0)
        {
            return NoOpSendingHandler;
        }
        if (count == 1)
        {
            return single!;
        }
        var handlers = new Action<IMessage>[count];
        var index = 0;
        for (var ctx = this; ctx is not null; ctx = ctx._parent)
        {
            if (ctx._onSendingRequest.HasValue)
            {
                var handler = ctx._onSendingRequest.Value;
                if (handler is not null)
                {
                    handlers[index++] = handler;
                }
            }
        }
        return request =>
        {
            for (var i = 0; i < handlers.Length; i++)
            {
                handlers[i](request);
            }
        };
    }
}
