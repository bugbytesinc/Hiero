using Proto;

namespace Hiero;
/// <summary>
/// The token balance information associated with an account,
/// including the amount of coins held, KYC status and Freeze status.
/// </summary>
public sealed record TokenBalance : CryptoBalance
{
    /// <summary>
    /// ID of the Token or NFT class.
    /// </summary>
    public EntityId Token { get; private init; }
    /// <summary>
    /// The string symbol representing this token.
    /// </summary>
    public string Symbol { get; private init; }
    /// <summary>
    /// The KYC status of the token for this account.
    /// </summary>
    public TokenKycStatus KycStatus { get; private init; }
    /// <summary>
    /// The Frozen status of the token for this account.
    /// </summary>
    public TokenTradableStatus TradableStatus { get; private init; }
    /// <summary>
    /// True if this token was associated automatically by
    /// the network via autoassociaiton via becomming a
    /// token or assset treasury.
    /// </summary>
    public bool AutoAssociated { get; private init; }
    /// <summary>
    /// Internal Helper Function to create a token balance
    /// from raw protobuf response.
    /// </summary>
    internal TokenBalance(TokenRelationship entry)
    {
        Balance = entry.Balance;
        Decimals = entry.Decimals;
        Token = entry.TokenId.AsAddress();
        Symbol = entry.Symbol;
        KycStatus = (TokenKycStatus)entry.KycStatus;
        TradableStatus = (TokenTradableStatus)entry.FreezeStatus;
        AutoAssociated = entry.AutomaticAssociation;
    }
}