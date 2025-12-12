using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for Burning One or More NFT instances.
/// </summary>
public sealed class BurnNftParams : TransactionParams<TokenReceipt>, INetworkParams<TokenReceipt>
{
    /// <summary>
    /// The Token ID of the NFT to burn.
    /// </summary>
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// The Serial Numbers of the NFTs to burn.
    /// </summary>
    public IReadOnlyList<long> SerialNumbers { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to authorize the transfers.  Typically matches the
    /// Endorsement assigned to sending accounts.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token that interrupt the token
    /// submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    /// <summary>
    /// Creates a Crypto Transfer Transaction Body from these
    /// parameters.
    /// </summary>
    /// <returns>
    /// CryptoTransferTransactionBody implementing INetworkTransaction
    /// </returns>
    INetworkTransaction INetworkParams<TokenReceipt>.CreateNetworkTransaction()
    {
        if (SerialNumbers is null)
        {
            throw new ArgumentOutOfRangeException(nameof(SerialNumbers), "The list of serial numbers must not be null.");
        }
        var result = new TokenBurnTransactionBody
        {
            Token = new TokenID(Token)
        };
        result.SerialNumbers.AddRange(SerialNumbers);
        if (result.SerialNumbers.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SerialNumbers), "The list of serial numbers must not be empty.");
        }
        return result;
    }
    TokenReceipt INetworkParams<TokenReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TokenReceipt(transactionId, receipt);
    }
    string INetworkParams<TokenReceipt>.OperationDescription => "Burn NFT";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class BurnNftExtensions
{
    /// <summary>
    /// Destroys an nft (NFT) instance.
    /// </summary>
    /// <param name="asset">
    /// The identifier of the nft to destroy.
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
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission, for example if the nft is already deleted.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TokenReceipt> BurnNftAsync(this ConsensusClient client, Nft asset, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new BurnNftParams { Token = asset.Token, SerialNumbers = [asset.SerialNumber] }, configure);
    }
    /// <summary>
    /// Destroys multiple nft (NFT) instances.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the burn.
    /// </param>
    /// <param name="burnParams">
    /// The Parameters for burning the NFTs, including the list of NFTs to burn.
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
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission, for example if the nft is already deleted.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TokenReceipt> BurnNftsAsync(this ConsensusClient client, BurnNftParams burnParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(burnParams, configure);
    }
}