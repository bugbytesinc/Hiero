// SPDX-License-Identifier: Apache-2.0
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
    /// may be spent by the spender.
    /// </summary>
    public EntityId Owner { get; private init; }
    /// <summary>
    /// The account that may spend the allocated
    /// allowance of NFT(s).
    /// </summary>
    public EntityId Spender { get; private init; }
    /// <summary>
    /// Optional. The account, previously granted an
    /// <c>approved_for_all</c> allowance over this NFT
    /// class by the <see cref="Owner"/>, that is granting
    /// this specific-serial sub-allowance to
    /// <see cref="Spender"/> without requiring the owner
    /// to sign. When set, this account must sign the
    /// allowance transaction and <see cref="Owner"/>
    /// must not. Maps to the proto <c>delegating_spender</c>
    /// field on an NFT allowance.
    /// </summary>
    public EntityId DelegatingSpender { get; private init; }
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
    /// <param name="delegatingSpender">
    /// Optional. An account with a pre-existing <c>approved_for_all</c>
    /// allowance over this NFT class, granting this specific-serial
    /// sub-allowance on the owner's behalf.
    /// </param>
    /// <exception cref="ArgumentException">
    /// If any of the addresses are null or empty.
    /// </exception>
    public NftAllowance(EntityId token, EntityId owner, EntityId spender, IReadOnlyList<long>? serialNumbers = null, EntityId? delegatingSpender = null)
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
        if (serialNumbers == null && !delegatingSpender.IsNullOrNone())
        {
            throw new ArgumentException("When specifying a delegating spender, the serial numbers must be specified.", nameof(delegatingSpender));
        }
        Token = token;
        Owner = owner;
        Spender = spender;
        SerialNumbers = serialNumbers;
        DelegatingSpender = delegatingSpender is null ? EntityId.None : delegatingSpender;
    }
    /// <summary>
    /// Represents an allowance allocation permitting a
    /// spender account privileges of spending the specified
    /// NFT instance from the owning account.
    /// </summary>
    /// <remarks>
    /// Convenience constructor for a singular NFT allowance.
    /// </remarks>
    /// <param name="nft">
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
    /// <param name="delegatingSpender">
    /// Optional. An account with a pre-existing <c>approved_for_all</c>
    /// allowance over this NFT class, granting this specific-serial
    /// sub-allowance on the owner's behalf.
    /// </param>
    /// <exception cref="ArgumentException">
    /// If any of the addresses are null or empty.
    /// </exception>
    public NftAllowance(Nft nft, EntityId owner, EntityId spender, EntityId? delegatingSpender = null)
    {
        if (nft is null || Nft.None.Equals(nft))
        {
            throw new ArgumentException("The allowance token cannot be null or empty.", nameof(nft));
        }
        if (owner.IsNullOrNone())
        {
            throw new ArgumentException("The allowance owner account cannot be null or empty.", nameof(owner));
        }
        if (spender.IsNullOrNone())
        {
            throw new ArgumentException("The allowance spender account cannot be null or empty.", nameof(spender));
        }
        Token = nft;
        Owner = owner;
        Spender = spender;
        SerialNumbers = new[] { nft.SerialNumber };
        DelegatingSpender = delegatingSpender is null ? EntityId.None : delegatingSpender;
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
            DelegatingSpender = EntityId.None;
        }
        else
        {
            Token = EntityId.None;
            Owner = EntityId.None;
            Spender = EntityId.None;
            SerialNumbers = [];
            DelegatingSpender = EntityId.None;
        }
    }
}