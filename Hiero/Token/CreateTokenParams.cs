using Hiero.Implementation;
using Proto;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Hiero;
/// <summary>
/// Token Creation Parameters.
/// </summary>
/// <remarks>
/// The specified Treasury Address is receiving the initial supply of tokens as-well 
/// as the tokens from Token Mint operations when executed.  The balance of the treasury 
/// account is decreased when the Token Burn operation is executed.
/// 
/// The supply that is going to be put in circulation is going to be <code>S*(10^D)</code>,
/// where <code>S</code> is initial supply and <code>D</code> is Decimals. The maximum supply 
/// a token can have is <code>S* (10^D) &lt; 2^63</code>.
/// 
/// The token can be created as immutable if the <code>Administrator</code> endorsement is omitted
/// or set to <code>None</code>.  In this case, the name, symbol, treasury, management keys, Expiration
/// and renew properties cannot be updated. If a token is created as immutable, any account is able to 
/// extend the expiration time by paying the fee.
/// </remarks>
public sealed class CreateTokenParams : TransactionParams, INetworkParams
{
    /// <summary>
    /// Name of the token, not required to be globally unique.
    /// </summary>
    public string Name { get; set; } = default!;
    /// <summary>
    /// Symbol of the token, not required to be globally unique.
    /// </summary>
    public string Symbol { get; set; } = default!;
    /// <summary>
    /// The initial number of tokens to placed into the token treasury
    /// account upon creation of the token (specified in the smallest 
    /// unit). The Treasury receivie the initial circulation.
    /// </summary>
    public ulong Circulation { get; set; }
    /// <summary>
    /// The number of decimal places token may be subdivided.
    /// </summary>
    public uint Decimals { get; set; }
    /// <summary>
    /// The maximum number of tokens allowed to be in circulation at any
    /// given time. If set to a value of zero or less, the toal circulation
    /// will be allowed to grow to the maxumin amount allowed by the network.
    /// </summary>
    public long Ceiling { get; set; }
    /// <summary>
    /// The treasury account receiving the Initial Circulation balance of tokens.
    /// </summary>
    public EntityId Treasury { get; set; } = default!;
    /// <summary>
    /// Administrator key for signing transactions modifying this token's properties.
    /// </summary>
    public Endorsement? Administrator { get; set; }
    /// <summary>
    /// Administrator key for signing transactions updating the grant or revoke 
    /// KYC status of an account.
    /// </summary>
    public Endorsement? GrantKycEndorsement { get; set; }
    /// <summary>
    /// Administrator key for signing transactions for freezing or unfreezing an 
    /// account's ability to transfer tokens.
    /// </summary>
    public Endorsement? SuspendEndorsement { get; set; }
    /// <summary>
    /// Administrator key for signing transactions that can pasue or continue
    /// the exchange of all tokens across all accounts on the network.
    /// </summary>
    public Endorsement? PauseEndorsement { get; set; }
    /// <summary>
    /// Administrator key for signing transaction that completely remove tokens
    /// from an crypto address.
    /// </summary>
    public Endorsement? ConfiscateEndorsement { get; set; }
    /// <summary>
    /// Administrator key for signing transactions for minting or unminting 
    /// tokens in the treasury account.
    /// </summary>
    public Endorsement? SupplyEndorsement { get; set; }
    /// <summary>
    /// Administrator key for changing metadata associated with Tokens 
    /// and Assets.
    /// </summary>
    public Endorsement? MetadataEndorsement { get; set; }
    /// <summary>
    /// Administrator key for signing transactions updating the royalty
    /// (custom transfer fees) associated with this token.
    /// </summary>
    public Endorsement? RoyaltiesEndorsement { get; set; }
    /// <summary>
    /// The list of royalties applied to transactions
    /// transferring this token.  If a royalty endorsement is not
    /// supplied upon creation, the royalties are imutable after
    /// creation.
    /// </summary>
    public IReadOnlyList<IRoyalty>? Royalties { get; set; }
    /// <summary>
    /// The default frozen setting for current and newly created accounts.  A value 
    /// of <code>true</code> will default crypto account status of <code>Frozen</code> 
    /// with relationship to this token.  A value of <code>false</code> will default 
    /// to an tradable/unfrozen relationship.
    /// </summary>
    public bool InitializeSuspended { get; set; }
    /// <summary>
    /// Original expiration date for the token, fees will be charged as appropriate.
    /// </summary>
    public ConsensusTimeStamp Expiration { get; set; }
    /// <summary>
    /// Interval of the topic and auto-renewal period. If
    /// the associated renewal account does not have sufficient funds to 
    /// renew at the expiration time, it will be renewed for a period 
    /// of time the remaining funds can support.  If no funds remain, the
    /// topic instance will be deleted.
    /// </summary>
    public TimeSpan? RenewPeriod { get; set; }
    /// <summary>
    /// Optional address of the account supporting the auto renewal of 
    /// the token at expiration time.  The topic lifetime will be
    /// extended by the RenewPeriod at expiration time if this account
    /// contains sufficient funds.  The private key associated with
    /// this account must sign the transaction if RenewAccount is
    /// specified.
    /// </summary>
    /// <remarks>
    /// If specified, an Administrator Endorsement must also be specified.
    /// </remarks>
    public EntityId? RenewAccount { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to create to this token.  Typically matches the
    /// Administrator, KycEndorsement, FreezeEndorsement and other
    /// listed endorsements associated with this token.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Additional Short description of the token, not checked for uniqueness.
    /// </summary>
    public string Memo { get; set; } = default!;
    /// <summary>
    /// Optional Cancellation token that interrupt the token
    /// submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }

    INetworkTransaction INetworkParams.CreateNetworkTransaction()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ArgumentOutOfRangeException(nameof(Name), "The name cannot be null or empty.");
        }
        if (Name.Length > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(Name), "The token name cannot exceed 100 characters in length.");
        }
        if (string.IsNullOrWhiteSpace(Symbol))
        {
            throw new ArgumentOutOfRangeException(nameof(Symbol), "The token symbol must be specified.");
        }
        if (Symbol.Trim().Length != Symbol.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(Symbol), "The token symbol cannot contain leading or trailing white space.");
        }
        if (Symbol.Length > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(Symbol), "The token symbol cannot exceed 32 characters in length.");
        }
        if (Circulation < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Circulation), "The initial circulation of tokens must be greater than zero.");
        }
        if (Decimals < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Decimals), "The divisibility of tokens cannot be negative.");
        }
        if (Treasury is null || Treasury == Hiero.EntityId.None)
        {
            throw new ArgumentOutOfRangeException(nameof(Treasury), "The treasury must be specified.");
        }
        if (Expiration < ConsensusTimeStamp.Now)
        {
            throw new ArgumentOutOfRangeException(nameof(Expiration), "The expiration time must be in the future.");
        }
        if (RenewAccount.IsNullOrNone() == RenewPeriod.HasValue)
        {
            throw new ArgumentOutOfRangeException(nameof(RenewPeriod), "Both the renew account and period must be specified, or not at all.");
        }
        if (!string.IsNullOrEmpty(Memo))
        {
            if (Memo.Trim().Length != Memo.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(Memo), "The token memo cannot contain leading or trailing white space.");
            }
        }
        var result = new TokenCreateTransactionBody
        {
            Name = Name,
            Symbol = Symbol,
            InitialSupply = Circulation,
            Decimals = Decimals,
            Treasury = new AccountID(Treasury),
            TokenType = Proto.TokenType.FungibleCommon
        };
        if (Ceiling > 0 && Ceiling < long.MaxValue)
        {
            result.MaxSupply = Ceiling;
            result.SupplyType = TokenSupplyType.Finite;
        }
        else
        {
            result.SupplyType = TokenSupplyType.Infinite;
        }
        if (!Administrator.IsNullOrNone())
        {
            result.AdminKey = new Key(Administrator);
        }
        if (!GrantKycEndorsement.IsNullOrNone())
        {
            result.KycKey = new Key(GrantKycEndorsement);
        }
        if (!SuspendEndorsement.IsNullOrNone())
        {
            result.FreezeKey = new Key(SuspendEndorsement);
        }
        if (!PauseEndorsement.IsNullOrNone())
        {
            result.PauseKey = new Key(PauseEndorsement);
        }
        if (!ConfiscateEndorsement.IsNullOrNone())
        {
            result.WipeKey = new Key(ConfiscateEndorsement);
        }
        if (!SupplyEndorsement.IsNullOrNone())
        {
            result.SupplyKey = new Key(SupplyEndorsement);
        }
        if (!MetadataEndorsement.IsNullOrNone())
        {
            result.MetadataKey = new Key(MetadataEndorsement);
        }
        if (!RoyaltiesEndorsement.IsNullOrNone())
        {
            result.FeeScheduleKey = new Key(RoyaltiesEndorsement);
        }
        if (Royalties is { Count: > 0 })
        {
            foreach (var royalty in Royalties)
            {
                result.CustomFees.Add(royalty.ToCustomFee());
            }
        }
        result.FreezeDefault = InitializeSuspended;
        result.Expiry = new Timestamp(Expiration);
        if (!RenewAccount.IsNullOrNone())
        {
            result.AutoRenewAccount = new AccountID(RenewAccount);
        }
        if (RenewPeriod.HasValue)
        {
            result.AutoRenewPeriod = new Duration(RenewPeriod.Value);
        }
        result.Memo = Memo ?? string.Empty;
        return result;
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new CreateTokenReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "Create Token";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CreateTokenExtensions
{
    /// <summary>
    /// Creates a new token with the given create parameters.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the continuation.
    /// </param>
    /// <param name="createParameters">
    /// Details regarding the token to instantiate.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt with a description of the newly created token.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<CreateTokenReceipt> CreateTokenAsync(this ConsensusClient client, CreateTokenParams createParameters, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<CreateTokenReceipt>(createParameters, configure);
    }
}