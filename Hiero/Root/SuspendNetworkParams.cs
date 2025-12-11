using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;

public sealed class SuspendNetworkParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The time of consensus that nodes will stop services, this
    /// date must be in the future relative to the submission of
    /// this transaciton.
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
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        return new FreezeTransactionBody
        {
            StartTime = new Timestamp(Consensus),
            FreezeType = FreezeType.FreezeOnly
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Suspend Network Command";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SuspendNetworkExtensions
{
    /// <summary>
    /// Suspends the network at the specified consensus time.  
    /// This does not result in any network changes or upgrades 
    /// and requires manual intervention to restart the network.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the administrative command.
    /// </param>
    /// <param name="suspendParams">
    /// The time of consensus that nodes will stop services, this
    /// date must be in the future relative to the submission of
    /// this transaciton.
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> SuspendNetworkAsync(this ConsensusClient client, SuspendNetworkParams suspendParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(suspendParams, configure);
    }
}