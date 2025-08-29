using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Represents the properties on a token definition that can be changed.
/// Any property set to <code>null</code> on this object when submitted to the 
/// <see cref="ConsensusClient.UpdateTokenAsync(UpdateTokenParams, Action{IConsensusContext})"/>
/// method will be left unchanged by the system.  The transaction must be
/// appropriately signed as described by the original
/// <see cref="CreateTokenParams.Administrator"/> endorsement in order
/// to make changes.  If there is no administrator endorsement specified,
/// the token is imutable and cannot be changed.
/// </summary>
public sealed class UpdateTokenParams : TransactionParams, INetworkParams
{
    /// <summary>
    /// The identifier of the token to update.
    /// </summary>
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// If specified, changes the treasury account holding the reserve 
    /// balance of tokens.
    /// </summary>
    public EntityId? Treasury { get; set; }
    /// <summary>
    /// Replace this Tokens's current administrative key signing rquirements 
    /// with new signing requirements.  
    /// </summary>
    /// <remarks>
    /// For this request to be accepted by the network, both the current private
    /// key(s) for this account and the new private key(s) must sign the transaction.  
    /// The existing key must sign for security and the new key must sign as a 
    /// safeguard to avoid accidentally changing the key to an invalid value.  
    /// </remarks>
    public Endorsement? Administrator { get; set; }
    /// <summary>
    /// Changes the administrator key for signing transactions updating 
    /// the grant or revoke KYC status of an account.
    /// </summary>
    public Endorsement? GrantKycEndorsement { get; set; }
    /// <summary>
    /// Changes the administrator key for signing transactions for freezing 
    /// or unfreezing an account's ability to transfer tokens.
    /// </summary>
    public Endorsement? SuspendEndorsement { get; set; }
    /// <summary>
    /// Changes the administrator key for signing transactions that can 
    /// pasue or continue the exchange of all assets across all accounts 
    /// on the network.
    /// </summary>
    public Endorsement? PauseEndorsement { get; set; }
    /// <summary>
    /// Changes the administrator key for signing transaction that completely 
    /// remove tokens from an crypto address.
    /// </summary>
    public Endorsement? ConfiscateEndorsement { get; set; }
    /// <summary>
    /// Changes the administrator key for signing transactions for minting 
    /// or unminting tokens in the treasury account.
    /// </summary>
    public Endorsement? SupplyEndorsement { get; set; }
    /// <summary>
    /// Changes the administrator key for updating metadata associated with
    /// tokens and assets.
    /// </summary>
    public Endorsement? MetadataEndorsement { get; set; }
    /// <summary>
    /// Changes the administrator key for signing transactions updating the 
    /// royalties (custom transfer fees) associated with this token.
    /// </summary>
    public Endorsement? RoyaltiesEndorsement { get; set; }
    /// <summary>
    /// If specified, replaces the current symbol for this 
    /// token with the new Symbol.
    /// </summary>
    public string? Symbol { get; set; }
    /// <summary>
    /// If specified, replaces the current name of this
    /// token with the new name.
    /// </summary>
    public string? Name { get; set; }
    /// <summary>
    /// If specified, changes to expiration new date, fees will be charged as appropriate.
    /// </summary>
    public ConsensusTimeStamp? Expiration { get; set; }
    /// <summary>
    /// If specified, update the interval of the topic and auto-renewal period. 
    /// If the associated renewal account does not have sufficient funds to 
    /// renew at the expiration time, it will be renewed for a period 
    /// of time the remaining funds can support.  If no funds remain, the
    /// topic instance will be deleted.
    /// </summary>
    public TimeSpan? RenewPeriod { get; set; }
    /// <summary>
    /// If specified updates the address of the account supporting the auto 
    /// renewal of the token at expiration time.  The topic lifetime will be
    /// extended by the RenewPeriod at expiration time if this account
    /// contains sufficient funds.  The private key associated with
    /// this account must sign the transaction if RenewAccount is
    /// specified.  Setting the value to <code>Payer.None</code> clears the
    /// renewal account.
    /// </summary>
    public EntityId? RenewAccount { get; set; }
    /// <summary>
    /// If specified, updates the publicly visible memo to be associated 
    /// with the token.
    /// </summary>
    public string? Memo { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to authorize the update.  Typically matches the
    /// administrative Endorsement associated with the token if it 
    /// is not already the same as the payer for the transaction.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token that interrupt the update.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams.CreateNetworkTransaction()
    {
        if (Token.IsNullOrNone())
        {
            throw new ArgumentNullException(nameof(Token), "The Token is missing.  Please check that it is not null or empty.");
        }
        if (Treasury is null &&
            Administrator is null &&
            GrantKycEndorsement is null &&
            SuspendEndorsement is null &&
            PauseEndorsement is null &&
            ConfiscateEndorsement is null &&
            SupplyEndorsement is null &&
            MetadataEndorsement is null &&
            RoyaltiesEndorsement is null &&
            string.IsNullOrWhiteSpace(Symbol) &&
            string.IsNullOrWhiteSpace(Name) &&
            !Expiration.HasValue &&
            !RenewPeriod.HasValue &&
            RenewAccount is null &&
            Memo is null)
        {
            throw new ArgumentException("The Topic Updates contain no update properties, it is blank.", nameof(UpdateTokenParams));
        }
        var result = new TokenUpdateTransactionBody()
        {
            Token = new TokenID(Token)
        };
        if (!string.IsNullOrWhiteSpace(Symbol))
        {
            if (Symbol.Trim().Length != Symbol.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(Symbol), "The new token symbol cannot contain leading or trailing white space.");
            }
            if (Symbol.Length > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(Symbol), "The new token symbol cannot exceed 100 characters in length.");
            }
            result.Symbol = Symbol;
        }
        if (!string.IsNullOrWhiteSpace(Name))
        {
            if (Name.Trim().Length != Name.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(Name), "The new token name cannot contain leading or trailing white space.");
            }
            result.Name = Name;
        }
        if (Expiration.HasValue)
        {
            if (Expiration.Value < ConsensusTimeStamp.Now)
            {
                throw new ArgumentOutOfRangeException(nameof(Expiration), "The new expiration can not be set to the past.");
            }
            result.Expiry = new Timestamp(Expiration.Value);
        }
        if (RenewPeriod.HasValue)
        {
            if (RenewPeriod.Value.TotalSeconds < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(RenewPeriod), "The renew period must be non negative.");
            }
            result.AutoRenewPeriod = new Duration(RenewPeriod.Value);
        }
        if (Memo is not null)
        {
            if (Memo.Trim().Length != Memo.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(Memo), "The new token memo cannot contain leading or trailing white space.");
            }
            result.Memo = Memo;
        }
        if (Treasury is not null)
        {
            result.Treasury = new AccountID(Treasury);
        }
        if (Administrator is not null)
        {
            result.AdminKey = new Key(Administrator);
        }
        if (GrantKycEndorsement is not null)
        {
            result.KycKey = new Key(GrantKycEndorsement);
        }
        if (SuspendEndorsement is not null)
        {
            result.FreezeKey = new Key(SuspendEndorsement);
        }
        if (PauseEndorsement is not null)
        {
            result.PauseKey = new Key(PauseEndorsement);
        }
        if (ConfiscateEndorsement is not null)
        {
            result.WipeKey = new Key(ConfiscateEndorsement);
        }
        if (SupplyEndorsement is not null)
        {
            result.SupplyKey = new Key(SupplyEndorsement);
        }
        if (MetadataEndorsement is not null)
        {
            result.MetadataKey = new Key(MetadataEndorsement);
        }
        if (RoyaltiesEndorsement is not null)
        {
            result.FeeScheduleKey = new Key(RoyaltiesEndorsement);
        }
        if (RenewAccount is not null)
        {
            result.AutoRenewAccount = new AccountID(RenewAccount);
        }
        return result;
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "Update Token";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class UpdateTokenExtensions
{
    /// <summary>
    /// Updates the changeable properties of a Hedera Fungable Token.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the update.
    /// </param>
    /// <param name="updateParameters">
    /// The Token update parameters, includes a required 
    /// <see cref="EntityId"/> or <code>Symbol</code> reference to the Token 
    /// to update plus a number of changeable properties of the Token.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating success of the operation.
    /// of the request.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<TransactionReceipt> UpdateTokenAsync(this ConsensusClient client, UpdateTokenParams updateParameters, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<TransactionReceipt>(updateParameters, configure);
    }
}