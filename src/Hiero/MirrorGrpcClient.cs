// SPDX-License-Identifier: Apache-2.0
using Grpc.Core;
using Grpc.Net.Client;
using Hiero.Implementation;

namespace Hiero;
/// <summary>
/// Hedera Network Mirror Streaming Client
/// </summary>
/// <remarks>
/// This client facilitates access to the Hedera Mirror Network
/// HCS streaming service.  It is used to subscribe to a topic
/// and forward the messages returned by the mirror node in 
/// near real-time to a .NET channel for processing.
/// <para>
/// <b>Thread safety.</b> An instance is safe for concurrent use and is
/// intended to be shared — a singleton registered with dependency injection
/// is the expected pattern. Each operation builds its own child context, so
/// concurrent calls share no mutable state, and the underlying gRPC channels
/// are pooled in a thread-safe cache.
/// </para>
/// <para>
/// <see cref="Configure"/> is the exception. It mutates this instance's own
/// context in place, which every in-flight operation reads through, and is
/// meant for setup before the client is used or while it is otherwise idle.
/// To vary settings on a live client, pass the per-call <c>configure</c>
/// argument — it applies only to that one call — or take a separately
/// configured instance with <see cref="Clone"/>.
/// </para>
/// </remarks>
public sealed class MirrorGrpcClient : IAsyncDisposable
{
    /// <summary>
    /// The context (stack) keeps a memory of configuration and preferences 
    /// within a variety of call contexts.  It can be cloned and tweaked as 
    /// required.  Preferences can be set on cloned or immediate call 
    /// contexts without changing parent contexts.  If a property is not set 
    /// in the current context, the system falls back to the parent context 
    /// for the value, and to its parent until a value has been set.
    /// </summary>
    private readonly MirrorContextStack _context;
    /// <summary>
    /// Set once this client has released its context, so a second
    /// dispose cannot release the shared parent chain a second time.
    /// </summary>
    private int _disposed;
    /// <summary>
    /// Creates a new instance of a Hedera Mirror Network MirrorGrpcClient.
    /// </summary>
    /// <remarks>
    /// Creating a new instance of a <code>MirrorGrpcClient</code> initializes a new instance 
    /// of a client.  It will have a separate cache of GRPC channels to the network 
    /// and will maintain a separate configuration from other clients.  The constructor 
    /// takes an optional callback method that configures the details on how the 
    /// client should connect to the network configurable details.  See the 
    /// <see cref="IMirrorGrpcContext"/> documentation for configuration details.
    /// </remarks>
    /// <param name="configure">
    /// Optional configuration method that can set the location of the network node 
    /// accessing the network.
    /// </param>
    public MirrorGrpcClient(Action<IMirrorGrpcContext>? configure = null) : this(DefaultChannelFactory, configure)
    {
    }
    /// <summary>
    /// Creates a new instance of a Hedera Mirror Network MirrorGrpcClient with a 
    /// custom gRPC channel factory.
    /// </summary>
    /// <remarks>
    /// Creating a new instance of a <code>MirrorGrpcClient</code> initializes a new instance 
    /// of a client.  It will have a separate cache of GRPC channels to the network 
    /// and will maintain a separate configuration from other clients.  The constructor 
    /// takes an optional callback method that configures the details on how the 
    /// client should connect to the network configurable details.  See the 
    /// <see cref="IMirrorGrpcContext"/> documentation for configuration details.
    /// </remarks>
    /// <param name="channelFactory">
    /// A custom callback method returning a new channel given the target mirror 
    /// node URI.  Note, this method is only called once for each unique URI 
    /// requested by the mirror grpc client (which is a function of the current
    /// context's URI parameter)
    /// </param>
    /// <param name="configure">
    /// Optional configuration method that can set the location of the network node 
    /// accessing the network.
    /// </param>
    public MirrorGrpcClient(Func<Uri, GrpcChannel> channelFactory, Action<IMirrorGrpcContext>? configure = null)
    {
        // Create a Context with System Defaults 
        // that are unreachable and can't be "Reset".
        // At the moment, there are no defaults to set
        // but we still want a "root".
        _context = MirrorContextStack.Configure(new MirrorContextStack(new MirrorContextStack(channelFactory)), configure);
    }
    /// <summary>
    /// Internal implementation of mirror client creation.  Accounts for  newly created 
    /// clients and cloning of clients alike.
    /// </summary>
    /// <param name="parent">
    /// The parent <see cref="MirrorContextStack"/> if this creation is a result of a
    /// <see cref="MirrorGrpcClient.Clone(Action{IMirrorGrpcContext})"/> method call.
    /// </param>
    /// <param name="configure">
    /// The optional <see cref="IMirrorGrpcContext"/> callback method, passed in from public
    /// instantiation or a <see cref="MirrorGrpcClient.Clone(Action{IMirrorGrpcContext})"/> method call.
    /// </param>
    private MirrorGrpcClient(MirrorContextStack parent, Action<IMirrorGrpcContext>? configure)
    {
        _context = MirrorContextStack.Configure(new MirrorContextStack(parent), configure);
    }
    /// <summary>
    /// Updates the configuration of this instance of a mirror client through 
    /// implementation of the supplied <see cref="IMirrorGrpcContext"/> callback method.
    /// </summary>
    /// <param name="configure">
    /// The callback method receiving the <see cref="IMirrorGrpcContext"/> object providing 
    /// the configuration details of this client instance.  Values can be retrieved 
    /// and set within the context of the method invocation.
    /// </param>
    /// <remarks>
    /// Mutates this client's own context in place. Every operation already in
    /// flight reads its inherited settings through that context, so calling
    /// this concurrently with active work can let a call observe a partially
    /// applied configuration. Configure before use, or while the client is
    /// idle. For a live client, prefer the per-call <c>configure</c> argument
    /// (scoped to that call alone) or <see cref="Clone"/> for a separately
    /// configured instance.
    /// </remarks>
    public void Configure(Action<IMirrorGrpcContext> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure), "Configuration action cannot be null.");
        }
        configure(_context);
    }
    /// <summary>
    /// Creates a new instance of the mirror client having a shared base configuration with its 
    /// parent.  Changes to the parent’s configuration will reflect in this instances 
    /// configuration while changes in this instances configuration will not be reflected 
    /// in the parent configuration.
    /// </summary>
    /// <param name="configure">
    /// The callback method receiving the <see cref="IMirrorGrpcContext"/> object providing 
    /// the configuration details of this client instance.  Values can be retrieved 
    /// and set within the context of the method invocation.
    /// </param>
    /// <returns>
    /// A new instance of a client object.
    /// </returns>
    public MirrorGrpcClient Clone(Action<IMirrorGrpcContext>? configure = null)
    {
        return new MirrorGrpcClient(_context, configure);
    }
    /// <summary>
    /// .NET Asynchronous dispose method.
    /// </summary>
    /// <remarks>
    /// Closes any GRPC channels solely owned by this <code>MirrorGrpcClient</code> instance.
    /// </remarks>
    /// <returns>
    /// An Async Task.
    /// </returns>
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return ValueTask.CompletedTask;
        }
        return _context.DisposeAsync();
    }
    /// <summary>
    /// Subscribes to a Topics Stream from a mirror node, placing the
    /// topic messages returned meeting the query criteria into the
    /// provided .net Channel.
    /// </summary>
    /// <param name="subscribeParameters">
    /// The details of the query, including the id of the topic, time
    /// constraint filters and the .net channel receiving the messages
    /// as they are returned from the server.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify the
    /// execution configuration for just this method call.  It is executed
    /// prior to submitting the request to the mirror node.
    /// </param>
    /// <returns>
    /// Returns only after one of the four conditions occur: the output channel is 
    /// completed by calling code; the cancellation token provided in the params is
    /// signaled; the maximum number of topic messages was returned as configured in
    /// the params; or if the mirror stream faults during streaming, in which case a 
    /// <see cref="MirrorGrpcException"/> is thrown.
    /// </returns>
    /// <exception cref="ArgumentNullException">If required arguments are missing.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the ending time is before the starting time.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="MirrorGrpcException">If the mirror node stream faulted during request processing or upon submission.</exception>
    /// <example>
    /// Live stream from the current time forward — reads keep pace with the
    /// incoming mirror messages via a <c>Channel</c>:
    /// <code source="../../samples/TopicSubscription/Program.cs" region="SubscribeTopic" language="csharp"/>
    /// </example>
    /// <example>
    /// Replay a bounded historical time range instead of tailing:
    /// <code source="../../samples/DocSnippets/ConsensusSnippets.cs" region="SubscribeBoundedRange" language="csharp"/>
    /// </example>
    public async Task SubscribeTopicAsync(SubscribeTopicParams subscribeParameters, Action<IMirrorGrpcContext>? configure = null)
    {
        if (subscribeParameters is null)
        {
            throw new ArgumentNullException(nameof(subscribeParameters), "Topic Subscribe Parameters argument is missing. Please check that it is not null.");
        }
        if (subscribeParameters.Topic is null)
        {
            throw new ArgumentNullException(nameof(subscribeParameters.Topic), "Topic address is missing. Please check that it is not null.");
        }
        if (subscribeParameters.MessageWriter is null)
        {
            throw new ArgumentNullException(nameof(subscribeParameters.MessageWriter), "The destination channel writer missing. Please check that it is not null.");
        }
        if (subscribeParameters.Starting.HasValue && subscribeParameters.Ending.HasValue)
        {
            if (subscribeParameters.Ending.Value < subscribeParameters.Starting.Value)
            {
                throw new ArgumentOutOfRangeException(nameof(subscribeParameters.Ending), "The ending filter date is less than the starting filter date, no records can be returned.");
            }
        }
        await using var context = CreateChildContext(configure);
        if (context.Uri is null)
        {
            throw new InvalidOperationException("The Mirror Node URL has not been configured. Please check that 'Url' is set in the Mirror context.");
        }
        var query = new Com.Hedera.Mirror.Api.Proto.ConsensusTopicQuery()
        {
            TopicID = new Proto.TopicID(subscribeParameters.Topic),
            Limit = subscribeParameters.MaxCount
        };
        if (subscribeParameters.Starting.HasValue)
        {
            query.ConsensusStartTime = new Proto.Timestamp(subscribeParameters.Starting.Value);
        }
        if (subscribeParameters.Ending.HasValue)
        {
            query.ConsensusEndTime = new Proto.Timestamp(subscribeParameters.Ending.Value);
        }
        var service = new Com.Hedera.Mirror.Api.Proto.ConsensusService.ConsensusServiceClient(context.GetChannel());
        using var cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(subscribeParameters.CancellationToken);
        var options = new CallOptions(cancellationToken: cancelTokenSource.Token);
        context.InstantiateOnSendingRequestHandler()(query);
        using var response = service.subscribeTopic(query, options);
        var stream = response.ResponseStream;
        var writer = subscribeParameters.MessageWriter;
        try
        {
            await ProcessResultStreamAsync(subscribeParameters.Topic).ConfigureAwait(false);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled && cancelTokenSource.IsCancellationRequested)
        {
            // Cancelled is an expected closing condition, not an error
        }
        catch (OperationCanceledException) when (cancelTokenSource.IsCancellationRequested)
        {
            // Cancelled while waiting for room in a full channel.
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            throw new MirrorGrpcException($"The topic with the specified address does not exist.", MirrorGrpcExceptionCode.TopicNotFound, ex);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
        {
            throw new MirrorGrpcException($"The address exists, but is not a topic.", MirrorGrpcExceptionCode.InvalidTopicAddress, ex);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
        {
            throw new MirrorGrpcException($"The Mirror node is not available at this time.", MirrorGrpcExceptionCode.Unavailable, ex);
        }
        catch (RpcException ex)
        {
            throw new MirrorGrpcException($"Stream Terminated with Error: {ex.StatusCode}", MirrorGrpcExceptionCode.CommunicationError, ex);
        }
        finally
        {
            if (subscribeParameters.CompleteChannelWhenFinished)
            {
                writer.TryComplete();
            }
        }

        async Task ProcessResultStreamAsync(EntityId topic)
        {
            while (await stream.MoveNext().ConfigureAwait(false))
            {
                var data = stream.Current;
                var message = new TopicMessage
                {
                    Topic = topic,
                    Consensus = data.ConsensusTimestamp.ToConsensusTimeStamp(),
                    Message = data.Message.Memory,
                    RunningHash = data.RunningHash.Memory,
                    SequenceNumber = data.SequenceNumber,
                    SegmentInfo = data.ChunkInfo is not null ? new MessageSegmentInfo(data.ChunkInfo) : null
                };
                if (!await writer.WriteOnceAsync(message, cancelTokenSource.Token).ConfigureAwait(false))
                {
                    // The consuming side completed the channel, which
                    // ends the subscription normally.
                    cancelTokenSource.Cancel();
                    return;
                }
            }
        }
    }
    /// <summary>
    /// Creates a new child context based on the current context instance.
    /// Includes an optional configuration method that can be immediately
    /// applied to the new context.  This method is used internally to create
    /// contexts for cloned clients and network method calls having custom
    /// configuration callbacks.
    /// </summary>
    private MirrorContextStack CreateChildContext(Action<IMirrorGrpcContext>? configure)
    {
        return MirrorContextStack.Configure(new MirrorContextStack(_context), configure);
    }
    /// <summary>
    /// The default algorithm for creating channels for the
    /// gRPC mirror streaming client.  This implementation sets
    /// the keep alive timeout of 30s, ping delay 60s and
    /// keep alive policy of always.  Testing has shown this is
    /// the best method for keeping the HCS streaming service alive
    /// for monitoring an HCS stream.
    /// </summary>
    /// <param name="uri">
    /// The URI endpoint of the gRPC mirror node HCS stream.
    /// </param>
    /// <returns>
    /// A GrpcChannel pointing to the URI of the mirror node endpoint.
    /// </returns>
    private static GrpcChannel DefaultChannelFactory(Uri uri)
    {
        var options = new GrpcChannelOptions()
        {
            HttpHandler = new SocketsHttpHandler
            {
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always
            },
            // The channel must dispose the handler it was given: without
            // this, a disposed client leaves the handler's connection pool
            // holding a keep-alive-pinged HTTP/2 connection to the mirror
            // node until garbage collection, so applications that create
            // and dispose clients repeatedly accumulate live connections.
            DisposeHttpClient = true
        };
        return GrpcChannel.ForAddress(uri, options);
    }
}
