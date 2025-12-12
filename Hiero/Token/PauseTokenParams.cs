using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for Pausing a token preventing all 
/// accounts from sending or receiving the specified token.
/// </summary>
public sealed class PauseTokenParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The TransactionId of token or NFT class to pause.
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
    /// Optional cancellation token to interrupt the token
    /// pausing submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        return new TokenPauseTransactionBody
        {
            Token = new TokenID(Token)
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Pause Token";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class PauseTokenExtensions
{
    /// <summary>
    /// Pauses the all accounts' ability to send or
    /// receive the specified token.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the pause.
    /// </param>
    /// <param name="token">
    /// The identifier of the token to pause/suspend exchanges.
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
    public static Task<TransactionReceipt> PauseTokenAsync(this ConsensusClient client, EntityId token, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new PauseTokenParams { Token = token }, configure);
    }
    /// <summary>
    /// Pauses the all accounts' ability to send or
    /// receive the specified token.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the pause.
    /// </param>
    /// <param name="pauseParams">
    /// The details identifying the token to pause/suspend exchanges.
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
    public static Task<TransactionReceipt> PauseTokenAsync(this ConsensusClient client, PauseTokenParams pauseParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(pauseParams, configure);
    }
}