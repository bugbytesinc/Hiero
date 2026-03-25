// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;

namespace Hiero;
/// <summary>
/// A transaction record containing information regarding
/// one or more newly created airdrops.
/// </summary>
public sealed record AirdropRecord : TransactionRecord
{
    /// <summary>
    /// The list of pending airdrops created by this transaction.
    /// </summary>
    public IReadOnlyList<AirdropAmount> Airdrops { get; internal init; }
    /// <summary>
    /// Internal Constructor of the record.
    /// </summary>
    internal AirdropRecord(Proto.TransactionRecord record) : base(record)
    {
        Airdrops = record.NewPendingAirdrops.AsAirdropAmountList();
    }
}
