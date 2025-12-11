using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Removes the holdings of given asset from the associated 
/// account and destorys them. Must be signed by 
/// the confiscate/wipe admin key.
/// </summary>
public sealed class ConfiscateNftParams : TransactionParams<TokenReceipt>, INetworkParams<TokenReceipt>
{
    /// <summary>
    /// The Token ID of the NFT(s) to confiscate and destroy.
    /// </summary>
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// The Holder Holding the NFT(s) to confiscate and destroy.
    /// </summary>
    public EntityId Account { get; set; } = default!;
    /// <summary>
    /// The Serial Numbers of the NFTs to confiscate and destroy.
    /// </summary>
    public IReadOnlyList<long> SerialNumbers { get; set; } = default!;
    /// <summary>
    /// Optional Additional private key, keys or signing callback method 
    /// required to authorize the confiscation/wipe. Usefull when
    /// the wipe key is not the same as the payer key.
    /// </summary>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token to interrupt the transaction
    /// submission process if needed.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    /// <summary>
    /// Creates the appropriate Hedera tansaction Body from these parameters.
    /// </summary>
    /// <returns>
    /// TokenWipeAccountTransactionBody implementing INetworkTransaction
    /// </returns>
    INetworkTransaction INetworkParams<TokenReceipt>.CreateNetworkTransaction()
    {
        if (Token.IsNullOrNone())
        {
            throw new ArgumentOutOfRangeException(nameof(Token), "The asset token type to confiscate is missing.");
        }
        if (Account.IsNullOrNone())
        {
            throw new ArgumentOutOfRangeException(nameof(Account), "The account Addresss can not be empty or None.  Please provide a valid value.");
        }
        var result = new TokenWipeAccountTransactionBody
        {
            Token = new TokenID(Token),
            Account = new AccountID(Account),
        };
        result.SerialNumbers.AddRange(SerialNumbers);
        return result;
    }
    TokenReceipt INetworkParams<TokenReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TokenReceipt(transactionId, receipt);
    }
    string INetworkParams<TokenReceipt>.OperationDescription => "Confiscate NFT";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConfiscateNftExtensions
{
    /// <summary>
    /// Removes given NFT from the holding
    /// account and destorys it. Must be signed by 
    /// the confiscate/wipe admin key.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the confiscation.
    /// </param>
    /// <param name="nft">
    /// The identifier of the NFT to confiscate and destroy.
    /// </param>
    /// <param name="account">
    /// The account holding the NFT to confiscate and destroy.
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
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission, for example of the nft is already deleted.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TokenReceipt> ConfiscateNftAsync(this ConsensusClient client, Nft nft, EntityId account, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new ConfiscateNftParams { Token = nft.Token, Account = account, SerialNumbers = [nft.SerialNumber] }, configure);
    }
    /// <summary>
    /// Confiscates and Destroys multiple nft (NFT) instances.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the confiscation.
    /// </param>
    /// <param name="confiscateParams">
    /// The Parameters for confiscating the NFTs, including the 
    /// list of NFTs to confiscate and destroy.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A token transaction receipt indicating a successful operation.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission, for example of the nft is already deleted.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TokenReceipt> ConfiscateNftsAsync(this ConsensusClient client, ConfiscateNftParams confiscateParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(confiscateParams, configure);
    }
}