// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for Revoking KYC status from an account
/// associated with a Token.
/// </summary>
/// <example>
/// Clear a holder's KYC-approved flag; future transfers by or to this
/// holder will fail until KYC is re-granted:
/// <code source="../../../samples/DocSnippets/TokenSnippets.cs" region="RevokeKyc" language="csharp"/>
/// </example>
public sealed class RevokeTokenKycParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The identifier of the Fungible or NFT Token Class to revoke the KYC.
    /// </summary>
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// The identifier of the account associated or holding the tokens.
    /// </summary>
    public EntityId Holder { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to authorize the KYC revocation.  Typically matches the
    /// Endorsement assigned to the KYC key for the token if it is not already
    /// set as the payer for the transaction.
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
        return new TokenRevokeKycTransactionBody
        {
            Token = new TokenID(Token),
            Account = new AccountID(Holder)
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Revoke Token KYC";
}
/// <summary>
/// Extension methods for revoking KYC status from token-holding accounts.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RevokeTokenKycExtensions
{
    /// <summary>
    /// Revokes KYC status from the holding account relating to the specified token.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the revoke.
    /// </param>
    /// <param name="token">
    /// The identifier of the token to revoke KYC status.
    /// </param>
    /// <param name="holder">
    /// The holding account to revoke KYC status from.
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
    /// <code source="../../../samples/DocSnippets/TokenSnippets.cs" region="RevokeKyc" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> RevokeTokenKycAsync(this ConsensusClient client, EntityId token, EntityId holder, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new RevokeTokenKycParams { Token = token, Holder = holder }, configure);
    }
    /// <summary>
    /// Revokes KYC status from the holding account relating to the specified token.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the revoke.
    /// </param>
    /// <param name="revokeKycParams">
    /// The details identifying the token and account to revoke KYC status from.
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
    /// <code source="../../../samples/DocSnippets/TokenSnippets.cs" region="RevokeKyc" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> RevokeTokenKycAsync(this ConsensusClient client, RevokeTokenKycParams revokeKycParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(revokeKycParams, configure);
    }
}