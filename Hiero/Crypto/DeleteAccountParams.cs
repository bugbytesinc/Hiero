using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Represents the parameters required to delete an account from the hedera network.
/// </summary>
public class DeleteAccountParams : TransactionParams, INetworkParams
{
    /// <summary>
    /// The address of the account to delete.
    /// </summary>
    public EntityId Account { get; set; } = default!;
    /// <summary>
    /// The address of the account that will receive any remaining 
    /// crypto funds held by the contract.
    /// </summary>
    public EntityId FundsReceiver { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to delete this contract.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token that interrupt the delete attempt.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams.CreateNetworkTransaction()
    {
        if (Account.IsNullOrNone())
        {
            throw new ArgumentNullException(nameof(Account), "Address to Delete is missing. Please check that it is not null.");
        }
        if (FundsReceiver.IsNullOrNone())
        {
            throw new ArgumentNullException(nameof(FundsReceiver), "Transfer address is missing. Please check that it is not null.");
        }
        return new CryptoDeleteTransactionBody()
        {
            DeleteAccountID = new AccountID(Account),
            TransferAccountID = new AccountID(FundsReceiver)

        };
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "Delete Account";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DeleteAccountExtensions
{
    /// <summary>
    /// Deletes an account from the network returning the remaining 
    /// crypto balance to the specified account.  Must be signed 
    /// by the account being deleted.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the delete.
    /// </param>
    /// <param name="deleteAccountParams">
    /// The account deletion parameters, includes a required
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
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<TransactionReceipt> DeleteAccountAsync(this ConsensusClient client, DeleteAccountParams deleteAccountParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<TransactionReceipt>(deleteAccountParams, configure);
    }
}