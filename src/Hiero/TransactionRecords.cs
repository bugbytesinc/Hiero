// SPDX-License-Identifier: Apache-2.0
namespace Hiero;

/// <summary>
/// Every record the network holds for one transaction ID, grouped as
/// the network reports them.
/// </summary>
/// <remarks>
/// <para>
/// The record counterpart of <see cref="TransactionReceipts"/>: a
/// transaction ID can be associated with records in three distinct
/// roles, which the network returns separately and this type keeps
/// separate.  Any of the groups may be empty.
/// </para>
/// <para>
/// <see cref="Children"/> holds the records of work the network
/// performed on the transaction's behalf — for example the account
/// created by a transfer to an alias, whose transfers and fees appear
/// on the child record rather than the parent's.
/// </para>
/// </remarks>
public sealed record TransactionRecords
{
    /// <summary>
    /// The record for the submitted (parent) transaction itself, or
    /// <code>null</code> when the queried node holds no record for the ID.
    /// That does not prove the transaction never executed: nodes keep
    /// records for only about three minutes after consensus.  Settle an
    /// older or unknown transaction on a mirror node by its transaction id.
    /// </summary>
    public TransactionRecord? Parent { get; internal init; }
    /// <summary>
    /// Records for the child transactions the network executed as part
    /// of the parent, in nonce order.  Empty when the transaction
    /// spawned no internal work.
    /// </summary>
    public IReadOnlyList<TransactionRecord> Children { get; internal init; } = [];
    /// <summary>
    /// Records for additional submissions of the same transaction ID.
    /// These are not failures in themselves — the network records every
    /// submission it saw, of which at most one reaches consensus
    /// successfully.  Empty in the ordinary case of a single submission.
    /// </summary>
    public IReadOnlyList<TransactionRecord> Duplicates { get; internal init; } = [];
}
