using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;
public sealed class SystemRestoreFileParams : TransactionParams, INetworkParams
{
    /// <summary>
    /// The address of the file to restore.
    /// </summary>
    public EntityId File { get; set; } = default!;
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
            FileID = new FileID(File)
        };
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "System Restore File";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SystemRestoreFileExtensions
{
    /// <summary>
    /// Undeletes a file from the network via Administrative Restore
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the administrative command.
    /// </param>
    /// <param name="restoreParams">
    /// The parameters for the system restore operation, including the file address to restore.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating success of the file deletion.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<TransactionReceipt> SystemRestoreFileAsync(this ConsensusClient client, SystemRestoreFileParams restoreParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<TransactionReceipt>(restoreParams, configure);
    }
}