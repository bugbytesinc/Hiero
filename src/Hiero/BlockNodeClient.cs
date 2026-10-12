// SPDX-License-Identifier: Apache-2.0
using Grpc.Net.Client;
using Hiero.Implementation;
using System.Diagnostics.CodeAnalysis;

namespace Hiero;
/// <summary>
/// Hiero Block Node Client
/// </summary>
/// <remarks>
/// This client facilitates access to a Hiero Block Node: streaming
/// blocks from the block stream in near real-time, retrieving
/// individual blocks, querying node status, obtaining block contents
/// proofs and state snapshot references, and (for advanced producer
/// scenarios) publishing block streams.
/// <para>
/// The Block Node service and this client surface are experimental;
/// the API may change in any minor release while the upstream Block
/// Node protocol stabilizes.
/// </para>
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
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed partial class BlockNodeClient : IAsyncDisposable
{
    /// <summary>
    /// The context (stack) keeps a memory of configuration and preferences
    /// within a variety of call contexts.  It can be cloned and tweaked as
    /// required.  Preferences can be set on cloned or immediate call
    /// contexts without changing parent contexts.  If a property is not set
    /// in the current context, the system falls back to the parent context
    /// for the value, and to its parent until a value has been set.
    /// </summary>
    private readonly BlockNodeContextStack _context;
    /// <summary>
    /// Set once this client has released its context, so a second
    /// dispose cannot release the shared parent chain a second time.
    /// </summary>
    private int _disposed;
    /// <summary>
    /// Creates a new instance of a Hiero Block Node Client.
    /// </summary>
    /// <remarks>
    /// Creating a new instance of a <code>BlockNodeClient</code> initializes a new instance
    /// of a client.  It will have a separate cache of GRPC channels to the network
    /// and will maintain a separate configuration from other clients.  The constructor
    /// takes an optional callback method that configures the details on how the
    /// client should connect to the block node.  See the
    /// <see cref="IBlockNodeContext"/> documentation for configuration details.
    /// </remarks>
    /// <param name="configure">
    /// Optional configuration method that can set the location of the block node
    /// and retry behavior.
    /// </param>
    public BlockNodeClient(Action<IBlockNodeContext>? configure = null) : this(DefaultChannelFactory, configure)
    {
    }
    /// <summary>
    /// Creates a new instance of a Hiero Block Node Client with a
    /// custom gRPC channel factory.
    /// </summary>
    /// <remarks>
    /// Creating a new instance of a <code>BlockNodeClient</code> initializes a new instance
    /// of a client.  It will have a separate cache of GRPC channels to the network
    /// and will maintain a separate configuration from other clients.  The constructor
    /// takes an optional callback method that configures the details on how the
    /// client should connect to the block node.  See the
    /// <see cref="IBlockNodeContext"/> documentation for configuration details.
    /// </remarks>
    /// <param name="channelFactory">
    /// A custom callback method returning a new channel given the target block
    /// node URI.  Note, this method is only called once for each unique URI
    /// requested by the block node client (which is a function of the current
    /// context's URI parameter)
    /// </param>
    /// <param name="configure">
    /// Optional configuration method that can set the location of the block node
    /// and retry behavior.
    /// </param>
    public BlockNodeClient(Func<Uri, GrpcChannel> channelFactory, Action<IBlockNodeContext>? configure = null)
    {
        if (channelFactory is null)
        {
            throw new ArgumentNullException(nameof(channelFactory));
        }
        // Create a Context with System Defaults
        // that are unreachable and can't be "Reset".
        _context = BlockNodeContextStack.Configure(new BlockNodeContextStack(new BlockNodeContextStack(channelFactory)
        {
            RetryCount = 5,
            RetryDelay = TimeSpan.FromMilliseconds(200)
        }), configure);
    }
    /// <summary>
    /// Internal implementation of block node client creation.  Accounts for newly
    /// created clients and cloning of clients alike.
    /// </summary>
    /// <param name="parent">
    /// The parent <see cref="BlockNodeContextStack"/> if this creation is a result of a
    /// <see cref="BlockNodeClient.Clone(Action{IBlockNodeContext})"/> method call.
    /// </param>
    /// <param name="configure">
    /// The optional <see cref="IBlockNodeContext"/> callback method, passed in from public
    /// instantiation or a <see cref="BlockNodeClient.Clone(Action{IBlockNodeContext})"/> method call.
    /// </param>
    private BlockNodeClient(BlockNodeContextStack parent, Action<IBlockNodeContext>? configure)
    {
        _context = BlockNodeContextStack.Configure(new BlockNodeContextStack(parent), configure);
    }
    /// <summary>
    /// Updates the configuration of this instance of a block node client through
    /// implementation of the supplied <see cref="IBlockNodeContext"/> callback method.
    /// </summary>
    /// <param name="configure">
    /// The callback method receiving the <see cref="IBlockNodeContext"/> object providing
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
    public void Configure(Action<IBlockNodeContext> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure), "Configuration action cannot be null.");
        }
        configure(_context);
    }
    /// <summary>
    /// Creates a new instance of the block node client having a shared base configuration
    /// with its parent.  Changes to the parent’s configuration will reflect in this instances
    /// configuration while changes in this instances configuration will not be reflected
    /// in the parent configuration.
    /// </summary>
    /// <param name="configure">
    /// The callback method receiving the <see cref="IBlockNodeContext"/> object providing
    /// the configuration details of this client instance.  Values can be retrieved
    /// and set within the context of the method invocation.
    /// </param>
    /// <returns>
    /// A new instance of a client object.
    /// </returns>
    public BlockNodeClient Clone(Action<IBlockNodeContext>? configure = null)
    {
        return new BlockNodeClient(_context, configure);
    }
    /// <summary>
    /// .NET Asynchronous dispose method.
    /// </summary>
    /// <remarks>
    /// Closes any GRPC channels solely owned by this <code>BlockNodeClient</code> instance.
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
    /// Creates a new child context based on the current context instance.
    /// Includes an optional configuration method that can be immediately
    /// applied to the new context.  This method is used internally to create
    /// contexts for cloned clients and network method calls having custom
    /// configuration callbacks.
    /// </summary>
    private BlockNodeContextStack CreateChildContext(Action<IBlockNodeContext>? configure)
    {
        return BlockNodeContextStack.Configure(new BlockNodeContextStack(_context), configure);
    }
    /// <summary>
    /// The default algorithm for creating channels for the gRPC block
    /// node client.  This implementation sets the keep alive timeout
    /// of 30s, ping delay 60s and keep alive policy of always — the
    /// same tuning the mirror gRPC client uses for long-lived streams,
    /// which block stream subscriptions also are.
    /// </summary>
    /// <param name="uri">
    /// The URI endpoint of the gRPC block node.
    /// </param>
    /// <returns>
    /// A GrpcChannel pointing to the URI of the block node endpoint.
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
            // holding a keep-alive-pinged HTTP/2 connection to the block
            // node until garbage collection, so applications that create
            // and dispose clients repeatedly accumulate live connections.
            DisposeHttpClient = true
        };
        return GrpcChannel.ForAddress(uri, options);
    }
}
