using Hiero.Implementation;

namespace Hiero;
/// <summary>
/// A transaction record containing information regarding
/// new token coin balance, typically returned from methods
/// that can affect a change on the total circulation supply.
/// </summary>
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