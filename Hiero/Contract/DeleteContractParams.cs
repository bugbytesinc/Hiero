using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Represents the parameters required to delete a contract from the hedera network.
/// </summary>
public sealed class DeleteContractParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The address of the contract to delete.
    /// </summary>
    public EntityId Contract { get; set; } = default!;
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
    /// Optional Cancellation token that interrupt the contract
    /// call.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        if (Contract.IsNullOrNone())
        {
            throw new ArgumentNullException(nameof(Contract), "Contract to Delete is missing. Please check that it is not null.");
        }
        if (FundsReceiver.IsNullOrNone())
        {
            throw new ArgumentNullException(nameof(FundsReceiver), "Transfer address is missing. Please check that it is not null.");
        }
        return new ContractDeleteTransactionBody()
        {
            ContractID = new ContractID(Contract),
            TransferAccountID = new AccountID(FundsReceiver)
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Delete Contract";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DeleteContractExtensions
{
    /// <summary>
    /// Deletes a contract instance from the network returning the remaining 
    /// crypto balance to the specified address.  Must be signed 
    /// by the admin key.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client executing the contract delete.
    /// </param>
    /// <param name="deleteParams">
    /// The contract delete parameters.
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
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission, for example of the contract is already deleted.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> DeleteContractAsync(this ConsensusClient client, DeleteContractParams deleteParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(deleteParams, configure);
    }
}