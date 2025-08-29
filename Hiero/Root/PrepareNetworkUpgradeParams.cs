using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;

public sealed class PrepareNetworkUpgradeParams : TransactionParams, INetworkParams
{
    /// <summary>
    /// Address the upgrade file (previously uploaded).
    /// </summary>
    public EntityId File { get; set; } = default!;
    /// <summary>
    /// Hash of the file upgrade file's contents.
    /// </summary>
    public ReadOnlyMemory<byte> FileHash { get; set; }
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
        if (File.IsNullOrNone())
        {
            throw new ArgumentOutOfRangeException(nameof(File), "The upgrade file's File Address ID is missing.");
        }
        if (FileHash.IsEmpty)
        {
            throw new ArgumentOutOfRangeException(nameof(FileHash), "The hash of the file contents must be included.");
        }
        return new FreezeTransactionBody
        {
            UpdateFile = new FileID(File),
            FileHash = ByteString.CopyFrom(FileHash.Span),
            FreezeType = FreezeType.PrepareUpgrade
        };
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "Prepare Upgrade Command";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class PrepareNetworkUpgradeExtensions
{
    /// <summary>
    /// Prepares the network for an upgrade as configured by 
    /// the specified upgrade file.  The file hash must match 
    /// the hash of the identified upgrade file stored at the 
    /// specified location.  This operation does not 
    /// immediately affect network operations.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the administrative command.
    /// </param>
    /// <param name="prepareParams">
    /// The parameters for preparing the network upgrade.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A TransactionId Receipt indicating success.
    /// </returns>
    /// <remarks>
    /// This operation must be submitted by a privileged account
    /// having access rights to perform this operation.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<TransactionReceipt> PrepareNetworkUpgradeAsync(this ConsensusClient client, PrepareNetworkUpgradeParams prepareParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<TransactionReceipt>(prepareParams, configure);
    }
}