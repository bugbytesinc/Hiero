using Proto;

namespace Hiero;
/// <summary>
/// Represents an allowance allocation permitting a
/// spender account privileges of spending the specified
/// NFT(s) from the owning account.
/// </summary>
public sealed record NftAllowance
{
    /// <summary>
    /// The address of the NFT's token definition
    /// having the allocated allowance.
    /// </summary>
    public EntityId Token { get; private init; }
    /// <summary>
    /// The account holding the NFT(s) that
    /// may be spent by the delegate spender.
    /// </summary>
    public EntityId Owner { get; private init; }
    /// <summary>
    /// The account that may spend the allocated
    /// allowance of NFT(s).
    /// </summary>
    public EntityId Spender { get; private init; }
    /// <summary>
    /// An account, approved by the owner, that 
    /// is given access to all of the Owner's 
    /// NFTs of this class, and can therefore
    /// in turn allocate a specific NFT instance
    /// to a 3rd party to sell on behalf of the
    /// original owner.
    /// </summary>
    public EntityId OwnersDelegate { get; private init; }
    /// <summary>
    /// The explicit list of serial numbers that
    /// can be spent by the delegate.  If the value
    /// is <code>null</code> then all assets of the
    /// token class may be spent.  If the list is 
    /// empty, it means all of the identified assets
    /// with specific serial numbers have already been
    /// removed from the account.
    /// </summary>
    public IReadOnlyList<long>? SerialNumbers { get; private init; }
    /// <summary>
    /// Represents an allowance allocation permitting a
    /// spender account privileges of spending the specified
    /// amount assets from the owning account.
    /// </summary>
    /// <param name="token">
    /// The address of the NFT's token definition
    /// having the allocated allowance.
    /// </param>
    /// <param name="owner">
    /// The Address owner holding the NFT(s) that
    /// may be spent by the spender.
    /// </param>
    /// <param name="spender">
    /// The account that may spend the allocated
    /// allowance of NFT(s).
    /// </param>
    /// <param name="serialNumbers">
    /// The explicit list of serial numbers that
    /// can be spent by the spender.  If the value
    /// is <code>null</code> then all NFTs of the
    /// token class may be spent.
    /// </param>
    /// <exception cref="ArgumentException">
    /// If any of the addresses are null or empty.
    /// </exception>
    public NftAllowance(EntityId token, EntityId owner, EntityId spender, IReadOnlyList<long>? serialNumbers = null, EntityId? ownersDelegate = null)
    {
        if (token.IsNullOrNone())
        {
            throw new ArgumentException("The allowance token cannot be null or empty.", nameof(token));
        }
        if (owner.IsNullOrNone())
        {
            throw new ArgumentException("The allowance owner account cannot be null or empty.", nameof(owner));
        }
        if (spender.IsNullOrNone())
        {
            throw new ArgumentException("The allowance spender account cannot be null or empty.", nameof(spender));
        }
        if (serialNumbers == null && !ownersDelegate.IsNullOrNone())
        {
            throw new ArgumentException("When specifying a delegating account controlling NFTs for an owner, the serial numbers must be specified.", nameof(ownersDelegate));
        }
        Token = token;
        Owner = owner;
        Spender = spender;
        SerialNumbers = serialNumbers;
        OwnersDelegate = ownersDelegate is null ? EntityId.None : ownersDelegate;
    }
    /// <summary>
    /// Represents an allowance allocation permitting a
    /// spender account privileges of spending the specified
    /// NFT instance from the owning account.
    /// </summary>
    /// <remarks>
    /// Convenience constructor for a singular NFT allowance.
    /// </remarks>
    /// <param name="asset">
    /// Single NFT instance to grant the allowance.
    /// </param>
    /// <param name="owner">
    /// The Address owner holding the NFT that
    /// may be spent by the spender.
    /// </param>
    /// <param name="spender">
    /// The account that may spend the allocated
    /// allowance of NFT.
    /// </param>
    /// <exception cref="ArgumentException">
    /// If any of the addresses are null or empty.
    /// </exception>
    public NftAllowance(Nft asset, EntityId owner, EntityId spender, EntityId? ownersDelegate = null)
    {
        if (asset is null || Nft.None.Equals(asset))
        {
            throw new ArgumentException("The allowance token cannot be null or empty.", nameof(asset));
        }
        if (owner.IsNullOrNone())
        {
            throw new ArgumentException("The allowance owner account cannot be null or empty.", nameof(owner));
        }
        if (spender.IsNullOrNone())
        {
            throw new ArgumentException("The allowance spender account cannot be null or empty.", nameof(spender));
        }
        Token = asset;
        Owner = owner;
        Spender = spender;
        SerialNumbers = new[] { asset.SerialNumber };
        OwnersDelegate = ownersDelegate is null ? EntityId.None : ownersDelegate;
    }
    /// <summary>
    /// Internal helper constructor for creating the
    /// allowance from protobuf object.
    /// </summary>
    internal NftAllowance(GrantedNftAllowance allowance, EntityId owner)
    {
        if (allowance is not null)
        {
            Token = allowance.TokenId.AsAddress();
            Owner = owner;
            Spender = allowance.Spender.AsAddress();
            SerialNumbers = [];
            OwnersDelegate = EntityId.None;
        }
        else
        {
            Token = EntityId.None;
            Owner = EntityId.None;
            Spender = EntityId.None;
            SerialNumbers = [];
            OwnersDelegate = EntityId.None;
        }
    }
}