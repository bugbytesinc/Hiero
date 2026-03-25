// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for updating the metadata of one or more NFT instances.
/// </summary>
public sealed class UpdateNftsParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The Token ID of the NFT collection containing the instances to update.
    /// </summary>
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// The serial numbers of the NFT instances to update.
    /// Must contain between 1 and 10 entries.
    /// </summary>
    public IReadOnlyList<long> SerialNumbers { get; set; } = default!;
    /// <summary>
    /// The new metadata value to assign to the specified NFT instances.
    /// Must not exceed 100 bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Metadata { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method
    /// required to authorize the update. Must match the token's metadata key.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this
    /// transaction.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional cancellation token to interrupt the submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        if (SerialNumbers is null)
        {
            throw new ArgumentNullException(nameof(SerialNumbers), "The list of serial numbers must not be null.");
        }
        var result = new TokenUpdateNftsTransactionBody
        {
            Token = new TokenID(Token),
            Metadata = ByteString.CopyFrom(Metadata.Span)
        };
        result.SerialNumbers.AddRange(SerialNumbers);
        if (result.SerialNumbers.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SerialNumbers), "The list of serial numbers must not be empty.");
        }
        return result;
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Update NFT Metadata";
}
/// <summary>
/// Extension methods for updating NFT instance metadata on the network.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class UpdateNftsExtensions
{
    /// <summary>
    /// Updates the metadata of a single NFT instance.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the update.
    /// </param>
    /// <param name="nft">
    /// The NFT instance to update.
    /// </param>
    /// <param name="metadata">
    /// The new metadata to assign to the NFT instance. Must not exceed 100 bytes.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating a successful operation.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> UpdateNftMetadataAsync(this ConsensusClient client, Nft nft, ReadOnlyMemory<byte> metadata, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new UpdateNftsParams { Token = nft.Token, SerialNumbers = [nft.SerialNumber], Metadata = metadata }, configure);
    }
    /// <summary>
    /// Updates the metadata of one or more NFT instances.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the update.
    /// </param>
    /// <param name="updateParams">
    /// The parameters for updating the NFTs, including the token, serial numbers, and new metadata.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating a successful operation.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> UpdateNftsMetadataAsync(this ConsensusClient client, UpdateNftsParams updateParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(updateParams, configure);
    }
}
