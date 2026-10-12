// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for Minting (Creating) a new NFT for
/// a given class of NFTs.
/// </summary>
/// <example>
/// Mint three NFTs with different JSON metadata in one transaction. The
/// receipt's <c>SerialNumbers</c> lists the newly issued serials in order:
/// <code source="../../../samples/CreateNft/Program.cs" region="MintNftBatch" language="csharp"/>
/// </example>
public sealed class MintNftParams : TransactionParams<NftMintReceipt>, INetworkParams<NftMintReceipt>
{
    /// <summary>
    /// The existing token (NFT class) under which to mint the new serials.
    /// </summary>
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// The list of Metadata entries, to be associated with the newly
    /// created NFTs.  Each metadata entry will correspond to
    /// a newly created NFT.  The metadata value may be empty.
    /// </summary>
    public IReadOnlyList<ReadOnlyMemory<byte>> Metadata { get; set; } = default!;
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
    /// Optional cancellation token.  Cancelling stops the client waiting;
    /// it cannot recall a transaction already sent.  Cancelled after the
    /// send, the outcome is unknown: settle it by its transaction id on a
    /// mirror node rather than resubmitting it.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    /// <summary>
    /// Creates a Token Mint Transaction Body from these
    /// parameters.
    /// </summary>
    /// <returns>
    /// TokenMintTransactionBody implementing INetworkTransaction
    /// </returns>
    INetworkTransaction INetworkParams<NftMintReceipt>.CreateNetworkTransaction()
    {
        if (Metadata is null)
        {
            throw new ArgumentNullException(nameof(Metadata), "The metadata list cannot be null.");
        }
        var result = new TokenMintTransactionBody
        {
            Token = new TokenID(Token)
        };
        var metadataItems = result.Metadata;
        var count = Metadata.Count;
        if (metadataItems.Capacity < count)
        {
            metadataItems.Capacity = count;
        }
        for (var i = 0; i < count; i++)
        {
            metadataItems.Add(ByteString.CopyFrom(Metadata[i].Span));
        }
        return result;
    }
    NftMintReceipt INetworkParams<NftMintReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new NftMintReceipt(transactionId, receipt);
    }
    string INetworkParams<NftMintReceipt>.OperationDescription => "Mint NFT";
}
/// <summary>
/// Extension methods for minting new NFT instances on the network.
/// </summary>
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
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the mint request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/NftSnippets.cs" region="MintNftSingle" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<NftMintReceipt> MintNftAsync(this ConsensusClient client, EntityId token, ReadOnlyMemory<byte> metadata, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new MintNftParams { Token = token, Metadata = [metadata] }, configure);
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
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the mint request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/CreateNft/Program.cs" region="MintNftBatch" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<NftMintReceipt> MintNftsAsync(this ConsensusClient client, MintNftParams mintParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(mintParams, configure);
    }
}
