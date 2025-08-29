using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Numerics;

namespace Hiero;
/// <summary>
/// Detailed description of a network file.
/// </summary>
public sealed record FileInfo
{
    /// <summary>
    /// The network address of the file.
    /// </summary>
    public EntityId File { get; private init; }
    /// <summary>
    /// A short description of the file.
    /// </summary>
    public string Memo { get; private init; }
    /// <summary>
    /// The size of the file in bytes (plus 30 extra for overhead).
    /// </summary>
    public long Size { get; private init; }
    /// <summary>
    /// The file expiration date at which it will be removed from 
    /// the network.  The date can be extended thru updates.
    /// </summary>
    public ConsensusTimeStamp Expiration { get; private init; }
    /// <summary>
    /// A descriptor of the all the keys required to sign transactions 
    /// editing and otherwise manipulating the contents of this file.
    /// </summary>
    public Endorsement[] Endorsements { get; private init; }
    /// <summary>
    /// Flag indicating the file has been deleted.
    /// </summary>
    public bool Deleted { get; private init; }
    /// <summary>
    /// Identification of the Ledger (Network) this 
    /// account information was retrieved from.
    /// </summary>
    public BigInteger Ledger { get; private init; }
    /// </summary>
    internal FileInfo(Response response)
    {
        var info = response.FileGetInfo.FileInfo;
        File = info.FileID.AsAddress();
        Memo = info.Memo;
        Size = info.Size;
        Expiration = info.ExpirationTime.ToConsensusTimeStamp();
        Endorsements = info.Keys?.ToEndorsements() ?? Array.Empty<Endorsement>();
        Deleted = info.Deleted;
        Ledger = new BigInteger(info.LedgerId.Span, true, true);
    }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class FileInfoExtensions
{
    /// <summary>
    /// Retrieves the contents of a file from the network.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="file">
    /// The address of the file contents to retrieve.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// The contents of the file as a blob of bytes.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static async Task<ReadOnlyMemory<byte>> GetFileContentAsync(this ConsensusClient client, EntityId file, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        var response = await Engine.QueryAsync(client, new FileGetContentsQuery { FileID = new FileID(file) }, cancellationToken, configure).ConfigureAwait(false);
        return response.FileGetContents.FileContents.Contents.Memory;
    }
    /// <summary>
    /// Retrieves the details regarding a file stored on the network.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="file">
    /// Payer of the file to query.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// The details of the network file, excluding content.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    public static async Task<FileInfo> GetFileInfoAsync(this ConsensusClient client, EntityId file, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        return new FileInfo(await Engine.QueryAsync(client, new FileGetInfoQuery { FileID = new FileID(file) }, cancellationToken, configure).ConfigureAwait(false));
    }
}