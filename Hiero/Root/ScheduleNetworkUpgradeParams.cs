using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;

public sealed class ScheduleNetworkUpgradeParams : TransactionParams, INetworkParams
{
    /// <summary>
    /// The time of consensus that nodes will stop services, this
    /// date must be in the future relative to the submission of
    /// this transaction.
    /// </summary>
    public ConsensusTimeStamp Consensus { get; set; }
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
        return new FreezeTransactionBody
        {
            StartTime = new Timestamp(Consensus),
            FreezeType = FreezeType.FreezeUpgrade
        };
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "Schedule Network Upgrade Command";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ScheduleNetworkUpgradeExtensions
{
    /// <summary>
    /// Executes a previously "prepared" upgrade file at the
    /// specified consensus time across the entire network.
    /// This act will suspend network services for the duration
    /// of the upgrade.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the administrative command.
    /// </param>
    /// <param name="scheduleParams">
    /// The parameters for scheduling the network upgrade.
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
    public static Task<TransactionReceipt> ScheduleNetworkUpgradeAsync(this ConsensusClient client, ScheduleNetworkUpgradeParams scheduleParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<TransactionReceipt>(scheduleParams, configure);
    }
}