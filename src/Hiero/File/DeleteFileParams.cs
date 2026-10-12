// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// File deletion parameters.
/// </summary>
/// <example>
/// Permanently delete a network file. At least one of the file's key-list
/// endorsements must sign:
/// <code source="../../../samples/DocSnippets/FileSnippets.cs" region="DeleteFile" language="csharp"/>
/// </example>
public sealed class DeleteFileParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
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
    /// Keys or callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction.
    /// </remarks>
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
        return new FileDeleteTransactionBody()
        {
            FileID = new FileID(File)
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Delete File";
}
/// <summary>
/// Extension methods for deleting network files.
/// </summary>
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
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the delete request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/FileSnippets.cs" region="DeleteFile" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> DeleteFileAsync(this ConsensusClient client, DeleteFileParams deleteParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(deleteParams, configure);
    }
}