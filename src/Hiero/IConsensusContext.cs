// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;

namespace Hiero;
/// <summary>
/// A <see cref="ConsensusClient"/> instance’s configuration.
/// </summary>
/// <remarks>
/// This interface exposes the current configuration context for a 
/// <see cref="ConsensusClient"/> instance.  When accessed through a 
/// <see cref="ConsensusClient.Configure(Action{IConsensusContext})"/>, 
/// <see cref="ConsensusClient.Clone(Action{IConsensusContext})"/> or one of the 
/// network request methods, calling code can interrogate the 
/// object for configuration details and update as necessary.  
/// Typically, the bare minimum that must be configured in a 
/// context in order to access the network are values for 
/// the Node <see cref="IConsensusContext.Endpoint"/> and 
/// <see cref="IConsensusContext.Payer"/> Address.  The other default 
/// values are typically suitable for most interactions with 
/// the network.
/// </remarks>
public interface IConsensusContext
{
    /// <summary>
    /// The consensus node (gRPC address and node account) that
    /// requests are sent to.  The payer is configured separately
    /// by <see cref="Payer"/>.
    /// </summary>
    ConsensusNodeEndpoint? Endpoint { get; set; }
    /// <summary>
    /// The address of the account that pays for transactions 
    /// submitted to the network, otherwise known as the 
    /// "Operator".
    /// </summary>
    EntityId? Payer { get; set; }
    /// <summary>
    /// The private key, keys or signing callback method that 
    /// are needed to sign the transaction.  At a minimum, this
    /// typically includes the Payer's signing key, but can also
    /// include other signatories such as those required when 
    /// creating files and contracts.
    /// </summary>
    /// <remarks>
    /// Every key added into this context will sign every 
    /// transaction made by the owning client object.  This
    /// includes the payment for a paid query, such as a
    /// transaction record lookup.  It is important to note
    /// that many of the API calls have additional opportunities
    /// to identify specific signatories for various transactions.
    /// It is not necessary to add all the keys on this context
    /// for say, a key rotation on an account.  It is recommended to place
    /// only the keys associated with the Payer account in this
    /// property.  Not doing so may result in extraneous unnecessary
    /// signatures being sent with transactions.  If this happens
    /// the transaction cost goes up due to the larger transaction
    /// size, wasting crypto due to unnecessary signature processing
    /// fees.
    /// </remarks>
    Signatory? Signatory { get; set; }
    /// <summary>
    /// The maximum fee the payer is willing to pay for a transaction.
    /// </summary>
    long FeeLimit { get; set; }
    /// <summary>
    /// How long a submitted transaction stays valid, counted from its
    /// valid start.  Once this window has passed without consensus, the
    /// network can no longer execute the transaction.  The default is
    /// 180 seconds, the most the network allows.
    /// </summary>
    /// <remarks>
    /// A long window costs nothing when the transaction succeeds and gives
    /// the network more time to decide.  Keep the retry window
    /// (<see cref="RetryCount"/> and <see cref="RetryDelay"/>) at least
    /// this long, and keep the window with the transaction id: a
    /// transaction whose outcome is unknown is settled on a mirror node by
    /// its id, and cannot execute once the mirror's latest consensus time
    /// is past its valid start plus this window (see
    /// <see cref="ConsensusException"/>).
    /// </remarks>
    TimeSpan TransactionDuration { get; set; }
    /// <summary>
    /// The default memo associated with transactions sent to the network.
    /// </summary>
    string? Memo { get; set; }
    /// <summary>
    /// If set to <code>true</code> the library client will automatically
    /// scan for <see cref="ResponseCode.InvalidTransactionStart"/> responses
    /// from the server and adjust the automatic generation of transaction
    /// IDs as appropriate to compensate for local clock drift from the
    /// network server's clock.  This can help reduce the wait time for
    /// acceptance of transactions if the local clock deviates from the
    /// network time by too wide a margin.  It is only recommended to 
    /// enable this setting if you are having performance problems related
    /// to clock drift and are unable to resolve at the environment level.
    /// The default for this value is <code>false</code>.
    /// </summary>
    bool AdjustForLocalClockDrift { get; set; }
    /// <summary>
    /// If set to <code>true</code> (default) the library client throws when
    /// a transaction does not end in Success: a
    /// <see cref="PrecheckException"/> if the node refused it, a
    /// <see cref="TransactionException"/> if it reached consensus and
    /// failed, or a <see cref="ConsensusException"/> if its outcome is
    /// unknown.  Queries still throw if the operation cannot be executed
    /// without error.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For backwards compatibility, this value defaults to <code>true</code>.
    /// </para>
    /// <para>
    /// When <code>false</code>, a transaction's receipt is returned whatever
    /// its status, so read <see cref="TransactionReceipt.Status"/>: only
    /// <see cref="ResponseCode.Success"/> is known to have executed, and
    /// <see cref="ResponseCode.Unknown"/> or
    /// <see cref="ResponseCode.ReceiptNotFound"/> means no verdict was
    /// returned and the outcome is unknown. A node with no receipt to give
    /// yields a stand-in receipt carrying
    /// <see cref="ResponseCode.ReceiptNotFound"/>. A
    /// <see cref="PrecheckException"/> is still thrown.
    /// </para>
    /// </remarks>
    bool ThrowIfNotSuccess { get; set; }
    /// <summary>
    /// The number of times the client retries a request: resending a
    /// transaction or query the node answered with BUSY (or, for a
    /// transaction, INVALID_TRANSACTION_START), retrying after a transport
    /// failure, and polling for a transaction's receipt until the network
    /// reaches consensus.  The default is 43.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Retries wait linearly longer: the n-th retry waits n ×
    /// <see cref="RetryDelay"/>, so the client keeps trying for
    /// <c>RetryDelay × RetryCount × (RetryCount + 1) / 2</c> in all.  The
    /// defaults, 43 retries at a 200 ms step, give 189 seconds, which covers
    /// the default 180-second <see cref="TransactionDuration"/>.
    /// </para>
    /// <para>
    /// Keep that total at least as long as
    /// <see cref="TransactionDuration"/>.  Hedera finalises a transaction
    /// seconds after a node accepts it, sometimes longer.  A client that
    /// stops waiting before the transaction's validity ends cannot tell
    /// "failed" from "still deciding", and treating the second as the first
    /// sends value twice or refunds a payment that happened.  A receipt
    /// still pending when the retries run out is reported as a
    /// <see cref="ConsensusException"/>: the outcome is unknown, not
    /// failed.  With <see cref="ThrowIfNotSuccess"/> set to
    /// <code>false</code>, that receipt is returned instead, with a
    /// <see cref="TransactionReceipt.Status"/> of
    /// <see cref="ResponseCode.Unknown"/>.
    /// </para>
    /// <para>
    /// The same count bounds the retries against a node that is busy or
    /// unreachable, so with the defaults such a node takes about three
    /// minutes to fail.  A caller that must answer sooner, such as an HTTP
    /// request handler, should wait a bounded time, answer "pending", and
    /// let the submission finish in the background rather than shorten
    /// the window.
    /// </para>
    /// </remarks>
    int RetryCount { get; set; }
    /// <summary>
    /// The step by which the wait between retries grows.  The first retry
    /// waits this long, the second twice as long, the third three times as
    /// long, and so on.  The default is 200 milliseconds.
    /// </summary>
    /// <remarks>
    /// See <see cref="RetryCount"/> for the total time the client keeps
    /// trying, which should cover <see cref="TransactionDuration"/>.
    /// </remarks>
    TimeSpan RetryDelay { get; set; }
    /// <summary>
    /// The additional amount of tinybars to add to the estimated
    /// cost of a QueryAsync when a query fee is required.  Sometimes the
    /// network can under-report the required query fee.  For critical
    /// application paths, this value can be used to hedge any last
    /// nanosecond changes to the cost of a query.  Most use cases
    /// will not need to set this value unless they frequently see
    /// "The Network Changed the price of ..." errors in their logs.
    /// </summary>
    long QueryTip { get; set; }
    /// <summary>
    /// The smallest desired signature map prefix value length.  
    /// The client will trim the length of signature prefix identifiers 
    /// in a signature map to the smallest value possible, but not less 
    /// than this value.  If the value is set to 0 (the default) the 
    /// minimum possible feasible value will be used when multiple 
    /// signatures are added to a transaction, omitting the prefix 
    /// completely if only one signature is provided.  This does not 
    /// guarantee a minimum length of a signature prefix (a shorter 
    /// value may be provided to the client by the signing algorithm) 
    /// only that the client will not actively reduce the value beyond this limit.
    /// </summary>
    int SignaturePrefixTrimLimit { get; set; }
    /// <summary>
    /// Override the automatic generation of a transaction ID and use 
    /// the given Transaction ID.  Can be useful for submitting the 
    /// same transaction to multiple client/gateways.
    /// </summary>
    /// <remarks>
    /// For a transaction that moves value, choose the id before submitting
    /// with <see cref="UtilityExtensions.CreateNewTransactionId"/>, store
    /// it, and set it here for that one call.  It is then the idempotency
    /// key, the mirror node lookup if the outcome turns out to be unknown,
    /// and, with <see cref="TransactionDuration"/>, the clock that says when
    /// the transaction can no longer execute.  Set it per call, not on a
    /// shared client: every transaction needs its own id.
    /// </remarks>
    TransactionId? TransactionId { get; set; }
    /// <summary>
    /// Called by the library just before the serialized protobuf 
    /// is sent to the Hedera Network.  Together with
    /// <see cref="OnResponseReceived"/>, this exposes the underlying
    /// protobuf messages the library sends and receives
    /// (the protobuf types are also publicly available in the library).
    /// This method can be useful for logging and tracking purposes. 
    /// It could also be used in advanced scenarios to modify the 
    /// protobuf message just before sending.
    /// </summary>
    /// <remarks>
    /// Together with <see cref="OnResponseReceived"/>, this is the quickest
    /// way to see what the client is doing.  It fires once per request,
    /// not once per retry: a receipt query polled ten times is one send
    /// and eleven responses, numbered by <see cref="OnResponseReceived"/>.
    /// </remarks>
    Action<IMessage>? OnSendingRequest { get; set; }
    /// <summary>
    /// Called by the library just after receiving a response from the 
    /// server and before processing that response.  The information 
    /// includes a retry number, the response from the first send 
    /// being 0.  Subsequent numbers count retries of the same request:
    /// resends after a BUSY (or, for a transaction,
    /// INVALID_TRANSACTION_START) response, retries after a transport
    /// failure, and re-polls of a receipt query that has no verdict yet.
    /// A retry does not raise another <see cref="OnSendingRequest"/>
    /// call.  This information
    /// can be useful for logging and troubleshooting efforts.
    /// </summary>
    Action<int, IMessage>? OnResponseReceived { get; set; }
    /// <summary>
    /// Clears a property on the context.  This is the only way to clear 
    /// a property value on the context so that a parent value can be 
    /// used instead.  Setting a value to <code>null</code> does not 
    /// clear the value, it sets it to <code>null</code> for the 
    /// current context and child contexts. 
    /// </summary>
    /// <param name="name">
    /// The name of the property to reset, must be one of the public 
    /// properties of the <code>IConsensusContext</code>.  We suggest using 
    /// the <code>nameof()</code> operator to ensure type safety.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// when an invalid <code>name</code> is provided.
    /// </exception>
    void Reset(string name);
}