using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Numerics;

namespace Hiero;
/// <summary>
/// The information returned from the GetTokenInfo ConsensusClient 
/// method call.  It represents the details concerning 
/// Tokens and Assets.
/// </summary>
public sealed record TokenInfo
{
    /// <summary>
    /// The Hedera address of this token.
    /// </summary>
    public EntityId Token { get; private init; }
    /// <summary>
    /// The type of token this represents, fungible
    /// token or Non-Fungible token (NFT).
    /// </summary>
    public TokenType Type { get; private init; }
    /// <summary>
    /// The string symbol representing this token.
    /// </summary>
    public string Symbol { get; private init; }
    /// <summary>
    /// Name of this token
    /// </summary>
    public string Name { get; private init; }
    /// <summary>
    /// The treasury account holding uncirculated tokens.
    /// </summary>
    public EntityId Treasury { get; private init; }
    /// <summary>
    /// The total balance of tokens in all accounts (the whole denomination).
    /// </summary>
    public ulong Circulation { get; private init; }
    /// <summary>
    /// The number of decimal places which each token may be subdivided.
    /// </summary>
    public uint Decimals { get; private init; }
    /// <summary>
    /// The maximum number of tokens allowed in circulation at a given time.
    /// The value of 0 is unbounded.
    /// </summary>
    public long Ceiling { get; private init; }
    /// <summary>
    /// Administrator key for signing transactions modifying this token's properties.
    /// </summary>
    public Endorsement? Administrator { get; private init; }
    /// <summary>
    /// Administrator key for signing transactions updating the grant or revoke 
    /// KYC status of an account.
    /// </summary>
    public Endorsement? GrantKycEndorsement { get; private init; }
    /// <summary>
    /// Administrator key for signing transactions for freezing or unfreezing an 
    /// account's ability to transfer tokens.
    /// </summary>
    public Endorsement? SuspendEndorsement { get; private init; }
    /// <summary>
    /// Administrator key for signing transactions that can pause or continue
    /// the exchange of all assets across all accounts on the network.
    /// </summary>
    public Endorsement? PauseEndorsement { get; set; }
    /// <summary>
    /// Administrator key for signing transaction that completely remove tokens
    /// from an crypto address.
    /// </summary>
    public Endorsement? ConfiscateEndorsement { get; private init; }
    /// <summary>
    /// Administrator key for signing transactions for minting or unminting 
    /// tokens in the treasury account.
    /// </summary>
    public Endorsement? SupplyEndorsement { get; private init; }
    /// <summary>
    /// Administrator key for signing transactions for adjusting metadata of
    /// tokens or assets
    /// </summary>
    public Endorsement? MetadataEndorsement { get; private init; }
    /// <summary>
    /// Administrator key for signing transactions updating the royalties
    /// (custom transfer fees) associated with this token.
    /// </summary>
    public Endorsement? RoyaltiesEndorsement { get; set; }
    /// <summary>
    /// The current default suspended/frozen status of the token.
    /// </summary>
    public TokenTradableStatus TradableStatus { get; private init; }
    /// <summary>
    /// The current default KYC status of the token.
    /// </summary>
    public TokenKycStatus KycStatus { get; private init; }
    /// <summary>
    /// The current paused/frozen status of the token for all accounts.
    /// </summary>
    public TokenTradableStatus PauseStatus { get; private init; }
    /// <summary>
    /// The list of fixed royalties assessed on transactions
    /// by the network when transferring this token.
    /// </summary>
    public IReadOnlyList<IRoyalty> Royalties { get; internal init; }
    /// <summary>
    /// Expiration date for the token.  Will renew as determined by the
    /// renew period and balance of auto renew account.
    /// </summary>
    public ConsensusTimeStamp Expiration { get; private init; }
    /// <summary>
    /// Interval of the token and auto-renewal period. If
    /// the associated renewal account does not have sufficient funds to 
    /// renew at the expiration time, it will be renewed for a period 
    /// of time the remaining funds can support.  If no funds remain, the
    /// token instance will be deleted.
    /// </summary>
    public TimeSpan? RenewPeriod { get; private init; }
    /// <summary>
    /// Optional address of the account supporting the auto renewal of 
    /// the token at expiration time.  The token lifetime will be
    /// extended by the RenewPeriod at expiration time if this account
    /// contains sufficient funds.  The private key associated with
    /// this account must sign the transaction if RenewAccount is
    /// specified.
    /// </summary>
    public EntityId? RenewAccount { get; private init; }
    /// <summary>
    /// Flag indicating the token has been deleted.
    /// </summary>
    public bool Deleted { get; private init; }
    /// <summary>
    /// The memo associated with the token instance.
    /// </summary>
    public string Memo { get; private init; }
    /// <summary>
    /// Identification of the Ledger (Network) 
    /// this token information was retrieved from.
    /// </summary>
    public BigInteger Ledger { get; private init; }
    internal TokenInfo(Response response)
    {
        var info = response.TokenGetInfo.TokenInfo;
        Token = info.TokenId.AsAddress();
        Type = (TokenType)info.TokenType;
        Symbol = info.Symbol;
        Name = info.Name;
        Treasury = info.Treasury.AsAddress();
        Circulation = info.TotalSupply;
        Decimals = info.Decimals;
        Ceiling = info.MaxSupply;
        Administrator = info.AdminKey?.ToEndorsement();
        GrantKycEndorsement = info.KycKey?.ToEndorsement();
        SuspendEndorsement = info.FreezeKey?.ToEndorsement();
        PauseEndorsement = info.PauseKey?.ToEndorsement();
        ConfiscateEndorsement = info.WipeKey?.ToEndorsement();
        SupplyEndorsement = info.SupplyKey?.ToEndorsement();
        MetadataEndorsement = info.MetadataKey?.ToEndorsement();
        RoyaltiesEndorsement = info.FeeScheduleKey?.ToEndorsement();
        if (info.CustomFees.Count == 0)
        {
            Royalties = [];
        }
        else
        {
            var list = new List<IRoyalty>(info.CustomFees.Count);
            foreach (var fee in info.CustomFees)
            {
                list.Add(fee.FeeCase switch
                {
                    CustomFee.FeeOneofCase.RoyaltyFee => new NftRoyalty(fee),
                    CustomFee.FeeOneofCase.FractionalFee => new TokenRoyalty(fee),
                    CustomFee.FeeOneofCase.FixedFee => new FixedRoyalty(fee),
                    // Should not get here?, if its invalid info, what do we do?
                    _ => new FixedRoyalty(EntityId.None, EntityId.None, 0),
                });
            }
            Royalties = list;
        }
        TradableStatus = (TokenTradableStatus)info.DefaultFreezeStatus;
        PauseStatus = (TokenTradableStatus)info.PauseStatus;
        KycStatus = (TokenKycStatus)info.DefaultKycStatus;
        Expiration = info.Expiry.ToConsensusTimeStamp();
        RenewPeriod = info.AutoRenewPeriod?.ToTimeSpan();
        RenewAccount = info.AutoRenewAccount?.AsAddress();
        Deleted = info.Deleted;
        Memo = info.Memo;
        Ledger = new BigInteger(info.LedgerId.Span, true, true);
    }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TokenInfoExtensions
{
    /// <summary>
    /// Retrieves detailed information regarding a Token.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client returning the information.
    /// </param>
    /// <param name="token">
    /// The identifier (Payer/Symbol) of the token to retrieve.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A detailed description of the token instance.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    public static async Task<TokenInfo> GetTokenInfoAsync(this ConsensusClient client, EntityId token, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        return new TokenInfo(await Engine.QueryAsync(client, new TokenGetInfoQuery { Token = new TokenID(token) }, cancellationToken, configure).ConfigureAwait(false));
    }
}