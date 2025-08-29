using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Input parameters describing how to update a network file.
/// </summary>
public sealed class UpdateFileParams : TransactionParams, INetworkParams
{
    /// <summary>
    /// The address of the network file to update.
    /// </summary>
    public EntityId File { get; set; } = default!;
    /// <summary>
    /// The new expiration date for this file, it will be ignored
    /// if it is equal to or before the current expiration date value
    /// for this file.
    /// </summary>
    public ConsensusTimeStamp? Expiration { get; set; }
    /// <summary>
    /// If not null, a new description of the file.
    /// </summary>
    public string? Memo { get; set; }
    /// <summary>
    /// A descriptor of the keys required to sign transactions editing and 
    /// otherwise manipulating the contents of this file. Set to
    /// <code>null</code> to leave unchanged.
    /// </summary>
    public Endorsement[]? Endorsements { get; set; }
    /// <summary>
    /// Replace the contents of the file with these new contents.  Set to
    /// <code>null</code> to leave the existing content unchanged.
    /// </summary>
    public ReadOnlyMemory<byte>? Contents { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to replace the contents of this file.  Typically
    /// matchs all the Endorsements in the Endorsement array
    /// associated with this file.
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
    INetworkTransaction INetworkParams.CreateNetworkTransaction()
    {
        var result = new FileUpdateTransactionBody();
        if (File is null)
        {
            throw new ArgumentNullException(nameof(File), "File identifier is missing. Please check that it is not null.");
        }
        if (Endorsements is null &&
            Contents is null &&
            Memo is null &&
            Expiration is null)
        {
            throw new ArgumentException("The File Update parameters contain no update properties, it is blank.", nameof(UpdateFileParams));
        }
        result.FileID = new FileID(File);
        if (Endorsements is not null)
        {
            result.Keys = new KeyList(Endorsements);
        }
        if (Contents.HasValue)
        {
            result.Contents = ByteString.CopyFrom(Contents.Value.ToArray());
        }
        if (Memo is not null)
        {
            result.Memo = Memo;
        }
        if (Expiration.HasValue)
        {
            result.ExpirationTime = new Timestamp(Expiration.Value);
        }
        return result;
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "File Update";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class UpdateFileExtensions
{
    /// <summary>
    /// Updates the properties or contents of an existing file stored in the network.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client executing the file update.
    /// </param>
    /// <param name="updateParameters">
    /// Update parameters indicating the file to update and what properties such 
    /// as the access key or content that should be updated.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating the operation was successful.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<TransactionReceipt> UpdateFileAsync(this ConsensusClient client, UpdateFileParams updateParameters, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<TransactionReceipt>(updateParameters, configure);
    }
}