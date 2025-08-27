namespace Hiero;
/// <summary>
/// Information identifying a pending transaction, includes the
/// address of the pending transaction record, plus the transaction
/// id that will exist representing the executed transaction if it
/// is ultimately executed (and not timed out or delted).
/// </summary>
public record PendingTransaction
{
    /// <summary>
    /// The identifier of the pending transaction 
    /// record held by the network.
    /// </summary>
    public EntityId Id { get; internal init; } = default!;
    /// <summary>
    /// The ID of the pending transaction, should it be executed.
    /// </summary>
    public TransactionId TxId { get; internal init; } = default!;
}