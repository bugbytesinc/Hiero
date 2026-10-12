// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;

namespace Hiero;

/// <summary>
/// A transaction receipt containing information regarding
/// the newly minted NFTs, typically returned from methods
/// that can affect a change on the total NFT circulation supply.
/// </summary>
/// <remarks>
/// <para>
/// <b>Best-effort when inferred.</b> Methods that derive a receipt's type from
/// the network's response — <see cref="TransactionReceiptExtensions.GetReceiptAsync"/>
/// and <see cref="TransactionReceiptExtensions.GetAllReceiptsAsync"/> — select
/// this type when the response carries serial numbers, falling back to a
/// circulation count. A response carrying neither (no serial numbers and a
/// zero supply) is indistinguishable from a transaction that minted nothing,
/// and arrives as a plain <see cref="TransactionReceipt"/> instead.
/// </para>
/// <para>
/// No value is lost to this: the substitution happens only when there are no
/// serial numbers to report and the circulation is zero. Test rather than
/// cast:
/// </para>
/// <code>
/// var serials = receipt is NftMintReceipt mint ? mint.SerialNumbers : [];
/// </code>
/// <para>
/// Receipts returned directly by the operation that produced them — such as
/// <c>MintNftAsync</c> — are typed explicitly and are always correct.
/// </para>
/// </remarks>
public sealed record NftMintReceipt : TransactionReceipt
{
    /// <summary>
    /// The current (new) total number of NFTs.
    /// </summary>
    public ulong Circulation { get; internal init; }
    /// <summary>
    /// The serial numbers of the newly created
    /// NFTs, related in order to the list of
    /// metadata sent to the mint method.
    /// </summary>
    /// <remarks>
    /// The value will be empty if the mint
    /// was scheduled as a pending transaction.
    /// </remarks>
    public IReadOnlyList<long> SerialNumbers { get; internal init; }
    /// <summary>
    /// Internal Constructor of the receipt.
    /// </summary>
    internal NftMintReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt) : base(transactionId, receipt)
    {
        Circulation = receipt.NewTotalSupply;
        SerialNumbers = receipt.SerialNumbers.CopyToReadOnlyList();
    }
}