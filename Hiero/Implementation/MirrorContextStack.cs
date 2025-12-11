#pragma warning disable CS8766
using Google.Protobuf;
using Grpc.Net.Client;

namespace Hiero.Implementation;

/// <summary>
/// Internal Implementation of the <see cref="IConsensusContext"/> used for configuring
/// <see cref="ConsensusClient"/> objects.  Maintains a stack of parent objects 
/// and coordinates values returned for various contexts.  Not intended for
/// public use.
/// </summary>
internal class MirrorContextStack : ContextStack<MirrorContextStack, Uri>, IMirrorGrpcContext
{
    private ContextValue<Uri?> _uri;
    private ContextValue<Action<IMessage>?> _onSendingRequest;

    // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    public Uri? Uri { get => _uri.HasValue ? _uri.Value : _parent?.Uri; set => _uri.Set(value); }
    public Action<IMessage>? OnSendingRequest { get => _onSendingRequest.HasValue ? _onSendingRequest.Value : _parent?.OnSendingRequest; set => _onSendingRequest.Set(value); }

    public MirrorContextStack(MirrorContextStack parent) : base(parent) { }
    public MirrorContextStack(Func<Uri, GrpcChannel> channelFactory) : base(channelFactory) { }

    public override void Reset(string name)
    {
        switch (name)
        {
            case nameof(Uri): _uri.Reset(); break;
            case nameof(OnSendingRequest): _onSendingRequest.Reset(); break;
            default: throw new ArgumentOutOfRangeException($"'{name}' is not a valid property to reset.");
        }
    }
    protected override Uri GetChannelKey()
    {
        return Uri ?? throw new InvalidOperationException("The Mirror Node Url has not been configured.");
    }

    public Action<IMessage> InstantiateOnSendingRequestHandler()
    {
        List<Action<IMessage>>? list = null;
        for (var ctx = this; ctx is not null; ctx = ctx._parent)
        {
            if (ctx._onSendingRequest.HasValue)
            {
                var handler = ctx._onSendingRequest.Value;
                if (handler is not null)
                {
                    (list ??= []).Add(handler);
                }
            }
        }
        if (list is null || list.Count == 0)
        {
            return NoOpSendingHandler;
        }
        if (list.Count == 1)
        {
            return list[0];
        }
        var handlers = list.ToArray();
        return request =>
        {
            for (var i = 0; i < handlers.Length; i++)
            {
                handlers[i](request);
            }
        };
    }
}