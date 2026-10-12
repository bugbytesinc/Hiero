// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Removes approved spending allowance(s) for specific NFTs, or revokes a
/// spender's approval to spend all of the owner's NFTs of a token class.
/// </summary>
/// <remarks>
/// Set exactly one of <see cref="SerialNumbers"/> or <see cref="Spender"/>.
/// The two remove different grants: serial numbers remove the allowances on
/// those NFTs, whoever holds them, while a spender loses only its blanket
/// approve-for-all, keeping any allowance it holds on specific serials.
/// </remarks>
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
    /// The Serial Numbers of the NFTs to revoke allowances from.  Leave
    /// <code>null</code> when revoking a <see cref="Spender"/>'s approval
    /// for all of the token's NFTs.
    /// </summary>
    public IReadOnlyList<long>? SerialNumbers { get; set; }
    /// <summary>
    /// The spender whose approval to spend all of the owner's NFTs of this
    /// token class is revoked.  Leave <code>null</code> when removing the
    /// allowances on specific <see cref="SerialNumbers"/>.
    /// </summary>
    /// <remarks>
    /// The network has no delete operation for this grant, so the SDK sends
    /// a crypto approve allowance transaction that sets the spender's
    /// approve-for-all to false.  Allowances the spender holds on specific
    /// serial numbers are not affected.
    /// </remarks>
    public EntityId? Spender { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to authorize the transaction.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this
    /// transaction.  The <see cref="Owner"/> account's key must sign,
    /// either here or through the context (for example when the owner
    /// is also the payer).
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional cancellation token.  Cancelling stops the client waiting;
    /// it cannot recall a transaction already sent.  Cancelled after the
    /// send, the outcome is unknown: settle it by its transaction id on a
    /// mirror node rather than resubmitting it.
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
        if (Spender is not null)
        {
            if (SerialNumbers is not null)
            {
                throw new ArgumentException("Set either SerialNumbers, to remove the allowances on those NFTs, or Spender, to revoke the spender's approval for all of the token's NFTs, but not both.", nameof(Spender));
            }
            if (Spender.IsNullOrNone())
            {
                throw new ArgumentException("The spender account cannot be None.", nameof(Spender));
            }
            var revokeApprovalForAll = new CryptoApproveAllowanceTransactionBody();
            revokeApprovalForAll.NftAllowances.Add(new Proto.NftAllowance
            {
                TokenId = new TokenID(Token),
                Owner = new AccountID(Owner),
                Spender = new AccountID(Spender),
                ApprovedForAll = false
            });
            return revokeApprovalForAll;
        }
        if (SerialNumbers is null)
        {
            throw new ArgumentNullException(nameof(SerialNumbers), "The list of serial numbers is missing. Please check that it is not null.");
        }
        var count = SerialNumbers.Count;
        if (count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SerialNumbers), "The list of serial numbers must contain at least one serial number to remove.");
        }
        var nftRemoveAllowance = new NftRemoveAllowance()
        {
            TokenId = new TokenID(Token),
            Owner = new AccountID(Owner),
        };
        var serialNumbers = nftRemoveAllowance.SerialNumbers;
        if (serialNumbers.Capacity < count)
        {
            serialNumbers.Capacity = count;
        }
        for (var i = 0; i < count; i++)
        {
            serialNumbers.Add(SerialNumbers[i]);
        }
        var result = new CryptoDeleteAllowanceTransactionBody();
        result.NftAllowances.Capacity = 1;
        result.NftAllowances.Add(nftRemoveAllowance);
        return result;
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => Spender is null ? "Delete Allowance" : "Revoke Approval For All";
}
/// <summary>
/// Extension methods for revoking NFT allowances.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RevokeNftAllowanceExtensions
{
    /// <summary>
    /// Removes approved spending allowance(s) for specific NFTs, or revokes
    /// a spender's approval to spend all of the owner's NFTs of a token
    /// class (see <see cref="RevokeNftAllowanceParams"/>).
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the revocation.
    /// </param>
    /// <param name="revokeParams">
    /// The parameters containing the token id, owner, and either the serial
    /// numbers or the spender.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating success, or an exception is thrown.
    /// </returns>
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the transaction. Only a code about the transaction itself proves it did not execute; any other code leaves the outcome unknown, so settle it by its transaction id on a mirror node rather than resubmitting it (see <see cref="PrecheckException"/>).</exception>
    /// <exception cref="ConsensusException">If the outcome could not be confirmed: the transaction may have executed, so settle it by its transaction id on a mirror node rather than resubmitting it.</exception>
    /// <exception cref="TransactionException">If the network rejected the revoke request as invalid or had missing data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> RevokeNftAllowanceAsync(this ConsensusClient client, RevokeNftAllowanceParams revokeParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(revokeParams, configure);
    }
}
