using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;
/// <summary>
/// Transaction Parameters for Minting (Creating) a new NFT for
/// a given class of NFTs.
/// </summary>
public sealed class MintNftParams : TransactionParams, INetworkParams
{
    /// <summary>
    /// The Token ID of the NFT class to create.
    /// </summary>
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// An array of Metadata, to be associated with the newly
    /// created NFTs.  Each metadata entry will correspond to
    /// a newly created NFT.  The metadata value may be empty.
    /// </summary>
    public IEnumerable<ReadOnlyMemory<byte>> Metadata { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to authorize the creation.  Typically matches the
    /// Endorsement assigned to NFT's supply key.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to create the NFT.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token that interrupt the NFT
    /// minting process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    /// <summary>
    /// Creates a Crypto Transfer Transaction Body from these
    /// parameters.
    /// </summary>
    /// <returns>
    /// TokenMintTransactionBody implementing INetworkTransaction
    /// </returns>
    INetworkTransaction INetworkParams.CreateNetworkTransaction()
    {
        var result = new TokenMintTransactionBody
        {
            Token = new TokenID(Token)
        };
        result.Metadata.AddRange(Metadata.Select(m => ByteString.CopyFrom(m.Span)));
        return result;
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new NftMintReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "Mint NFT";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class MintNftExtensions
{
    /// <summary>
    /// Creates (Mints) a new Non-Fungible Token (NFTs) under the specified token definition.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the mint.
    /// </param>
    /// <param name="token">
    /// The identifier of the token type of NFTs to mint.
    /// </param>
    /// <param name="metadata">
    /// The Metadata associated with the newly created NFT.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A mint transaction receipt indicating a successful operation.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission, for example of the token is already deleted.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<NftMintReceipt> MintNftAsync(this ConsensusClient client, EntityId token, ReadOnlyMemory<byte> metadata, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<NftMintReceipt>(new MintNftParams { Token = token, Metadata = [metadata] }, configure);
    }
    /// <summary>
    /// Creates (Mints) new Non-Fungible Tokens (NFTs) under the specified token definition.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the mint.
    /// </param>
    /// <param name="mintParams">
    /// The Parameters for minting the NFTs, including the metadata
    /// to be associated with each minted NFT.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A mint transaction receipt indicating a successful operation.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission, for example of the token is already deleted.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<NftMintReceipt> MintNftsAsync(this ConsensusClient client, MintNftParams mintParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<NftMintReceipt>(mintParams, configure);
    }
}