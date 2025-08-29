using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;
public sealed class SystemRestoreContractParams : TransactionParams, INetworkParams
{
    /// <summary>
    /// The address of the contract to restore.
    /// </summary>
    public EntityId Contract { get; set; } = default!;
    /// <summary>
    /// Optional additional signatories.
    /// </summary>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// An optional cancellation token that can be used to interrupt the transaction.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams.CreateNetworkTransaction()
    {
        return new SystemUndeleteTransactionBody
        {
            ContractID = new ContractID(Contract)
        };
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "System Restore Contract";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SystemRestoreContractExtensions
{
    /// <summary>
    /// Restores a contract to the network via Administrative Restore
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the administrative command.
    /// </param>
    /// <param name="restoreParams">
    /// The parameters for the system restore operation, including the contract address to restore.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating success of the contract deletion.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<TransactionReceipt> SystemRestoreContractAsync(this ConsensusClient client, SystemRestoreContractParams restoreParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<TransactionReceipt>(restoreParams, configure);
    }
}