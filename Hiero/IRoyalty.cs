namespace Hiero;
/// <summary>
/// Represents a royalty (fee or royalty) associated 
/// with transfers of a token or asset.
/// </summary>
public interface IRoyalty
{
    /// <summary>
    /// The account receiving the commision fee.
    /// </summary>
    public EntityId Receiver { get; }
    /// <summary>
    /// The type of royalty this object represents,
    /// will match the concrete implementation type.
    /// </summary>
    public RoyaltyType RoyaltyType { get; }
}