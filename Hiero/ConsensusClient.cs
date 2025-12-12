using Grpc.Net.Client;
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
    private readonly ConsensusContextStack _context;
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
        _context = new ConsensusContextStack(channelFactory)
        {
            FeeLimit = 30_00_000_000,
            TransactionDuration = TimeSpan.FromSeconds(120),
            RetryCount = 5,
            RetryDelay = TimeSpan.FromMilliseconds(200),
            QueryTip = 0,
            SignaturePrefixTrimLimit = 0,
            AdjustForLocalClockDrift = false,
            ThrowIfNotSuccess = true
        }.GetConfigured(configure ?? (_ => { }));
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
    /// The parent <see cref="ConsensusContextStack"/> if this creation is a result of a 
    /// <see cref="ConsensusClient.Clone(Action{IConsensusContext})"/> method call.
    /// </param>
    private ConsensusClient(ConsensusContextStack parent, Action<IConsensusContext>? configure)
    {
        _context = parent.GetConfigured(configure ?? (_ => { }));
    }
    /// <summary>
    /// Updates the configuration of this instance of a client through 
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
    /// Creates, signs, submits a transaction and waits for a response from 
    /// the target consensus node.  Returning the precheck response code.
    /// A <see cref="PrecheckException"/> may be thrown under certain invalid
    /// input scenarios.
    /// </summary>
    /// <remarks>
    /// This method will wait for the target consensus node to respond with 
    /// a code other than <see cref="ResponseCode.Busy"/> or 
    /// <see cref="ResponseCode.InvalidTransactionStart"/> if applicable, 
    /// until such time as the retry count is exhausted, in which case it 
    /// is possible to receive a <see cref="ResponseCode.Busy"/> response.
    /// </remarks>
    /// <typeparam name="T">
    /// The type of <see cref="TransactionReceipt"/> returned by the request.
    /// </typeparam>
    /// <param name="transactionParams">
    /// Transaction input parameters.
    /// </param>
    /// <param name="configure">
    /// Optional callback to configure the calling context immediately 
    /// before assembling the transaction for submission.
    /// </param>
    /// <returns>
    /// The precheck <see cref="ResponseCode"/> returned from the request
    /// after waiting for submission retries if applicable.
    /// </returns>
    public async Task<ResponseCode> SubmitAsync<T>(TransactionParams<T> transactionParams, Action<IConsensusContext>? configure) where T : TransactionReceipt
    {
        await using var context = BuildChildContext(configure);
        var networkParams = transactionParams.GetNetworkParams();
        var (networkTransaction, signedTransactionBytes, transactionId, cancellationToken) = await Engine.EncodeAndSignAsync(context, networkParams, true).ConfigureAwait(false);
        var transaction = new Proto.Transaction { SignedTransactionBytes = signedTransactionBytes };
        var precheck = await Engine.SubmitMessageAsync(context, transaction, networkTransaction.InstantiateNetworkRequestMethod, cancellationToken).ConfigureAwait(false);
        return (ResponseCode)precheck.NodeTransactionPrecheckCode;
    }
    /// <summary>
    /// Creates, signs, submits a transaction and waits for a response from 
    /// the target consensus node, returning a receipt.
    /// </summary>
    /// <typeparam name="T">
    /// The type of receipt to return.
    /// </typeparam>
    /// <param name="transactionParams">
    /// The details of the transaction to create, sign and submit.
    /// </param>
    /// <param name="configure">
    /// Optional callback to configure the calling context immediately 
    /// before assembling the transaction for submission.
    /// </param>
    /// <returns>
    /// A receipt object.
    /// </returns>
    /// <exception cref="PrecheckException">
    /// If there was a problem submitting the request, including the consensus node
    /// considering the request invalid.
    /// </exception>
    /// <exception cref="TransactionException">
    /// If the consensus node returned a failure code and throw on failure is set to
    /// <code>true</code> in the client context configuration.
    /// </exception>
    /// <exception cref="ConsensusException">
    /// Under heavy load, the network may not process the transaction before it expires.
    /// </exception>
    public async Task<T> ExecuteAsync<T>(TransactionParams<T> transactionParams, Action<IConsensusContext>? configure) where T : TransactionReceipt
    {
        await using var context = BuildChildContext(configure);
        var networkParams = transactionParams.GetNetworkParams();
        var (networkTransaction, signedTransactionBytes, transactionId, cancellationToken) = await Engine.EncodeAndSignAsync(context, networkParams, true).ConfigureAwait(false);
        return await Engine.ExecuteAsync(context, signedTransactionBytes, networkParams, networkTransaction, transactionId, cancellationToken).ConfigureAwait(false);
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
    internal ConsensusContextStack BuildChildContext(Action<IConsensusContext>? configure)
    {
        return configure is null ? _context.GetWithAddRef() : _context.GetConfigured(configure);
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