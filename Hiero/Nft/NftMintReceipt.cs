using Hiero.Implementation;
using Proto;
using System.Collections.Generic;

namespace Hiero;

/// <summary>
/// A transaction receipt containing information regarding
/// new token coin balance, typically returned from methods
/// that can affect a change on the total circulation supply.
/// </summary>
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
    /// The value will be empty if the update
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