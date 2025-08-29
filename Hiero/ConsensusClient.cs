﻿using Grpc.Net.Client;
using Hiero.Implementation;
using System.Diagnostics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Hashgraph.Test")]

namespace Hiero;
/// <summary>
/// Hedera Network Consensus (Gossip) Node Client
/// </summary>
/// <remarks>
/// This component facilitates interaction with the Hedera Network.  
/// It manages the communication channels with the network and 
/// serialization of requests and responses.  This library generally 
/// shields the client code from directly interacting with the 
/// underlying protobuf communication layer but does provide hooks 
/// allowing advanced low-level manipulation of messages if necessary.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ConsensusClient : IAsyncDisposable
{
    /// <summary>
    /// The context (stack) keeps a memory of configuration and preferences 
    /// within a variety of call contexts.  It can be cloned and tweaked as 
    /// required.  Preferences can be set on cloned or immediate call 
    /// contexts without changing parent contexts.  If a property is not set 
    /// in the current context, the system falls back to the parent context 
    /// for the value, and to its parent until a value has been set.
    /// </summary>
    private readonly GossipContextStack _context;
    /// <summary>
    /// Creates a new instance of an Hedera Network ConsensusClient.
    /// </summary>
    /// <remarks>
    /// Creating a new instance of a <code>ConsensusClient</code> initializes a new instance 
    /// of a client.  It will have a separate cache of GRPC channels to the network 
    /// and will maintain a separate configuration from other clients.  The constructor 
    /// takes an optional callback method that configures the details on how the 
    /// client should connect to the network and what accounts generally pay 
    /// transaction fees and other details.  See the <see cref="IConsensusContext"/> documentation 
    /// for configuration details.
    /// </remarks>
    /// <param name="configure">
    /// Optional configuration method that can set the location of the network node 
    /// accessing the network and how transaction fees shall be paid for.
    /// </param>
    public ConsensusClient(Action<IConsensusContext>? configure = null) : this(DefaultChannelFactory, configure)
    {
    }
    /// <summary>
    /// Creates a new instance of an Hedera Network ConsensusClient with a custom
    /// gRPC channel factory.
    /// </summary>
    /// <remarks>
    /// Creating a new instance of a <code>ConsensusClient</code> initializes a new instance 
    /// of a client.  It will have a separate cache of GRPC channels to the network 
    /// and will maintain a separate configuration from other clients.  The constructor 
    /// takes an optional callback method that configures the details on how the 
    /// client should connect to the network and what accounts generally pay 
    /// transaction fees and other details.  See the <see cref="IConsensusContext"/> documentation 
    /// for configuration details.
    /// </remarks>
    /// <param name="channelFactory">
    /// A custom callback method returning a new channel given the target ConsensusNodeEndpoint.
    /// Note, this method is only called once for each unique ConsensusNodeEndpoint requested by 
    /// the ConsensusClient (which is a function of the current context's ConsensusNodeEndpoint parameter)
    /// </param>
    /// <param name="configure">
    /// Optional configuration method that can set the location of the network node 
    /// accessing the network and how transaction fees shall be paid for.
    /// </param>
    public ConsensusClient(Func<ConsensusNodeEndpoint, GrpcChannel> channelFactory, Action<IConsensusContext>? configure = null)
    {
        if (channelFactory is null)
        {
            throw new ArgumentNullException(nameof(channelFactory));
        }
        // Create a Context with System Defaults 
        // that are unreachable and can't be "Reset".
        _context = new GossipContextStack(new GossipContextStack(channelFactory)
        {
            FeeLimit = 30_00_000_000,
            TransactionDuration = TimeSpan.FromSeconds(120),
            RetryCount = 5,
            RetryDelay = TimeSpan.FromMilliseconds(200),
            QueryTip = 0,
            SignaturePrefixTrimLimit = 0,
            AdjustForLocalClockDrift = false,
            ThrowIfNotSuccess = true
        });
        configure?.Invoke(_context);
    }
    /// <summary>
    /// Internal implementation of client creation.  Accounts for  newly created 
    /// clients and cloning of clients alike.
    /// </summary>
    /// <param name="configure">
    /// The optional <see cref="IConsensusContext"/> callback method, passed in from public 
    /// instantiation or a <see cref="ConsensusClient.Clone(Action{IConsensusContext})"/> method call.
    /// </param>
    /// <param name="parent">
    /// The parent <see cref="GossipContextStack"/> if this creation is a result of a 
    /// <see cref="ConsensusClient.Clone(Action{IConsensusContext})"/> method call.
    /// </param>
    private ConsensusClient(GossipContextStack parent, Action<IConsensusContext>? configure)
    {
        _context = new GossipContextStack(parent);
        configure?.Invoke(_context);
    }
    /// <summary>
    /// Updates the configuration of this instance of a client thru 
    /// implementation of the supplied <see cref="IConsensusContext"/> callback method.
    /// </summary>
    /// <param name="configure">
    /// The callback method receiving the <see cref="IConsensusContext"/> object providing 
    /// the configuration details of this client instance.  Values can be retrieved 
    /// and set within the context of the method invocation.
    /// </param>
    public void Configure(Action<IConsensusContext> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure), "Configuration action cannot be null.");
        }
        configure(_context);
    }
    /// <summary>
    /// Creates a new instance of the client having a shared base configuration with its 
    /// parent.  Changes to the parent’s configuration will reflect in this instances 
    /// configuration while changes in this instances configuration will not be reflected 
    /// in the parent configuration.
    /// </summary>
    /// <param name="configure">
    /// The callback method receiving the <see cref="IConsensusContext"/> object providing 
    /// the configuration details of this client instance.  Values can be retrieved 
    /// and set within the context of the method invocation.
    /// </param>
    /// <returns>
    /// A new instance of a client object.
    /// </returns>
    public ConsensusClient Clone(Action<IConsensusContext>? configure = null)
    {
        return new ConsensusClient(_context, configure);
    }
    /// <summary>
    /// Returns a human-readable debug string describing this client instance.
    /// </summary>
    public override string ToString()
    {
        return _context.Endpoint?.ToString() ?? "0.0.0@unconfigured";
    }
    /// <summary>
    /// .NET Asynchronous dispose method.
    /// </summary>
    /// <remarks>
    /// Closes any GRPC channels solely owned by this <code>ConsensusClient</code> instance.
    /// </remarks>
    /// <returns>
    /// An Async Task.
    /// </returns>
    public ValueTask DisposeAsync()
    {
        return _context.DisposeAsync();
    }
    /// <summary>
    /// Creates a new child context based on the current context instance.  
    /// Includes an optional configuration method that can be immediately 
    /// applied to the new context.  This method is used internally to create 
    /// contexts for cloned clients and network method calls having custom 
    /// configuration callbacks.
    /// </summary>
    internal GossipContextStack CreateChildContext(Action<IConsensusContext>? configure)
    {
        var context = new GossipContextStack(_context);
        configure?.Invoke(context);
        return context;
    }
    /// <summary>
    /// The default algorithm for creating channels for the client.
    /// It defaults to the underlying system gRPC defaults.
    /// </summary>
    /// <param name="endpoint">
    /// A ConsensusNodeEndpoint holding the address information for the channel
    /// to be created.
    /// </param>
    /// <returns>
    /// A GrpcChannel pointing to the URI of the associated endpoint.
    /// </returns>
    private static GrpcChannel DefaultChannelFactory(ConsensusNodeEndpoint endpoint)
    {
        return GrpcChannel.ForAddress(endpoint.Uri);
    }
}