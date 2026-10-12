// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;

/// <summary>
/// Transaction parameters for scheduling a previously prepared network upgrade.
/// </summary>
/// <example>
/// Step 2 of the upgrade flow: commit to the consensus timestamp at which
/// every node will bounce and load the staged upgrade:
/// <code source="../../../samples/DocSnippets/GovernanceSnippets.cs" region="ScheduleNetworkUpgrade" language="csharp"/>
/// </example>
public sealed class ScheduleNetworkUpgradeParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
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
    /// Optional cancellation token.  Cancelling stops the client waiting;
    /// it cannot recall a transaction already sent.  Cancelled after the
    /// send, the outcome is unknown: settle it by its transaction id on a
    /// mirror node rather than resubmitting it.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        return new FreezeTransactionBody
        {
            StartTime = new Timestamp(Consensus),
            FreezeType = FreezeType.FreezeUpgrade
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Schedule Network Upgrade Command";
}
/// <summary>
/// Extension methods for scheduling a network upgrade.
/// </summary>
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
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/GovernanceSnippets.cs" region="ScheduleNetworkUpgrade" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> ScheduleNetworkUpgradeAsync(this ConsensusClient client, ScheduleNetworkUpgradeParams scheduleParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(scheduleParams, configure);
    }
}