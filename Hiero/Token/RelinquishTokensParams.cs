using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for relinquishing one or more tokens, returning
/// their full balance (or specific NFT instances) to the token treasury.
/// </summary>
public sealed class RelinquishTokensParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// Optional account holding the tokens to relinquish.
    /// If not set, the transaction payer is treated as the token owner.
    /// If set, this account must sign the transaction.
    /// </summary>
    public EntityId? Owner { get; set; }
    /// <summary>
    /// Fungible token types to relinquish. The full balance of each token
    /// will be returned to the token treasury.
    /// </summary>
    public IEnumerable<EntityId>? Tokens { get; set; }
    /// <summary>
    /// Specific NFT instances to reject. Each NFT will be returned
    /// to the token treasury.
    /// </summary>
    public IEnumerable<Nft>? Nfts { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method
    /// required to authorize the relinquishment. Typically matches the
    /// endorsement assigned to the owner account if it is not already
    /// the payer for the transaction.
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
        var result = new TokenRejectTransactionBody();
        if (!Owner.IsNullOrNone())
        {
            result.Owner = new AccountID(Owner);
        }
        if (Tokens is not null)
        {
            foreach (var token in Tokens)
            {
                if (token.IsNullOrNone())
                {
                    throw new ArgumentOutOfRangeException(nameof(Tokens), "The list of fungible tokens cannot contain an empty or null token address.");
                }
                result.Rejections.Add(new TokenReference { FungibleToken = new TokenID(token) });
            }
        }
        if (Nfts is not null)
        {
            foreach (var nft in Nfts)
            {
                if (nft.IsNullOrNone())
                {
                    throw new ArgumentOutOfRangeException(nameof(Nfts), "The list of NFTs cannot contain an empty or null NFT.");
                }
                result.Rejections.Add(new TokenReference { Nft = new NftID(nft) });
            }
        }
        if (result.Rejections.Count == 0)
        {
            throw new ArgumentException("At least one fungible token or NFT must be specified for rejection.", nameof(Tokens));
        }
        return result;
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Relinquish Tokens";
}
/// <summary>
/// Extension methods for relinquishing tokens and NFTs back to the treasury.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RelinquishTokenExtensions
{
    /// <summary>
    /// Relinquishes a fungible token, returning the full balance to the token treasury.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the relinquishment.
    /// </param>
    /// <param name="token">
    /// The fungible token type to relinquish.
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
    public static Task<TransactionReceipt> RelinquishTokenAsync(this ConsensusClient client, EntityId token, Action<IConsensusContext>? configure = null)
    {
        if (token.IsNullOrNone())
        {
            throw new ArgumentNullException(nameof(token), "Token is missing. Please check that it is not null or empty.");
        }
        return client.ExecuteAsync(new RelinquishTokensParams { Tokens = [token] }, configure);
    }
    /// <summary>
    /// Relinquishes a specific NFT instance, returning it to the token treasury.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the rejection.
    /// </param>
    /// <param name="nft">
    /// The NFT instance to reject.
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
    public static Task<TransactionReceipt> RelinquishNftAsync(this ConsensusClient client, Nft nft, Action<IConsensusContext>? configure = null)
    {
        if (nft.IsNullOrNone())
        {
            throw new ArgumentNullException(nameof(nft), "NFT is missing. Please check that it is not null or empty.");
        }
        return client.ExecuteAsync(new RelinquishTokensParams { Nfts = [nft] }, configure);
    }
    /// <summary>
    /// Relinquishes one or more tokens and/or NFTs using detailed parameters.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the relinquishment.
    /// </param>
    /// <param name="relinquishParams">
    /// The rejection parameters, including optional owner, fungible tokens, and NFTs to reject.
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
    public static Task<TransactionReceipt> RelinquishAsync(this ConsensusClient client, RelinquishTokensParams relinquishParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(relinquishParams, configure);
    }
}
