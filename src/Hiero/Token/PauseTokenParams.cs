// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for Pausing a token preventing all
/// accounts from sending or receiving the specified token.
/// </summary>
/// <example>
/// Halt all transfers of a token across every holder. Contrast with
/// <see cref="SuspendTokenParams"/> which freezes a single holder:
/// <code source="../../../samples/DocSnippets/TokenSnippets.cs" region="PauseToken" language="csharp"/>
/// </example>
public sealed class PauseTokenParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The identifier of the token or NFT class to pause.
    /// </summary>
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to authorize the pause.  Typically matches the
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
    /// Optional cancellation token.  Cancelling stops the client waiting;
    /// it cannot recall a transaction already sent.  Cancelled after the
    /// send, the outcome is unknown: settle it by its transaction id on a
    /// mirror node rather than resubmitting it.
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
/// <summary>
/// Extension methods for pausing a token on the network.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class PauseTokenExtensions
{
    /// <summary>
    /// Pauses all accounts' ability to send or
    /// receive the specified token.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the pause.
    /// </param>
    /// <param name="token">
    /// The identifier of the token to pause.
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
    /// <code source="../../../samples/DocSnippets/TokenSnippets.cs" region="PauseToken" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> PauseTokenAsync(this ConsensusClient client, EntityId token, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new PauseTokenParams { Token = token }, configure);
    }
    /// <summary>
    /// Pauses all accounts' ability to send or
    /// receive the specified token.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the pause.
    /// </param>
    /// <param name="pauseParams">
    /// The details identifying the token to pause.
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
    /// <code source="../../../samples/DocSnippets/TokenSnippets.cs" region="PauseToken" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> PauseTokenAsync(this ConsensusClient client, PauseTokenParams pauseParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(pauseParams, configure);
    }
}