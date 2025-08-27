using Proto;

namespace Hiero;
/// <summary>
/// Receipt produced from creating a new consensus message topic.
/// </summary>
public sealed record CreateTopicReceipt : TransactionReceipt
{
    /// <summary>
    /// The newly created or associated topic instance address.
    /// </summary>
    /// <remarks>
    /// The value will be <code>None</code> if the create topic
    /// method was scheduled as a pending transaction.
    /// </remarks>
    public EntityId Topic { get; internal init; }
    /// <summary>
    /// Internal Constructor of the receipt.
    /// </summary>
    internal CreateTopicReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt) : base(transactionId, receipt)
    {
        Topic = receipt.TopicID.AsAddress();
    }
}