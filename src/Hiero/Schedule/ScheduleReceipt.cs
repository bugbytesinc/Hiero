// SPDX-License-Identifier: Apache-2.0
using Proto;

namespace Hiero;
/// <summary>
/// Receipt produced from creating a new scheduled transaction.
/// </summary>
public sealed record ScheduleReceipt : TransactionReceipt
{
    /// <summary>
    /// The address of the newly created schedule record.
    /// </summary>
    public EntityId Schedule { get; internal init; }
    /// <summary>
    /// The transaction ID that will be used when the scheduled
    /// transaction is executed by the network.
    /// </summary>
    public TransactionId ScheduledTransactionId { get; internal init; }
    /// <summary>
    /// Internal Constructor of the receipt.
    /// </summary>
    internal ScheduleReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt) : base(transactionId, receipt)
    {
        Schedule = receipt.ScheduleID?.ToAddress() ?? EntityId.None;
        ScheduledTransactionId = receipt.ScheduledTransactionID.AsTransactionId();
    }
}
