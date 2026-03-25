// SPDX-License-Identifier: Apache-2.0
namespace Hiero;
/// <summary>
/// Identifies a pending airdrop by its sender, receiver, and token.
/// </summary>
/// <remarks>
/// A pending airdrop is created when an airdrop transaction targets an account
/// that cannot immediately receive the token (e.g., no auto-association slots
/// available, or receiver signature required). It must be either claimed by the
/// receiver or cancelled by the sender.
/// </remarks>
public sealed record Airdrop
{
    /// <summary>
    /// The account that initiated and will fund this pending airdrop
    /// (the payer of the airdrop transfer transaction).
    /// </summary>
    public EntityId Sender { get; init; }
    /// <summary>
    /// The account that will receive the tokens if the airdrop is claimed.
    /// </summary>
    public EntityId Receiver { get; init; }
    /// <summary>
    /// The fungible token type being airdropped, or <c>null</c> if this is an NFT airdrop.
    /// </summary>
    public EntityId? Token { get; init; }
    /// <summary>
    /// The specific NFT instance being airdropped, or <c>null</c> if this is a fungible token airdrop.
    /// </summary>
    public Nft? Nft { get; init; }
    /// <summary>
    /// Creates a pending airdrop identifier for a fungible token.
    /// </summary>
    /// <param name="sender">The account sending the airdrop.</param>
    /// <param name="receiver">The account receiving the airdrop.</param>
    /// <param name="fungibleToken">The fungible token type being airdropped.</param>
    public Airdrop(EntityId sender, EntityId receiver, EntityId fungibleToken)
    {
        Sender = sender;
        Receiver = receiver;
        Token = fungibleToken;
    }
    /// <summary>
    /// Creates a pending airdrop identifier for a non-fungible token.
    /// </summary>
    /// <param name="sender">The account sending the airdrop.</param>
    /// <param name="receiver">The account receiving the airdrop.</param>
    /// <param name="nft">The specific NFT instance being airdropped.</param>
    public Airdrop(EntityId sender, EntityId receiver, Nft nft)
    {
        Sender = sender;
        Receiver = receiver;
        Nft = nft;
    }
}
