// SPDX-License-Identifier: Apache-2.0
namespace Hiero;

/// <summary>
/// Represents the condition where a submitted transaction
/// failed the pre-check validation by the network gateway node.
/// </summary>
/// <remarks>
/// <para>
/// A precheck failure means the transaction never reached consensus — no fees
/// were charged and no state was changed. Inspect <see cref="Status"/> to
/// determine why the gateway rejected it.
/// </para>
/// <para><strong>Transient codes (safe to retry with back-off):</strong>
/// <see cref="ResponseCode.Busy"/>,
/// <see cref="ResponseCode.PlatformTransactionNotCreated"/>. The SDK
/// auto-retries these based on <c>IConsensusContext.RetryCount</c> and
/// <c>RetryDelay</c>; if you see a <c>PrecheckException</c> with one of
/// these codes it means the retry budget was exhausted.</para>
/// <para><strong>Permanent codes (do not retry):</strong>
/// <see cref="ResponseCode.InvalidSignature"/>,
/// <see cref="ResponseCode.InsufficientAccountBalance"/>,
/// <see cref="ResponseCode.InvalidTransactionBody"/> — fix the request
/// configuration before resubmitting.</para>
/// <para>If <see cref="Status"/> is
/// <see cref="ResponseCode.InsufficientTxFee"/>, the
/// <see cref="RequiredFee"/> property contains the minimum fee (in tinybars)
/// the gateway expects — increase <c>IConsensusContext.FeeLimit</c> to at
/// least this amount and retry.</para>
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