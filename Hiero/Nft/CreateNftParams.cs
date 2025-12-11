using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// NFT Token Type Creation Parameters.
/// </summary>
/// <remarks>
/// These parameters are used to create a new NFT token type on the Hedera network.
/// To create instances of an NFT, use the mint functionality.
/// </remarks>
public sealed class CreateNftParams : TransactionParams<CreateTokenReceipt>, INetworkParams<CreateTokenReceipt>
{
    /// <summary>
    /// Name of the NFT class of tokens.
    /// </summary>
    public string Name { get; set; } = default!;
    /// <summary>
    /// Symbol of the NFT class of tokens.
    /// </summary>
    public string Symbol { get; set; } = default!;
    /// <summary>
    /// The maximum number of NFTs allowed to be minted. If set to
    /// a value of zero or less, an infinite amount of assets can be minted.
    /// </summary>
    public long Ceiling { get; set; }
    /// <summary>
    /// The treasury account receiving NFTs when they are minted.
    /// </summary>
    public EntityId Treasury { get; set; } = default!;
    /// <summary>
    /// Administrator key for signing transactions modifying this NFT Token's 
    /// metadata properties.
    /// </summary>
    public Endorsement? Administrator { get; set; }
    /// <summary>
    /// Administrator key for signing transactions updating the grant or revoke 
    /// KYC status of an account.
    /// </summary>
    public Endorsement? GrantKycEndorsement { get; set; }
    /// <summary>
    /// Administrator key for signing transactions for freezing or unfreezing an 
    /// account's ability to transfer assets.
    /// </summary>
    public Endorsement? SuspendEndorsement { get; set; }
    /// <summary>
    /// Administrator key for signing transactions that can pasue or continue
    /// the exchange of all assets across all accounts on the network.
    /// </summary>
    public Endorsement? PauseEndorsement { get; set; }
    /// <summary>
    /// Administrator key for signing transaction that confiscate and destroy
    /// (wipe) NFTs from an aribrary crypto address.
    /// </summary>
    public Endorsement? ConfiscateEndorsement { get; set; }
    /// <summary>
    /// Administrator key for signing transactions for minting or unminting 
    /// NFTs in the treasury account.
    /// </summary>
    public Endorsement? SupplyEndorsement { get; set; }
    /// <summary>
    /// Administrator key for signing transactions updating the royalty
    /// (custom transfer fees) associated with this NFT.
    /// </summary>
    public Endorsement? RoyaltiesEndorsement { get; set; }
    /// <summary>
    /// Administrator key for changing metadata associated with this NFT
    /// token type.
    /// </summary>
    public Endorsement? MetadataEndorsement { get; set; }
    /// <summary>
    /// The list of royalties applied to transactions when
    /// transferring this asset.  If a royalty endorsement is not
    /// supplied upon creation, the royalties are imutable after
    /// creation.
    /// </summary>
    public IReadOnlyList<IRoyalty>? Royalties { get; set; }
    /// <summary>
    /// The default frozen setting for current and newly created accounts.  A value 
    /// of <code>true</code> will default crypto account status of <code>Frozen</code> 
    /// with relationship to this asset.  A value of <code>false</code> will default 
    /// to an tradable/unfrozen relationship.
    /// </summary>
    public bool InitializeSuspended { get; set; }
    /// <summary>
    /// Original expiration date for the NTF, fees will be charged as appropriate.
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
    /// the asset at expiration time.  The topic lifetime will be
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
    /// Additional Short description of the asset, not checked for uniqueness.
    /// </summary>
    public string Memo { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to create to this asset.  Typically matches the
    /// Administrator, KycEndorsement, FreezeEndorsement and other
    /// listed endorsements associated with this asset.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token that interrupt the token
    /// creation process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    /// <summary>
    /// Creates a Crypto Transfer Transaction Body from these
    /// parameters.
    /// </summary>
    /// <returns>
    /// CryptoTransferTransactionBody implementing INetworkTransaction
    /// </returns>
    INetworkTransaction INetworkParams<CreateTokenReceipt>.CreateNetworkTransaction()
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
            throw new ArgumentOutOfRangeException(nameof(Symbol), "The token symbol cannot exceed 100 characters in length.");
        }
        if (Treasury.IsNullOrNone())
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
            Treasury = new AccountID(Treasury),
            TokenType = Proto.TokenType.NonFungibleUnique,
            Memo = Memo ?? string.Empty
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
        return result;
    }
    CreateTokenReceipt INetworkParams<CreateTokenReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new CreateTokenReceipt(transactionId, receipt);
    }
    string INetworkParams<CreateTokenReceipt>.OperationDescription => "Create NFT";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CreateNftExtensions
{
    /// <summary>
    /// Creates a new Non-Fungible token definition with the given create parameters.
    /// <param name="client">
    /// The Consensus Node Client orchestrating the confiscation.
    /// </param>
    /// </summary>
    /// <param name="createParameters">
    /// Details regarding the NFT definition to instantiate.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt with a description of the newly created NFT metadata.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<CreateTokenReceipt> CreateNftAsync(this ConsensusClient client, CreateNftParams createParameters, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(createParameters, configure);
    }
}