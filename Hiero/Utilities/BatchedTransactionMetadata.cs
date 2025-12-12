namespace Hiero;
/// <summary>
/// Represents a single transaction within a batch, identifying
/// the transaction parameters, and other optional batch metadata.
/// </summary>
public sealed class BatchedTransactionMetadata : TransactionParams<TransactionReceipt>
{
    /// <summary>
    /// The transaction parameters for this transaction within the batch.
    /// </summary>
    public TransactionParams<TransactionReceipt> TransactionParams { get; set; } = default!;
    /// <summary>
    /// Optional payer entity for this individual batched transaction, 
    /// if not specified, it will be the main Payer identified by the 
    /// client context configuration.
    /// </summary>
    public EntityId? Payer { get; set; } = default!;
    /// <summary>
    /// Optional explicit endorsement that must be applied to the outer transaction batch
    /// transaction for this individual transaction to be executed. If not specified,
    /// the default endorsement will be used, which are signatories of the client's context.
    /// </summary>
    /// <remarks>
    /// This will override the optional endorsement specified in the <see cref="BatchedTransactionParams"/>.
    /// </remarks>
    public Endorsement? Endorsement { get; set; }
    /// <summary>
    /// Optional memo to attach to the transaction memo field.
    /// </summary>
    public string Memo { get; set; } = default!;
}
