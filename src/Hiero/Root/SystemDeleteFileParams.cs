// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;

/// <summary>
/// Transaction parameters for administratively deleting a file from the network.
/// </summary>
/// <example>
/// Superuser-only: remove a file that would otherwise be undeletable.
/// Restorable via <see cref="SystemRestoreFileParams"/>:
/// <code source="../../../samples/DocSnippets/GovernanceSnippets.cs" region="SystemDeleteFile" language="csharp"/>
/// </example>
public sealed class SystemDeleteFileParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The address of the file to delete.
    /// </summary>
    public EntityId File { get; set; } = default!;
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
        return new SystemDeleteTransactionBody
        {
            FileID = new FileID(File)
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "System Delete File";
}
/// <summary>
/// Extension methods for administratively deleting a file from the network.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SystemDeleteFileExtensions
{
    /// <summary>
    /// Removes a file from the network via Administrative Delete
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the administrative command.
    /// </param>
    /// <param name="deleteParams">
    /// The parameters for the system delete operation, including the file address to delete.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating success of the file deletion.
    /// </returns>
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the delete request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/GovernanceSnippets.cs" region="SystemDeleteFile" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> SystemDeleteFileAsync(this ConsensusClient client, SystemDeleteFileParams deleteParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(deleteParams, configure);
    }
}