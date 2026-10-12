// SPDX-License-Identifier: Apache-2.0
using Grpc.Net.Client;
using Hiero.Implementation;
using System.Diagnostics;
using System.Net.Security;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Hiero.Test.Helpers")]
[assembly: InternalsVisibleTo("Hiero.Test.Unit")]
[assembly: InternalsVisibleTo("Hiero.Test.Integration")]

namespace Hiero;
/// <summary>
/// Hiero Network Consensus (Gossip) Node Client
/// </summary>
/// <remarks>
/// This component facilitates interaction with the Hiero Network.  
/// It manages the communication channels with the network and 
/// serialization of requests and responses.  This library generally 
/// shields the client code from directly interacting with the 
/// underlying protobuf communication layer but does provide hooks 
/// allowing advanced low-level manipulation of messages if necessary.
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
    /// Set once this client has released its context, so a second
    /// dispose cannot release the shared parent chain a second time.
    /// </summary>
    private int _disposed;
    /// <summary>
    /// Creates a new instance of a Hiero Network ConsensusClient.
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
    /// Creates a new instance of a Hiero Network ConsensusClient with a custom
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
    /// the ConsensusClient (which is a function of the current context's ConsensusNodeEndpoint parameter).
    /// The factory replaces the default one entirely, including its TLS certificate pinning:
    /// consensus nodes' <c>https</c> endpoints present self-signed certificates that standard
    /// validation rejects, so for an endpoint carrying a
    /// <see cref="ConsensusNodeEndpoint.CertificateHash"/> the factory's
    /// <c>RemoteCertificateValidationCallback</c> should call
    /// <see cref="ConsensusNodeEndpoint.MatchesCertificate"/> instead.
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
            // The network's maximum validity, waited out in full: 43
            // retries at a linearly growing 200 ms step span 189 s, so a
            // receipt still pending is followed until the transaction can
            // no longer execute (see IConsensusContext.RetryCount).
            TransactionDuration = TimeSpan.FromSeconds(180),
            RetryCount = 43,
            RetryDelay = TimeSpan.FromMilliseconds(200),
            QueryTip = 0,
            SignaturePrefixTrimLimit = 0,
            AdjustForLocalClockDrift = false,
            ThrowIfNotSuccess = true
        }.CloneAndConfigure(configure ?? (_ => { }));
    }
    /// <summary>
    /// Internal implementation of client creation.  Accounts for  newly created 
    /// clients and cloning of clients alike.
    /// </summary>
    /// <param name="parent">
    /// The parent <see cref="ConsensusContextStack"/> if this creation is a result of a
    /// <see cref="ConsensusClient.Clone(Action{IConsensusContext})"/> method call.
    /// </param>
    /// <param name="configure">
    /// The optional <see cref="IConsensusContext"/> callback method, passed in from public
    /// instantiation or a <see cref="ConsensusClient.Clone(Action{IConsensusContext})"/> method call.
    /// </param>
    private ConsensusClient(ConsensusContextStack parent, Action<IConsensusContext>? configure)
    {
        _context = parent.CloneAndConfigure(configure ?? (_ => { }));
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
    /// <remarks>
    /// Mutates this client's own context in place. Every operation already in
    /// flight reads its inherited settings through that context, so calling
    /// this concurrently with active work can let a call observe a partially
    /// applied configuration. Configure before use, or while the client is
    /// idle. For a live client, prefer the per-call <c>configure</c> argument
    /// (scoped to that call alone) or <see cref="Clone"/> for a separately
    /// configured instance.
    /// </remarks>
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
    /// parent.  Changes to the parent’s configuration will reflect in this instance’s
    /// configuration while changes in this instance’s configuration will not be reflected 
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
    /// <para>
    /// This method will wait for the target consensus node to respond with 
    /// a code other than <see cref="ResponseCode.Busy"/> or 
    /// <see cref="ResponseCode.InvalidTransactionStart"/> if applicable, 
    /// until such time as the retry count is exhausted, in which case it 
    /// is possible to receive a <see cref="ResponseCode.Busy"/> response.
    /// </para>
    /// <para>
    /// The code returned is the node's answer to the submission, not the
    /// transaction's outcome.  <see cref="ResponseCode.Ok"/> means only that
    /// the node accepted it and the network has yet to decide, and a final
    /// <see cref="ResponseCode.Busy"/> leaves the outcome unknown, since an
    /// earlier attempt in the call may have been accepted.  This method does
    /// not return the transaction id, so choose one first with
    /// <see cref="UtilityExtensions.CreateNewTransactionId"/>, set it as
    /// <see cref="IConsensusContext.TransactionId"/>, and keep it: it is the
    /// only way to learn the outcome, from a receipt or a mirror node.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">
    /// The type of <see cref="TransactionReceipt"/> tied to the transaction
    /// parameters.  This method produces no receipt; it returns only the
    /// precheck code.
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
    public async Task<ResponseCode> SubmitAsync<T>(TransactionParams<T> transactionParams, Action<IConsensusContext>? configure = null) where T : TransactionReceipt
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
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="TransactionException">
    /// If the consensus node returned a failure code and throw on failure is set to
    /// <code>true</code> in the client context configuration.
    /// </exception>
    /// <exception cref="ConsensusException">
    /// If the outcome could not be confirmed: the transaction may have executed, so settle it
    /// by its transaction id on a mirror node rather than resubmitting it.
    /// </exception>
    public async Task<T> ExecuteAsync<T>(TransactionParams<T> transactionParams, Action<IConsensusContext>? configure = null) where T : TransactionReceipt
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
    internal ConsensusContextStack BuildChildContext(Action<IConsensusContext>? configure)
    {
        return configure is null ? _context.CopyWithAddRef() : _context.CloneAndConfigure(configure);
    }
    /// <summary>
    /// The default algorithm for creating channels for the client.
    /// </summary>
    /// <remarks>
    /// When the endpoint is an <c>https</c> endpoint carrying an expected TLS
    /// certificate hash (<see cref="ConsensusNodeEndpoint.CertificateHash"/>),
    /// the channel is built with a validation callback that pins the connection
    /// to that hash instead of using standard chain/hostname validation.  This is
    /// required because Hiero consensus nodes present self-signed placeholder
    /// certificates that cannot pass ordinary TLS validation (see the address
    /// book's published <c>node_cert_hash</c>).  Otherwise — a plaintext endpoint,
    /// or an <c>https</c> endpoint with no supplied hash such as a proxy-fronted
    /// DNS endpoint that presents a chain-valid certificate — the underlying
    /// system gRPC / TLS defaults are used unchanged.
    /// </remarks>
    /// <param name="endpoint">
    /// A ConsensusNodeEndpoint holding the address information for the channel
    /// to be created.
    /// </param>
    /// <returns>
    /// A GrpcChannel pointing to the URI of the associated endpoint.
    /// </returns>
    private static GrpcChannel DefaultChannelFactory(ConsensusNodeEndpoint endpoint)
    {
        if (endpoint.Uri.Scheme == Uri.UriSchemeHttps && !endpoint.CertificateHash.IsEmpty)
        {
            var options = new GrpcChannelOptions
            {
                HttpHandler = new SocketsHttpHandler
                {
                    SslOptions = new SslClientAuthenticationOptions
                    {
                        RemoteCertificateValidationCallback = (_, cert, _, _) => endpoint.MatchesCertificate(cert),
                    },
                },
                // The channel must dispose the handler it was given:
                // without this, a disposed client leaves the handler's
                // connection pool holding a live connection to the
                // consensus node until garbage collection, so
                // applications that create and dispose clients
                // repeatedly accumulate live connections.
                DisposeHttpClient = true
            };
            return GrpcChannel.ForAddress(endpoint.Uri, options);
        }
        return GrpcChannel.ForAddress(endpoint.Uri);
    }
}