using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for Confiscating/Wiping Tokens from an arbitrary account.
/// </summary>
public sealed class ConfiscateTokenParams : TransactionParams<TokenReceipt>, INetworkParams<TokenReceipt>
{
    /// <summary>
    /// The TransactionId of the fungible tokens to confiscate (wipe) and
    /// return to the treasury account.
    /// </summary>
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// The TransactionId of the account holding the tokens to confiscate (wipe).
    /// </summary>
    public EntityId Holder { get; set; } = default!;
    /// <summary>
    /// The Amount of fungible tokens to confiscate and return
    /// to the treasury account, specified in the smallest denomination.
    /// </summary>
    public ulong Amount { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to authorize the transfers.  Typically matches the
    /// Endorsement assigned to the wipe key for the token if it is not already
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
    /// confiscation submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TokenReceipt>.CreateNetworkTransaction()
    {
        if (Amount == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Amount), "The amount to confiscate must be greater than zero.");
        }
        return new TokenWipeAccountTransactionBody
        {
            Token = new TokenID(Token),
            Account = new AccountID(Holder),
            Amount = Amount
        };
    }
    TokenReceipt INetworkParams<TokenReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TokenReceipt(transactionId, receipt);
    }
    string INetworkParams<TokenReceipt>.OperationDescription => "Confiscate Tokens";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConfiscateTokenExtensions
{
    /// <summary>
    /// Removes the holdings of given token from the associated 
    /// account and returns them to the treasury. Must be signed by 
    /// the confiscate/wipe admin key.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the confiscation.
    /// </param>
    /// <param name="token">
    /// The identifier of the token that will be confiscated.
    /// </param>
    /// <param name="holder">
    /// Holder holding the tokens to be confiscated.
    /// </param>
    /// <param name="amount">
    /// The amount of fungible token to confiscate and return to
    /// the treasury account.
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
    public static Task<TokenReceipt> ConfiscateTokensAsync(this ConsensusClient client, EntityId token, EntityId holder, ulong amount, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new ConfiscateTokenParams { Token = token, Holder = holder, Amount = amount }, configure);
    }
    /// <summary>
    /// Removes the holdings of given token from the associated 
    /// account and returns them to the treasury. Must be signed by 
    /// the confiscate/wipe admin key.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the confiscation.
    /// </param>
    /// <param name="confiscateParams">
    /// The details identifying the token, holder, and amount to confiscate.
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
    public static Task<TokenReceipt> ConfiscateTokensAsync(this ConsensusClient client, ConfiscateTokenParams confiscateParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(confiscateParams, configure);
    }
}