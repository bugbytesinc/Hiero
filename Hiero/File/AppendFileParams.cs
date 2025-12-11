using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// File content append parameters.
/// </summary>
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
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token to interrupt the transaction submission process.
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
            Contents = ByteString.CopyFrom(Contents.ToArray())
        };
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Append File";
}
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
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> AppendFileAsync(this ConsensusClient client, AppendFileParams appendParameters, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(appendParameters, configure);
    }
}