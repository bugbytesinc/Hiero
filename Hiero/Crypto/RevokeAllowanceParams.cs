using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Removes approved spending allowance(s) for specific NFTs.
/// </summary>
public sealed class RevokeNftAllowanceParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// The ID of the owner of the NFTs 
    /// </summary>
    public EntityId Owner { get; set; } = default!;
    /// <summary>
    /// The Token ID of the NFT class to remove allowances.
    /// </summary>
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// The Serial Numbers of the NFTs to revoke allowances from.
    /// </summary>
    public IEnumerable<long> SerialNumbers { get; set; } = default!;
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to authorize the transaction.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional Cancellation token that interrupt the allowance
    /// update process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        if (Token is null)
        {
            throw new ArgumentNullException(nameof(Token), "Token is missing. Please check that it is not null.");
        }
        if (Owner is null)
        {
            throw new ArgumentNullException(nameof(Owner), "Owning account address is missing. Please check that it is not null.");
        }
        if (SerialNumbers is null)
        {
            throw new ArgumentNullException(nameof(SerialNumbers), "The list of serial numbers is missing. Please check that it is not null.");
        }
        var nftRemoveAllowance = new NftRemoveAllowance()
        {
            TokenId = new TokenID(Token),
            Owner = new AccountID(Owner),
        };
        nftRemoveAllowance.SerialNumbers.AddRange(SerialNumbers);
        var result = new CryptoDeleteAllowanceTransactionBody();
        result.NftAllowances.Add(nftRemoveAllowance);
        if (result.NftAllowances.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SerialNumbers), "The list of serial must contain at least one serial number to remove.");
        }
        return result;
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Delete Allowance";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RevokeNftAllowanceExtensions
{
    /// <summary>
    /// Removes approved spending allowance(s) for specific NFTs.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="revokeParams">
    /// The parameters containing the token id, owner and serial numbers
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A TransactionId record indicating success, or an exception is thrown.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> RevokeNftAllowancesAsync(this ConsensusClient client, RevokeNftAllowanceParams revokeParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(revokeParams, configure);
    }
}