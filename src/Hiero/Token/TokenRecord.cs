// SPDX-License-Identifier: Apache-2.0
namespace Hiero;

/// <summary>
/// A transaction record containing information regarding
/// new token coin balance, typically returned from methods
/// that can affect a change on the total circulation supply.
/// </summary>
/// <remarks>
/// <b>Best-effort when inferred</b>, exactly as for
/// <see cref="TokenReceipt"/>: a supply of exactly zero cannot be recognised as a token operation. Records obtained from
/// <see cref="TransactionRecordExtensions.GetTransactionRecordAsync"/>,
/// <see cref="TransactionRecordExtensions.GetAllTransactionRecordsAsync"/> or
/// <see cref="TransactionRecordExtensions.GetAccountRecordsAsync"/> therefore
/// arrive as a plain <see cref="TransactionRecord"/> in that case. No value is
/// lost — the substitution happens only when the value is zero. Test rather
/// than cast:
/// <code>
/// ulong circulation = record is TokenRecord token ? token.Circulation : 0;
/// </code>
/// </remarks>
public sealed record TokenRecord : TransactionRecord
{
    /// <summary>
    /// The current (new) total supply of the token across all accounts,
    /// in its smallest unit: divide by 10^<see cref="TokenInfo.Decimals"/>
    /// for whole tokens.  For an NFT class, the number of NFTs.
    /// </summary>
    public ulong Circulation { get; internal init; }
    /// <summary>
    /// Internal Constructor of the record.
    /// </summary>
    internal TokenRecord(Proto.TransactionRecord record) : base(record)
    {
        Circulation = record.Receipt.NewTotalSupply;
    }
}