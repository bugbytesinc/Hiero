using Proto;

namespace Hiero;

/// <summary>
/// Record produced from creating a new consensus message topic.
/// </summary>
public sealed record CreateTopicRecord : TransactionRecord
{
    /// <summary>
    /// The newly created topic instance address.
    /// </summary>
    /// <remarks>
    /// The value will be <code>None</code> if the create acocunt
    /// method was scheduled as a pending transaction.
    /// </remarks>
    public EntityId Topic { get; internal init; }
    /// <summary>
    /// Internal Constructor of the record.
    /// </summary>
    internal CreateTopicRecord(Proto.TransactionRecord record) : base(record)
    {
        Topic = record.Receipt.TopicID.AsAddress();
    }
}