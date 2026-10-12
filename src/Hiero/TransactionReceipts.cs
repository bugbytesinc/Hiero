// SPDX-License-Identifier: Apache-2.0
namespace Hiero;

/// <summary>
/// Every receipt the network holds for one transaction ID, grouped as
/// the network reports them.
/// </summary>
/// <remarks>
/// <para>
/// A transaction ID can be associated with more than one receipt, in
/// three distinct roles.  The network returns them separately and this
/// type preserves that separation, because the groups are not
/// interchangeable and any of them may be empty.
/// </para>
/// <para>
/// <see cref="Children"/> is the group most callers are looking for:
/// when the network performs work on a transaction's behalf — creating
/// an account for a transfer to an alias, or dispatching a contract's
/// calls to a precompile — the results arrive as child receipts, not on
/// the submitted transaction's own receipt.
/// </para>
/// </remarks>
public sealed record TransactionReceipts
{
    /// <summary>
    /// The receipt for the submitted (parent) transaction itself.  When the
    /// node holds no receipt for the ID, the lookup throws a
    /// <see cref="ConsensusException"/>, or, with
    /// <see cref="IConsensusContext.ThrowIfNotSuccess"/> set to
    /// <code>false</code>, this is a stand-in whose status is
    /// <see cref="ResponseCode.ReceiptNotFound"/>.  That does not prove the
    /// transaction never executed: nodes keep receipts for only about three
    /// minutes after consensus.  Settle an older or unknown transaction on a
    /// mirror node by its transaction id.
    /// </summary>
    public TransactionReceipt? Parent { get; internal init; }
    /// <summary>
    /// Receipts for the child transactions the network executed as part
    /// of the parent, in nonce order.  Each carries the parent's
    /// transaction ID with its own nonce.  Empty when the transaction
    /// spawned no internal work.
    /// </summary>
    public IReadOnlyList<TransactionReceipt> Children { get; internal init; } = [];
    /// <summary>
    /// Receipts for additional submissions of the same transaction ID.
    /// These are not failures in themselves — the network records every
    /// submission it saw, of which at most one reaches consensus
    /// successfully.  Empty in the ordinary case of a single submission.
    /// </summary>
    public IReadOnlyList<TransactionReceipt> Duplicates { get; internal init; } = [];
}
