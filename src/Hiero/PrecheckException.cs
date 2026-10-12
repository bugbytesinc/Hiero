// SPDX-License-Identifier: Apache-2.0
namespace Hiero;

/// <summary>
/// Represents the condition where a submitted transaction
/// failed the pre-check validation by the network gateway node.
/// </summary>
/// <remarks>
/// <para>
/// The gateway node answered the last attempt with a refusal. Whether that
/// proves the transaction did not execute depends on <see cref="Status"/>.
/// </para>
/// <para><strong>Codes about the transaction itself are definite:</strong>
/// for example <see cref="ResponseCode.InvalidSignature"/>,
/// <see cref="ResponseCode.InsufficientPayerBalance"/>,
/// <see cref="ResponseCode.InvalidAccountId"/>,
/// <see cref="ResponseCode.TokenNotAssociatedToAccount"/> and
/// <see cref="ResponseCode.InvalidTransactionBody"/>. Nothing executed and
/// no fee was charged; fix the request before sending it again.</para>
/// <para><strong>Codes about the node, the network or the connection prove
/// nothing:</strong> <see cref="ResponseCode.Unknown"/> (no answer could be
/// had, for example a refused connection), <see cref="ResponseCode.RpcError"/>,
/// <see cref="ResponseCode.Busy"/>,
/// <see cref="ResponseCode.PlatformTransactionNotCreated"/>,
/// <see cref="ResponseCode.PlatformNotActive"/>,
/// <see cref="ResponseCode.DuplicateTransaction"/> (a transaction with this
/// id was already accepted) and <see cref="ResponseCode.TransactionExpired"/>.
/// The SDK may have sent the same transaction earlier in the call, and an
/// answer can be lost after the network accepted it. Treat the outcome as
/// unknown: do not resubmit, and settle it on a mirror node by
/// <see cref="TransactionId"/> (see <see cref="ConsensusException"/>). Code
/// that moves value is safest treating every <c>PrecheckException</c> this
/// way.</para>
/// <para>The SDK itself retries <see cref="ResponseCode.Busy"/> and
/// <see cref="ResponseCode.InvalidTransactionStart"/> within
/// <see cref="IConsensusContext.RetryCount"/>, so seeing either here means
/// those retries ran out.</para>
/// <para>If <see cref="Status"/> is
/// <see cref="ResponseCode.InsufficientTxFee"/>, the
/// <see cref="RequiredFee"/> property contains the minimum fee (in tinybars)
/// the gateway expects — increase <c>IConsensusContext.FeeLimit</c> to at
/// least this amount and retry <em>with the same
/// <see cref="IConsensusContext.TransactionId"/></em>, so the network's
/// duplicate check stops a second execution should an earlier attempt in
/// the call have gone through.</para>
/// <para><strong>Queries:</strong> a paid query's payment can be charged
/// even when the answer carries a refusal code. Depending on the network
/// version, the node may submit the payment before checking or answering
/// the query, so "no fee was charged" holds for a query only when the
/// payment itself was refused, as with
/// <see cref="ResponseCode.InsufficientTxFee"/>.</para>
/// </remarks>
public sealed class PrecheckException : Exception
{
    /// <summary>
    /// The status code returned by the gateway node.
    /// </summary>
    public ResponseCode Status { get; private set; }
    /// <summary>
    /// The transaction ID corresponding to the request that failed.
    /// </summary>
    public TransactionId TransactionId { get; private set; }
    /// <summary>
    /// If the returned status is <see cref="ResponseCode.InsufficientTxFee"/>
    /// this value will contain the transaction fee necessary to execute the transaction.
    /// </summary>
    public ulong RequiredFee { get; private set; }
    /// <summary>
    /// Exception constructor.
    /// </summary>
    /// <param name="message">
    /// A message describing the nature of the problem.
    /// </param>
    /// <param name="transaction">
    /// The transaction ID corresponding to the request that failed.
    /// </param>
    /// <param name="code">
    /// The status code returned by the gateway node.
    /// </param>
    /// <param name="requiredFee">
    /// The cost value returned for insufficient transaction fee errors.
    /// </param>
    public PrecheckException(string message, TransactionId transaction, ResponseCode code, ulong requiredFee) : base(message)
    {
        Status = code;
        TransactionId = transaction;
        RequiredFee = requiredFee;
    }
    /// <summary>
    /// Exception constructor.
    /// </summary>
    /// <param name="message">
    /// A message describing the nature of the problem.
    /// </param>
    /// <param name="transaction">
    /// The transaction ID corresponding to the request that failed.
    /// </param>
    /// <param name="innerException">
    /// Inner exception causing this error, typically reserved for
    /// fundamental GRPC pipeline exceptions.
    /// </param>
    /// <param name="code">
    /// The status code returned by the gateway node.
    /// </param>
    /// <param name="requiredFee">
    /// The cost value returned for insufficient transaction fee errors.
    /// </param>
    public PrecheckException(string message, TransactionId transaction, ResponseCode code, ulong requiredFee, Exception innerException) : base(message, innerException)
    {
        Status = code;
        TransactionId = transaction;
        RequiredFee = requiredFee;
    }
}