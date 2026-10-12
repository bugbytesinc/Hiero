// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;

namespace Hiero;
/// <summary>
/// A transaction record containing information regarding
/// the newly minted NFTs, typically returned from methods
/// that can affect a change on the total NFT circulation supply.
/// </summary>
/// <remarks>
/// <b>Best-effort when inferred</b>, exactly as for
/// <see cref="NftMintReceipt"/>: a response with no serial numbers and a zero supply cannot be recognised as a mint. Records obtained from
/// <see cref="TransactionRecordExtensions.GetTransactionRecordAsync"/>,
/// <see cref="TransactionRecordExtensions.GetAllTransactionRecordsAsync"/> or
/// <see cref="TransactionRecordExtensions.GetAccountRecordsAsync"/> therefore
/// arrive as a plain <see cref="TransactionRecord"/> in that case. No value is
/// lost — the substitution happens only when the value is zero. Test rather
/// than cast:
/// <code>
/// var serials = record is NftMintRecord mint ? mint.SerialNumbers : [];
/// </code>
/// </remarks>
public sealed record NftMintRecord : TransactionRecord
{
    /// <summary>
    /// The current (new) total number of NFTs.
    /// </summary>
    public ulong Circulation { get; internal init; }
    /// <summary>
    /// The serial numbers of the newly created
    /// assets, related in order to the list of
    /// metadata sent to the mint method.
    /// </summary>
    public IReadOnlyList<long> SerialNumbers { get; internal init; }
    /// <summary>
    /// Internal Constructor of the record.
    /// </summary>
    internal NftMintRecord(Proto.TransactionRecord record) : base(record)
    {
        Circulation = record.Receipt.NewTotalSupply;
        SerialNumbers = record.Receipt.SerialNumbers.CopyToReadOnlyList();
    }
}