using Hiero.Implementation;
using Proto;
using System;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Hiero;
/// <summary>
/// The information returned from the GetNftInfo ConsensusClient 
/// method call.  It represents the details concerning an 
/// Hedera Non-Fungable Token (NFT).
/// </summary>
public sealed record NftInfo
{
    /// <summary>
    /// The identifer of the NFT Instance.
    /// </summary>
    public Nft Nft { get; private init; }
    /// <summary>
    /// The account currently owning the NFT.
    /// </summary>
    public EntityId Owner { get; private init; }
    /// <summary>
    /// The account that has the rights to spend
    /// this asset via an allowance grant.
    /// </summary>
    public EntityId Spender { get; private init; }
    /// <summary>
    /// The Consensus Timestamp for when this NFT was created (minted).
    /// </summary>
    public ConsensusTimeStamp Created { get; private init; }
    /// <summary>
    /// The metadata associated with this NFT, limited to 100 bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Metadata { get; private init; }
    /// <summary>
    /// Identification of the Ledger (Network) this 
    /// asset information was retrieved from.
    /// </summary>
    public BigInteger Ledger { get; private init; }
    /// <summary>
    /// Equality implementation
    /// </summary>
    /// <param name="other">
    /// The other <code>NftInfo</code> object to compare.
    /// </param>
    /// <returns>
    /// True if asset, owner, created and metadata are the same.
    /// </returns>
    public bool Equals(NftInfo? other)
    {
        return other is not null &&
            Nft.Equals(other.Nft) &&
            Owner.Equals(other.Owner) &&
            Created.Equals(other.Created) &&
            Metadata.Span.SequenceEqual(other.Metadata.Span) &&
            Ledger.Equals(other.Ledger) &&
            Spender.Equals(other.Spender);
    }
    /// <summary>
    /// Equality implementation.
    /// </summary>
    /// <returns>
    /// A unique hash of the contents of this <code>NftInfo</code> 
    /// object.  Only consistent within the current instance of 
    /// the application process.
    /// </returns>
    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(typeof(NftInfo));
        hash.Add(Nft);
        hash.Add(Owner);
        hash.Add(Created);
        hash.Add(Spender);
        foreach (var b in Metadata.Span) hash.Add(b);
        hash.Add(Ledger);
        return hash.ToHashCode();
    }
    internal NftInfo(Proto.Response response)
    {
        var info = response.TokenGetNftInfo.Nft;
        Nft = NftIDExtensions.AsNft(info.NftID);
        Owner = AccountIDExtensions.AsAddress(info.AccountID);
        Created = info.CreationTime.ToConsensusTimeStamp();
        Metadata = info.Metadata.ToByteArray();
        Ledger = new BigInteger(info.LedgerId.Span, true, true);
        Spender = AccountIDExtensions.AsAddress(info.SpenderId);
    }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class NftInfoExtensions
{
    /// <summary>
    /// Retrieves detailed information regarding a particular Non-Fungible Token (NFT) instance.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="nft">
    /// The identifier (Token and Serial Number) of the nft.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A description of the nft instance, including metadata, created date and current owning account.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    public static async Task<NftInfo> GetNftInfoAsync(this ConsensusClient client, Nft nft, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        return new NftInfo(await client.ExecuteQueryAsync(new Proto.TokenGetNftInfoQuery { NftID = new Proto.NftID(nft) }, cancellationToken, configure).ConfigureAwait(false));
    }
}