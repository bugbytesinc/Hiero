// SPDX-License-Identifier: Apache-2.0
using Proto;

namespace Hiero;

/// <summary>
/// Record produced from creating a new scheduled transaction.
/// </summary>
public sealed record ScheduleRecord : TransactionRecord
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
    /// Internal Constructor of the record.
    /// </summary>
    internal ScheduleRecord(Proto.TransactionRecord record) : base(record)
    {
        Schedule = record.Receipt.ScheduleID?.ToAddress() ?? EntityId.None;
        ScheduledTransactionId = record.Receipt.ScheduledTransactionID.AsTransactionId();
    }
}
