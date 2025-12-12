using System.ComponentModel;

namespace Hiero;

/// <summary>
/// The known types of Royalties applied to token and
/// asset transfers.
/// </summary>
public enum RoyaltyType
{
    /// <summary>
    /// A single Fixed Royalty applied to the transaction as
    /// a whole when transferring an asset or token.
    /// </summary>
    /// <remarks>
    /// Applies to both Fungible Tokens and Assets (NFTs).
    /// </remarks>
    [Description("Fixed Royalty Fee")] Fixed = 0,
    /// <summary>
    /// A Royalty computed from value given in exchange for
    /// receiving an Nft (NFT).
    /// </summary>
    /// <remarks>
    /// Only applies to assets (NFTs).
    /// </remarks>
    [Description("NFT Royalty Fee")] Nft = 1,
    /// <summary>
    /// A Royalty computed from the amount of Fungible token
    /// exchanged, can be in the form as a deduction of the
    /// token exchanged, or an excise amount taken from the 
    /// sender of the fungible token.
    /// </summary>
    /// <remarks>
    /// Only applies to Fungible Tokens.
    /// </remarks>
    [Description("Fractional Royalty Fee")] Token = 2,
}