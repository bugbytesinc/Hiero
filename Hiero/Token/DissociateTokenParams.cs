using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for Token and NFT Dissociation Requests.
/// </summary>
public sealed class DissociateTokenParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The Holder that will be un-associated with the Token or NFT class(es)
    /// </summary>
    public EntityId Account { get; set; } = default!;
    /// <summary>
    /// List of Token or NFT class IDs to associate with the account.
    /// </summary>
    public IEnumerable<EntityId> Tokens { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to authorize the un-associations.  Typically matches the
    /// Endorsement assigned to associated account if it is not already
    /// the payer for the transaction.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional cancellation token to interrupt the token
    /// dissociation submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        if (Tokens is null)
        {
            throw new ArgumentNullException(nameof(Tokens), "The list of tokens cannot be null.");
        }
        var result = new TokenDissociateTransactionBody
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
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Dissociate Token from Account";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DissociateTokenExtensions
{
    /// <summary>
    /// Removes Storage associated with the account for maintaining token balances 
    /// for this account.
    /// </summary>
    /// <remarks>
    /// Since this action modifies the account's records, 
    /// it must be signed by the account's key.
    /// </remarks>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the dissociation.
    /// </param>
    /// <param name="token">
    /// The Payer of the token that will be dissociated.
    /// </param>
    /// <param name="account">
    /// Payer of the account that will be dissociated.
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
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission, for example, if the token has already been dissociated.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> DissociateTokenAsync(this ConsensusClient client, EntityId token, EntityId account, Action<IConsensusContext>? configure = null)
    {
        if (token.IsNullOrNone())
        {
            throw new ArgumentNullException(nameof(token), "Token is missing. Please check that it is not null or empty.");
        }
        return client.ExecuteAsync(new DissociateTokenParams { Account = account, Tokens = [token] }, configure);
    }
    /// <summary>
    /// Removes Storage associated with the account for maintaining token balances 
    /// for this account.
    /// </summary>
    /// <remarks>
    /// Since this action modifies the account's records, 
    /// it must be signed by the account's key.
    /// </remarks>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the dissociation.
    /// </param>
    /// <param name="dissociateParams">
    /// The parameters containing the account and tokens to dissociate.
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
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission, for example, if the token has already been dissociated.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> DissociateTokenAsync(this ConsensusClient client, DissociateTokenParams dissociateParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(dissociateParams, configure);
    }
}