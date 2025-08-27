﻿using Proto;

namespace Hiero;

/// <summary>
/// Represents a Royalty type computed from value 
/// given in exchange for receiving an NFT.
/// </summary>
public sealed record NftRoyalty : IRoyalty
{
    /// <summary>
    /// Identifies this royalty as an Nft Royalty type.
    /// </summary>
    public RoyaltyType RoyaltyType => RoyaltyType.Nft;
    /// <summary>
    /// Holder receiving the royalty assessment.
    /// </summary>
    public EntityId Receiver { get; private init; }
    /// <summary>
    /// The numerator portion of the assement fraction
    /// of the value exchanged in return for the NFT.
    /// </summary>
    /// <remarks>
    /// This is not expressed as a floating point number
    /// in order to avoid rounding fees inheret in 
    /// computing platforms.
    /// </remarks>
    public long Numerator { get; private init; }
    /// <summary>
    /// The denominator portion of the assement fraction
    /// of the value exchanged in return for the NFT.
    /// </summary>
    /// <remarks>
    /// This is not expressed as a floating point number
    /// in order to avoid rounding fees inheret in 
    /// computing platforms.
    /// </remarks>
    public long Denominator { get; private init; }
    /// <summary>
    /// The fixed amount of token or cryptocurrency
    /// that will be assessed from the receiver
    /// receiving the associated token(s) if the 
    /// transaction transfering the token provides
    /// no other discernable exchange of value in
    /// payment.
    /// </summary>
    /// <remarks>Set to <code>0</code> if no
    /// fallback amount is required.</remarks>
    public long FallbackAmount { get; private init; }
    /// <summary>
    /// The address id of the token type used to pay
    /// the royalty if no other transfer value exists
    /// in payment for the tranfer of the associated token
    /// or asset, if set to <code>None</code> then
    /// native hBar crypto is assumed.
    /// </summary>
    public EntityId FallbackToken { get; private init; }
    /// <summary>
    /// Public Constructor, an <code>NftRoyalty</code> is immutable after creation.
    /// </summary>
    /// <param name="receiver">
    /// Address receiving the royalty assessment.
    /// </param>
    /// <param name="numerator">
    /// The denominator portion of the assement fraction
    /// of the value exchanged in return for the NFT.
    /// </param>
    /// <param name="denominator">
    /// The denominator portion of the assement fraction
    /// of the value exchanged in return for the NFT.
    /// </param>
    /// <param name="fallbackAmount">
    /// The fixed amount of token or cryptocurrency
    /// that will be assessed from the receiver
    /// receiving the associated token(s) if the 
    /// transaction transfering the token provides
    /// no other discernable exchange of value in
    /// payment.
    /// </param>
    /// <param name="fallbackToken">
    /// The address id of the token type used to pay
    /// the royalty if no other transfer value exists
    /// in payment for the tranfer of the associated token
    /// or asset, if set to <code>None</code> then
    /// native hBar crypto is assumed.
    /// </param>
    public NftRoyalty(EntityId receiver, long numerator, long denominator, long fallbackAmount, EntityId fallbackToken)
    {
        Receiver = receiver;
        Numerator = numerator;
        Denominator = denominator;
        FallbackAmount = fallbackAmount;
        FallbackToken = fallbackToken;
    }
    /// <summary>
    /// Internal Helper Constructor converting raw protobuf 
    /// into this royalty definition.
    /// </summary>
    internal NftRoyalty(CustomFee fee)
    {
        Receiver = fee.FeeCollectorAccountId.AsAddress();
        var royalty = fee.RoyaltyFee;
        Numerator = royalty.ExchangeValueFraction.Numerator;
        Denominator = royalty.ExchangeValueFraction.Denominator;
        var fallback = royalty.FallbackFee;
        if (fallback is null)
        {
            FallbackToken = EntityId.None;
            FallbackAmount = 0;
        }
        else
        {
            FallbackAmount = fallback.Amount;
            FallbackToken = fallback.DenominatingTokenId.AsAddress();
        }
    }
}