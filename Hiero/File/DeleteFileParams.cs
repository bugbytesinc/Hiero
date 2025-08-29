using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Represents the parameters required to delete a file from the hedera network.
/// </summary>
public class DeleteFileParams : TransactionParams, INetworkParams
{
    /// <summary>
    /// The address of the file to delete.
    /// </summary>
    public EntityId File { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to delete this file.
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
        return new FileDeleteTransactionBody()
        {
            FileID = new FileID(File)
        };
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "Delete File";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DeleteFileExtensions
{
    /// <summary>
    /// Removes a file from the network.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client executing the file deletion.
    /// </param>
    /// <param name="deleteParams">
    /// The parameters indicating the file to delete and any additional
    /// signatories necessary to authorize the transaction.
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
    public static Task<TransactionReceipt> DeleteFileAsync(this ConsensusClient client, DeleteFileParams deleteParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<TransactionReceipt>(deleteParams, configure);
    }
}