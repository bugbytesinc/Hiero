namespace Hiero;

/// <summary>
/// Represents a NFT transfer.
/// </summary>
public sealed record NftTransfer
{
    /// <summary>
    /// The Token ID and Serial Number of the nft to transfer.
    /// </summary>
    public Nft Nft { get; private init; }
    /// <summary>
    /// The account sending the NFT.
    /// </summary>
    public EntityId From { get; private init; }
    /// <summary>
    /// The account receiving the NFT.
    /// </summary>
    public EntityId To { get; private init; }
    /// <summary>
    /// Indicates the transfer was authorize thru the
    /// allowance mechanism and the sending account
    /// may not have signed this transaction.
    /// </summary>
    public bool Delegated { get; private init; }
    /// <summary>
    /// Public Constructor, an <code>NftTransfer</code> is immutable after creation.
    /// </summary>
    /// <param name="nft">
    /// The address and serial number of the nft to transfer.
    /// </param>
    /// <param name="fromAddress">
    /// The address of the crypto account having the NFT.
    /// </param>
    /// <param name="toAddress">
    /// The address of the crypto account sending the NFT.
    /// </param>
    /// <param name="delegated">
    /// Indicates the parties involved in the transaction
    /// are acting as delegates thru a granted allowance.
    /// </param>
    public NftTransfer(Nft nft, EntityId fromAddress, EntityId toAddress, bool delegated = false)
    {
        Nft = nft;
        From = fromAddress;
        To = toAddress;
        Delegated = delegated;
    }
}