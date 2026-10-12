// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;

/// <summary>
/// Transaction parameters for suspending network operations at a specified consensus time.
/// </summary>
/// <example>
/// Emergency freeze — halts every node at the given consensus timestamp.
/// Superuser payer only:
/// <code source="../../../samples/DocSnippets/GovernanceSnippets.cs" region="SuspendNetwork" language="csharp"/>
/// </example>
public sealed class SuspendNetworkParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
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
            FreezeType = FreezeType.FreezeOnly
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Suspend Network Command";
}
/// <summary>
/// Extension methods for suspending network operations.
/// </summary>
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
    /// The parameters for suspending the network, including the
    /// consensus time at which nodes will stop services.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating success.
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
    /// <code source="../../../samples/DocSnippets/GovernanceSnippets.cs" region="SuspendNetwork" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> SuspendNetworkAsync(this ConsensusClient client, SuspendNetworkParams suspendParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(suspendParams, configure);
    }
}