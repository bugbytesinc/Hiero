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
    /// Creates a new instance of an Hedera Mirror Network ConsensusClient.
    /// </summary>
    /// <remarks>
    /// Creating a new instance of a <code>Mirror</code> initializes a new instance 
    /// of a client.  It will have a separate cache of GRPC channels to the network 
    /// and will maintain a separate configuration from other clients.  The constructor 
    /// takes an optional callback method that configures the details on how the 
    /// client should connect to the network configuraable details.  See the 
    /// <see cref="IMirrorGrpcContext"/> documentation for configuration details.
    /// </remarks>
    /// <param name="configure">
    /// Optional configuration method that can set the location of the network node 
    /// accessing the network and how transaction fees shall be paid for.
    /// </param>
    public MirrorGrpcClient(Action<IMirrorGrpcContext>? configure = null) : this(DefaultChannelFactory, configure)
    {
    }
    /// <summary>
    /// Creates a new instance of an Hedera Mirror Network ConsensusClient with a 
    /// custom gRPC channel factory.
    /// </summary>
    /// <remarks>
    /// Creating a new instance of a <code>Mirror</code> initializes a new instance 
    /// of a client.  It will have a separate cache of GRPC channels to the network 
    /// and will maintain a separate configuration from other clients.  The constructor 
    /// takes an optional callback method that configures the details on how the 
    /// client should connect to the network configuraable details.  See the 
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
    /// accessing the network and how transaction fees shall be paid for.
    /// </param>
    public MirrorGrpcClient(Func<Uri, GrpcChannel> channelFactory, Action<IMirrorGrpcContext>? configure = null)
    {
        // Create a Context with System Defaults 
        // that are unreachable and can't be "Reset".
        // At the moment, there are no defaults to set
        // but we still want a "root".
        _context = new MirrorContextStack(new MirrorContextStack(channelFactory));
        configure?.Invoke(_context);
    }
    /// <summary>
    /// Internal implementation of mirror client creation.  Accounts for  newly created 
    /// clients and cloning of clients alike.
    /// </summary>
    /// <param name="channelFactory">
    /// The channel factory method to use when a new gRPC client channel is needed.
    /// </param>
    /// <param name="configure">
    /// The optional <see cref="IConsensusContext"/> callback method, passed in from public 
    /// instantiation or a <see cref="MirrorGrpcClient.Clone(Action{IMirrorGrpcContext})"/> method call.
    /// </param>
    /// <param name="parent">
    /// The parent <see cref="MirrorContextStack"/> if this creation is a result of a 
    /// <see cref="ConsensusClient.Clone(Action{IConsensusContext})"/> method call.
    /// </param>
    private MirrorGrpcClient(MirrorContextStack parent, Action<IMirrorGrpcContext>? configure)
    {
        _context = new MirrorContextStack(parent);
        configure?.Invoke(_context);
    }
    /// <summary>
    /// Updates the configuration of this instance of a mirror client thru 
    /// implementation of the supplied <see cref="IMirrorGrpcContext"/> callback method.
    /// </summary>
    /// <param name="configure">
    /// The callback method receiving the <see cref="IMirrorGrpcContext"/> object providing 
    /// the configuration details of this client instance.  Values can be retrieved 
    /// and set within the context of the method invocation.
    /// </param>
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
    /// Creates a new child context based on the current context instance.  
    /// Includes an optional configuration method that can be immediately 
    /// applied to the new context.  This method is used internally to create 
    /// contexts for cloned clients and network method calls having custom 
    /// configuration callbacks.
    /// </summary>
    private MirrorContextStack CreateChildContext(Action<IMirrorGrpcContext>? configure)
    {
        var context = new MirrorContextStack(_context);
        configure?.Invoke(context);
        return context;
    }
    /// <summary>
    /// .NET Asynchronous dispose method.
    /// </summary>
    /// <remarks>
    /// Closes any GRPC channels solely owned by this <code>Mirror</code> instance.
    /// </remarks>
    /// <returns>
    /// An Async Task.
    /// </returns>
    public ValueTask DisposeAsync()
    {
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
    /// Returns only after one of the four conditions ocurr: the output channel is 
    /// completed by calling code; the cancelation token provided in the params is 
    /// signaled; the maximum number of topic messages was returned as configured in
    /// the params; or if the mirror stream faults during streaming, in which case a 
    /// <see cref="MirrorGrpcException"/> is thrown.
    /// </returns>
    /// <exception cref="ArgumentNullException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing or a parameter is invalid.</exception>
    /// <exception cref="MirrorGrpcException">If the mirror node stream faulted during request processing or upon submission.</exception>
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
            throw new InvalidOperationException("The Mirror Node Urul has not been configured. Please check that 'Url' is set in the Mirror context.");
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
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            // Cancelled is an expected closing condition, not an error
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
            throw new MirrorGrpcException($"The Mirror node is not avaliable at this time.", MirrorGrpcExceptionCode.Unavailable, ex);
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
                    Concensus = data.ConsensusTimestamp.ToConsensusTimeStamp(),
                    Messsage = data.Message.Memory,
                    RunningHash = data.RunningHash.Memory,
                    SequenceNumber = data.SequenceNumber,
                    SegmentInfo = data.ChunkInfo is not null ? new MessageSegmentInfo(data.ChunkInfo) : null
                };
                if (!writer.TryWrite(message))
                {
                    while (await writer.WaitToWriteAsync().ConfigureAwait(false))
                    {
                        if (!writer.TryWrite(message))
                        {
                            cancelTokenSource.Cancel();
                            return;
                        }
                    }
                }
            }
        }
    }
    /// <summary>
    /// The default algorithm for creatting channels for the
    /// gRPC mirror streaming client.  This implementation sets
    /// the keep alive timeout of 30s, ping delay 60s and
    /// keep alive policy of always.  Testing has shown this is
    /// the best method for keepign the HCS streaming service alive
    /// for monitoring an HCS stream.
    /// </summary>
    /// <param name="uri">
    /// The URI endpoint of the gRPC mirror node HCS stream.
    /// </param>
    /// <returns>
    /// A GrpcChannel pointing to the URI of the mirror node endpoing.
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
            }
        };
        return GrpcChannel.ForAddress(uri, options);
    }
}