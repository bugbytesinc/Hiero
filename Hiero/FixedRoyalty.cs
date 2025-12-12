using Proto;

namespace Hiero;

/// <summary>
/// The definition of a single Fixed Royalty applied to 
/// the transaction as a whole when transferring an NFT or token.
/// </summary>
public sealed record FixedRoyalty : IRoyalty
{
    /// <summary>
    /// Identifies this royalty as a Fixed Royalty type.
    /// </summary>
    public RoyaltyType RoyaltyType => RoyaltyType.Fixed;
    /// <summary>
    /// Address receiving the royalty assessment.
    /// </summary>
    public EntityId Receiver { get; private init; }
    /// <summary>
    /// The address id of the token type used to pay
    /// the royalty, if <code>None</code> then
    /// native hBar crypto is assumed.
    /// </summary>
    public EntityId Token { get; private init; }
    /// <summary>
    /// The amount of token or cryptocurrency
    /// that will be assessed and deducted from
    /// the receiver sending the associated token
    /// or NFT.
    /// </summary>
    public long Amount { get; private init; }
    /// <summary>
    /// Internal Constructor representing the "None" version of a 
    /// fixed royalty.
    /// </summary>
    private FixedRoyalty()
    {
        Receiver = EntityId.None;
        Token = EntityId.None;
        Amount = 0;
    }
    /// <summary>
    /// Public Constructor, a <code>FixedRoyalty</code> is immutable after creation.
    /// </summary>
    /// <param name="receiver">
    /// Address receiving the royalty assessment.
    /// </param>
    /// <param name="token">
    /// The address id of the token type used to pay
    /// the royalty, if <code>None</code> then
    /// native hBar crypto is assumed.
    /// </param>
    /// <param name="amount">
    /// The amount of token or cryptocurrency
    /// that will be assessed and deducted from
    /// the receiver sending the associated token
    /// or NFT.
    /// </param>
    public FixedRoyalty(EntityId receiver, EntityId token, long amount)
    {
        Receiver = receiver;
        Token = token;
        Amount = amount;
    }
    /// <summary>
    /// Internal Helper Constructor converting raw protobuf 
    /// into this royalty definition.
    /// </summary>
    internal FixedRoyalty(CustomFee fee)
    {
        Receiver = fee.FeeCollectorAccountId.AsAddress();
        Token = fee.FixedFee.DenominatingTokenId.AsAddress();
        Amount = fee.FixedFee.Amount;
    }
}