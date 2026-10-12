// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// File content append parameters.
/// </summary>
/// <example>
/// Append bytes to an existing file. Transactions are capped at about 4KB,
/// so large payloads should be chunked across several AppendFile calls:
/// <code source="../../../samples/FileService/Program.cs" region="AppendFile" language="csharp"/>
/// </example>
public sealed class AppendFileParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The file receiving the appended content.
    /// </summary>
    public EntityId File { get; set; } = default!;
    /// <summary>
    /// The content to append to the file, in bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Contents { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to append to this file.  Typically matches the
    /// Endorsement associated with this file.
    /// </summary>
    /// <remarks>
    /// Keys or callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to append to this file.
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
        if (File is null)
        {
            throw new ArgumentNullException(nameof(File), "File identifier is missing. Please check that it is not null.");
        }
        return new FileAppendTransactionBody()
        {
            FileID = new FileID(File),
            Contents = ByteString.CopyFrom(Contents.Span)
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Append File";
}
/// <summary>
/// Extension methods for appending content to network files.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AppendFileExtensions
{
    /// <summary>
    /// Appends content to an existing file.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client executing the file append.
    /// </param>
    /// <param name="appendParameters">
    /// Configuration object identifying the file and contents to append.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating the success of the operation.
    /// </returns>
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the append request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/FileService/Program.cs" region="AppendFile" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> AppendFileAsync(this ConsensusClient client, AppendFileParams appendParameters, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(appendParameters, configure);
    }
}
