using Hiero.Implementation;
using Proto;
using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Hiero;
/// <summary>
/// Transaction Parameters for Continuing (Un-Pausing) a Token.
/// </summary>
public sealed class ContinueTokenParams : TransactionParams, INetworkParams
{
    /// <summary>
    /// The TransactionId of the Fungible or NFT Token Class to continue (un-pause).
    /// </summary>
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to authorize the transfers.  Typically matches the
    /// Endorsement assigned to the pause key for the token if it is not already
    /// set as the payer for the transaction.
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
        return new TokenUnpauseTransactionBody
        {
            Token = new TokenID(Token)
        };
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "Continue Token";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ContinueTokenExtensions
{
    /// <summary>
    /// Continues/Un-Pauses all accounts' ability to send or
    /// receive the specified token (unless they have been
    /// suspended/frozen)
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the continuation.
    /// </param>
    /// <param name="token">
    /// The identifier (Payer) of the token to continue/un-pause
    /// to re-enable transfers between accounts.
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
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission, for example of the token is already deleted.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<TransactionReceipt> ContinueTokenAsync(this ConsensusClient client, EntityId token, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<TransactionReceipt>(new ContinueTokenParams { Token = token }, configure);
    }
    /// <summary>
    /// Continues/Un-Pauses all accounts' ability to send or
    /// receive the specified token (unless they have been
    /// suspended/frozen)
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the continuation.
    /// </param>
    /// <param name="continueParams">
    /// The details identifying the token to continue/un-pause.
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
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission, for example of the token is already deleted.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<TransactionReceipt> ContinueTokenAsync(this ConsensusClient client, ContinueTokenParams continueParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<TransactionReceipt>(continueParams, configure);
    }
}