using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Transaction Parameters for Token and NFT Association Requests.
/// </summary>
public sealed class AssociateTokenParams : TransactionParams, INetworkParams
{
    /// <summary>
    /// The Holder that will be associated with the Token or NFT class(es)
    /// </summary>
    public EntityId Account { get; set; } = default!;
    /// <summary>
    /// List of Token or NFT class IDs to associate with the account.
    /// </summary>
    public IEnumerable<EntityId> Tokens { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to authorize the associations.  Typically matches the
    /// Endorsement assigned to assocated account if it is not already
    /// the payer for the transaction.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token that interrupt the token
    /// submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams.CreateNetworkTransaction()
    {
        if (Tokens is null)
        {
            throw new ArgumentNullException(nameof(Tokens), "The list of tokens cannot be null.");
        }
        var result = new TokenAssociateTransactionBody()
        {
            Account = new AccountID(Account)
        };
        result.Tokens.AddRange(Tokens.Select(token =>
        {
            if (token.IsNullOrNone())
            {
                throw new ArgumentOutOfRangeException(nameof(Tokens), "The list of tokens cannot contain an empty or null address.");
            }
            return new TokenID(token);
        }));
        if (result.Tokens.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Tokens), "The list of tokens cannot be empty.");
        }
        return result;
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "Associate Token with Account";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AssociateTokenExtensions
{
    /// <summary>
    /// Provisions Storage associated with the Holder
    /// for maintaining token balances for this account.
    /// </summary>
    /// <remarks>
    /// Since this action will result in higher account renewal costs, 
    /// it must be signed by the account's key.
    /// </remarks>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the transfer.
    /// </param>
    /// <param name="account">
    /// Payer of the account to provision token balance storage.
    /// </param>
    /// <param name="token">
    /// The token or NFT token class type to associate with the account.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating a successful operation.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission, for example of the token has already been associated.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<TransactionReceipt> AssociateTokenAsync(this ConsensusClient client, EntityId account, EntityId token, Action<IConsensusContext>? configure = null)
    {
        if (token.IsNullOrNone())
        {
            throw new ArgumentNullException(nameof(token), "Token is missing. Please check that it is not null or empty.");
        }
        return client.ExecuteNetworkParamsAsync<TransactionReceipt>(new AssociateTokenParams { Account = account, Tokens = [token] }, configure);
    }
    /// <summary>
    /// Provisions Storage associated with the Holder
    /// for maintaining token balances for this account.
    /// </summary>
    /// <remarks>
    /// Since this action will result in higher account renewal costs, 
    /// it must be signed by the account's key.
    /// </remarks>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the transfer.
    /// </param>
    /// <param name="associateParams">
    /// The association parameters containing the account and tokens to associate.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating a successful operation.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission, for example of the token has already been associated.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<TransactionReceipt> AssociateTokensAsync(this ConsensusClient client, AssociateTokenParams associateParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<TransactionReceipt>(associateParams, configure);
    }
}