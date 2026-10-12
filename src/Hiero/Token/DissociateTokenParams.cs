// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for Token and NFT Dissociation Requests.
/// </summary>
/// <example>
/// Remove the token-balance storage slot from an account. Account must hold
/// zero balance of the token first:
/// <code source="../../../samples/DocSnippets/TokenSnippets.cs" region="DissociateToken" language="csharp"/>
/// </example>
public sealed class DissociateTokenParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The account that will be un-associated with the Token or NFT class(es).
    /// </summary>
    public EntityId Account { get; set; } = default!;
    /// <summary>
    /// List of Token or NFT class IDs to dissociate from the account.
    /// </summary>
    public IReadOnlyList<EntityId> Tokens { get; set; } = default!;
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
    /// Optional cancellation token.  Cancelling stops the client waiting;
    /// it cannot recall a transaction already sent.  Cancelled after the
    /// send, the outcome is unknown: settle it by its transaction id on a
    /// mirror node rather than resubmitting it.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        if (Tokens is null)
        {
            throw new ArgumentNullException(nameof(Tokens), "The list of tokens cannot be null.");
        }
        var count = Tokens.Count;
        if (count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Tokens), "The list of tokens cannot be empty.");
        }
        var result = new TokenDissociateTransactionBody
        {
            Account = new AccountID(Account)
        };
        var tokens = result.Tokens;
        if (tokens.Capacity < count)
        {
            tokens.Capacity = count;
        }
        for (var i = 0; i < count; i++)
        {
            var token = Tokens[i];
            if (token.IsNullOrNone())
            {
                throw new ArgumentOutOfRangeException(nameof(Tokens), "The list of tokens cannot contain an empty or null address.");
            }
            tokens.Add(new TokenID(token));
        }
        return result;
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Dissociate Token from Account";
}
/// <summary>
/// Extension methods for dissociating tokens from accounts on the network.
/// </summary>
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
    /// The identifier of the token that will be dissociated.
    /// </param>
    /// <param name="account">
    /// The identifier of the account that will be dissociated.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating a successful operation.
    /// </returns>
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/TokenSnippets.cs" region="DissociateToken" language="csharp"/>
    /// </example>
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
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/TokenSnippets.cs" region="DissociateToken" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> DissociateTokensAsync(this ConsensusClient client, DissociateTokenParams dissociateParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(dissociateParams, configure);
    }
}
