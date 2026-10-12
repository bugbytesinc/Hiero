// SPDX-License-Identifier: Apache-2.0
namespace Hiero;

/// <summary>
/// A transaction receipt containing information regarding
/// new token coin balance, typically returned from methods
/// that can affect a change on the total circulation supply.
/// </summary>
/// <remarks>
/// <para>
/// <b>Best-effort when inferred.</b> Methods that derive a receipt's type from
/// the network's response — <see cref="TransactionReceiptExtensions.GetReceiptAsync"/>
/// and <see cref="TransactionReceiptExtensions.GetAllReceiptsAsync"/> — cannot
/// recognise a supply of exactly zero as a token operation. The network reports
/// <c>newTotalSupply</c> as a presence-less <c>uint64</c> and protobuf omits
/// zero from the wire, so burning or wiping an entire supply is
/// indistinguishable from a transaction that never touched a token. Such a
/// receipt arrives as a plain <see cref="TransactionReceipt"/> instead.
/// </para>
/// <para>
/// No value is lost to this. The substitution happens only when the supply is
/// zero, so a receipt that is <i>not</i> this type implies a circulation of
/// zero. Test rather than cast:
/// </para>
/// <code>
/// ulong circulation = receipt is TokenReceipt token ? token.Circulation : 0;
/// </code>
/// <para>
/// Receipts returned directly by the operation that produced them — such as
/// <c>BurnTokenAsync</c> — are typed explicitly and are always correct.
/// </para>
/// </remarks>
public sealed record TokenReceipt : TransactionReceipt
{
    /// <summary>
    /// The current (new) total supply of the token across all accounts,
    /// in its smallest unit: divide by 10^<see cref="TokenInfo.Decimals"/>
    /// for whole tokens.  For an NFT class, the number of NFTs.
    /// </summary>
    public ulong Circulation { get; internal init; }
    /// <summary>
    /// Internal Constructor of the receipt.
    /// </summary>
    internal TokenReceipt(Proto.TransactionID transactionId, Proto.TransactionReceipt receipt) : base(transactionId, receipt)
    {
        Circulation = receipt.NewTotalSupply;
    }
}