using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Token parameters for suspending an account's ability to 
/// send or receive the specified token.
/// </summary>
public sealed class SuspendTokenParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The TransactionId of token or NFT class to suspend.
    /// </summary>
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// The TransactionId of the account holding the token 
    /// that will be suspended/frozen.
    /// </summary>
    public EntityId Holder { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to authorize the transfers.  Typically matches the
    /// Endorsement assigned to the Suspend/Freeze key for the token if it 
    /// is not already set as the payer for the transaction.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token that interrupt transaction submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        return new TokenFreezeAccountTransactionBody
        {
            Token = new TokenID(Token),
            Account = new AccountID(Holder)
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Suspend Token";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SuspendTokenExtensions
{
    /// <summary>
    /// Suspends the associated account's ability to send or
    /// receive the specified token.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the suspend.
    /// </param>
    /// <param name="token">
    /// The identifier (Payer/Symbol) of the token to suspend/freeze.
    /// </param>
    /// <param name="holder">
    /// Payer of the account to suspend.
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> SuspendTokenAsync(this ConsensusClient client, EntityId token, EntityId holder, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new SuspendTokenParams { Token = token, Holder = holder }, configure);
    }
    /// <summary>
    /// Suspends the associated account's ability to send or
    /// receive the specified token.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the suspend.
    /// </param>
    /// <param name="token">
    /// The identifier (Payer/Symbol) of the token to suspend/freeze.
    /// </param>
    /// <param name="holder">
    /// Payer of the account to suspend.
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> SuspendTokenAsync(this ConsensusClient client, SuspendTokenParams suspendParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(suspendParams, configure);
    }
}