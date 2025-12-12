using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Token parameters for resuming an account's ability to 
/// send or receive the specified token.
/// </summary>
public sealed class ResumeTokenParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The TransactionId of token or NFT class to resume.
    /// </summary>
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// The TransactionId of the account holding the token 
    /// that may resume transactions.
    /// </summary>
    public EntityId Holder { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to authorize the transfers.  Typically matches the
    /// Endorsement assigned to the Suspend key for the token if it 
    /// is not already set as the payer for the transaction.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional cancellation token to interrupt the token
    /// resumption submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        return new TokenUnfreezeAccountTransactionBody
        {
            Token = new TokenID(Token),
            Account = new AccountID(Holder)
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Resume Token";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ResumeTokenExtensions
{
    /// <summary>
    /// Resumes the associated holding account's ability 
    /// to send or receive the specified token.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the resume/unfreeze.
    /// </param>
    /// <param name="token">
    /// The identifier of the token to resume/unfreeze.
    /// </param>
    /// <param name="holder">
    /// The account holding the token that will be resumed/unfrozen.
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
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission, for example, if the token is already deleted.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> ResumeTokenAsync(this ConsensusClient client, EntityId token, EntityId holder, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new ResumeTokenParams { Token = token, Holder = holder }, configure);
    }
    /// <summary>
    /// Resumes the associated holding account's ability 
    /// to send or receive the specified token.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the resume/unfreeze.
    /// </param>
    /// <param name="resumeParams">
    /// The details identifying the token and account to resume/unfreeze.
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
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission, for example, if the token is already deleted.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> ResumeTokenAsync(this ConsensusClient client, ResumeTokenParams resumeParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(resumeParams, configure);
    }
}