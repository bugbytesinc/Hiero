using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// File creation parameters.
/// </summary>
public sealed class CreateFileParams : TransactionParams<FileReceipt>, INetworkParams<FileReceipt>
{
    /// <summary>
    /// Original expiration date for the file, fees will be charged as appropriate.
    /// </summary>
    public ConsensusTimeStamp Expiration { get; set; }
    /// <summary>
    /// A descriptor of the keys required to sign transactions editing and 
    /// otherwise manipulating the contents of this file.  Only one key
    /// is required to sign the transaction to delete the file.
    /// </summary>
    public Endorsement[] Endorsements { get; set; } = default!;
    /// <summary>
    /// The initial contents of the file.
    /// </summary>
    public ReadOnlyMemory<byte> Contents { get; set; }
    /// <summary>
    /// A short description of the file.
    /// </summary>
    public string Memo { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to create to this file.  Typically matches the
    /// Endorsements associated with this file.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token to interrupt the file creation.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<FileReceipt>.CreateNetworkTransaction()
    {
        if (Endorsements is null)
        {
            throw new ArgumentOutOfRangeException(nameof(Endorsements), "Endorsements are required.");
        }
        return new FileCreateTransactionBody()
        {
            ExpirationTime = new Timestamp(Expiration),
            Keys = new KeyList(Endorsements),
            Contents = ByteString.CopyFrom(Contents.ToArray()),
            Memo = Memo ?? ""
        };
    }
    FileReceipt INetworkParams<FileReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new FileReceipt(transactionId, receipt);
    }
    string INetworkParams<FileReceipt>.OperationDescription => "Create File";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class CreateFileExtensions
{
    /// <summary>
    /// Creates a new file with the given content.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client executing the file ceate.
    /// </param>
    /// <param name="createParameters">
    /// File creation parameters specifying contents and ownership of the file.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt with a description of the newly created file.
    /// and record information.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<FileReceipt> CreateFileAsync(this ConsensusClient client, CreateFileParams createParameters, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(createParameters, configure);
    }
}