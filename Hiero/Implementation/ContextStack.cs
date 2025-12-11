using Google.Protobuf;
using Grpc.Net.Client;
using System.Collections.Concurrent;

namespace Hiero.Implementation;

/// <summary>
/// Internal Base Implementation of the <see cref="IConsensusContext"/> and 
/// <see cref="IMirrorGrpcContext"/> used for configuring
/// <see cref="ConsensusClient"/> and <see cref="MirrorGrpcClient"/>objects.  Maintains 
/// a stack of parent objects and coordinates values returned for 
/// various contexts.  Not intended for public use.
/// </summary>
internal abstract class ContextStack<TContext, TChannelKey> : IAsyncDisposable where TContext : ContextStack<TContext, TChannelKey> where TChannelKey : notnull
{
    protected static readonly Action<IMessage> NoOpSendingHandler = static (_) => { };
    protected static readonly Action<int, IMessage> NoOpResponseHandler = static (_, _) => { };
    protected readonly TContext? _parent;
    private readonly ConcurrentDictionary<TChannelKey, GrpcChannel> _channels;
    private readonly Func<TChannelKey, GrpcChannel> _channelFactory;
    private int _refCount;

    protected ContextStack(Func<TChannelKey, GrpcChannel> channelFactory)
    {
        // Root Context, holds the channels and is
        // only accessible via other contexts
        // so the ref count starts at 0
        _parent = null;
        _refCount = 0;
        _channels = new ConcurrentDictionary<TChannelKey, GrpcChannel>();
        _channelFactory = channelFactory;
    }
    protected ContextStack(ContextStack<TContext, TChannelKey> parent)
    {
        // Not the root context, will be held
        // by a client or call context. Ref count
        // starts at 1 for convenience
        _parent = (TContext)parent;
        _refCount = 1;
        _channels = parent._channels;
        _channelFactory = parent._channelFactory;
        parent.addRef();
    }
    protected abstract TChannelKey GetChannelKey();
    public abstract void Reset(string name);
    public GrpcChannel GetChannel()
    {
        var key = GetChannelKey();
        if (_channels.TryGetValue(key, out GrpcChannel? channel))
        {
            return channel;
        }
        lock (_channels)
        {
            return _channels.GetOrAdd(key, _channelFactory);
        }
    }
    public ValueTask DisposeAsync()
    {
        // Note: there still may be internal stacked references to this
        // object, it does not actually release resources unless it is root.
        // This all comes down to maintaining a map of uris to open grpc
        // channels.  Opening a chanel is an expensive operation.  The
        // map is shared thru the whole entire tree of child contexts.
        return removeRef();
    }
    protected void addRef()
    {
        _parent?.addRef();
        Interlocked.Increment(ref _refCount);
    }
    private async ValueTask removeRef()
    {
        var count = Interlocked.Decrement(ref _refCount);
        if (_parent == null)
        {
            if (count == 0)
            {
                var tasks = new List<Task>(_channels.Count);
                foreach (var channel in _channels.Values)
                {
                    tasks.Add(channel.ShutdownAsync());
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);
                _channels.Clear();
            }
        }
        else
        {
            await _parent.removeRef().ConfigureAwait(false);
        }
    }
}