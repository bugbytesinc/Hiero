// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Allowance Creation and Adjustment Parameters
/// </summary>
/// <remarks>
/// Also the revocation path for <b>crypto and token</b> allowances: HAPI has
/// no dedicated delete op for these, so to revoke a crypto or token allowance
/// re-submit via <see cref="AllowanceExtensions.AllocateAllowanceAsync"/> with
/// the same owner/spender and <c>Amount = 0</c>. NFT allowances are the
/// exception — they are revoked via
/// <see cref="RevokeNftAllowanceExtensions.RevokeNftAllowanceAsync"/>, which
/// submits a distinct <c>CryptoDeleteAllowance</c> HAPI transaction.
/// </remarks>
/// <example>
/// Grant another account the right to spend up to a fixed amount of HBAR
/// from the owning account (who must be the transaction Payer):
/// <code source="../../../samples/DocSnippets/CryptoSnippets.cs" region="AllocateAllowance" language="csharp"/>
/// </example>
public sealed class AllowanceParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// A list of accounts and allocated allowances that 
    /// each account may sign transactions moving crypto
    /// out of this account up to the specified limit.
    /// </summary>
    public IReadOnlyList<CryptoAllowance>? CryptoAllowances { get; set; }
    /// <summary>
    /// A list of accounts and allocated allowances that 
    /// each account may sign transactions moving tokens
    /// out of this account up to the specified limit.
    /// </summary>
    public IReadOnlyList<TokenAllowance>? TokenAllowances { get; set; }
    /// <summary>
    /// A list of accounts and allocated allowances that 
    /// each account may sign transactions moving NFTs
    /// out of this account up to the specified limit.
    /// </summary>
    public IReadOnlyList<NftAllowance>? NftAllowances { get; set; }
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
    /// Optional Cancellation token that can interrupt the allowance
    /// update process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TransactionReceipt>.CreateNetworkTransaction()
    {
        var result = new CryptoApproveAllowanceTransactionBody();
        if (CryptoAllowances is { Count: > 0 })
        {
            foreach (var allowance in CryptoAllowances)
            {
                if (allowance.Amount < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(allowance.Amount), "The allowance amount must be greater than or equal to zero.");
                }
                result.CryptoAllowances.Add(new Proto.CryptoAllowance
                {
                    Owner = new AccountID(allowance.Owner),
                    Spender = new AccountID(allowance.Spender),
                    Amount = allowance.Amount
                });
            }
        }
        if (TokenAllowances is { Count: > 0 })
        {
            foreach (var allowance in TokenAllowances)
            {
                if (allowance.Amount < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(allowance.Amount), "The allowance amount must be greater than or equal to zero.");
                }
                result.TokenAllowances.Add(new Proto.TokenAllowance
                {
                    TokenId = new TokenID(allowance.Token),
                    Owner = new AccountID(allowance.Owner),
                    Spender = new AccountID(allowance.Spender),
                    Amount = allowance.Amount
                });
            }
        }
        if (NftAllowances is { Count: > 0 })
        {
            foreach (var allowance in NftAllowances)
            {
                var nftAllowance = new Proto.NftAllowance
                {
                    TokenId = new TokenID(allowance.Token),
                    Owner = new AccountID(allowance.Owner),
                    Spender = new AccountID(allowance.Spender)
                };
                if (allowance.SerialNumbers == null)
                {
                    nftAllowance.ApprovedForAll = true;
                }
                else
                {
                    nftAllowance.SerialNumbers.AddRange(allowance.SerialNumbers);
                }
                if (!allowance.DelegatingSpender.IsNullOrNone())
                {
                    nftAllowance.DelegatingSpender = new AccountID(allowance.DelegatingSpender);
                }
                result.NftAllowances.Add(nftAllowance);
            }
        }
        if (result.CryptoAllowances.Count == 0 && result.TokenAllowances.Count == 0 && result.NftAllowances.Count == 0)
        {
            throw new ArgumentException(nameof(AllowanceParams), "Both crypto, token and NFT allowance lists are null or empty.  At least one must include a net amount.");
        }
        return result;
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Create Allowance";
}
/// <summary>
/// Extension methods for allocating crypto, token, and NFT allowances.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AllowanceExtensions
{
    /// <summary>
    /// Creates approved allowance(s) allowing the designated
    /// spender to spend crypto and tokens from the originating
    /// account.  Presently the owning account must be the
    /// Payer (operator) paying for this transaction when
    /// submitted to the network.
    /// </summary>
    /// <remarks>
    /// This method is also the revocation path for crypto and token
    /// allowances — resubmit with <c>Amount = 0</c> to revoke, since
    /// HAPI has no dedicated <c>Revoke*</c> transaction for those.
    /// NFT allowances use
    /// <see cref="RevokeNftAllowanceExtensions.RevokeNftAllowanceAsync"/>
    /// instead.
    /// </remarks>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the update.
    /// </param>
    /// <param name="allowanceParams">
    /// Parameters containing the list of allowances to create.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction receipt indicating success, or an exception is thrown.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/CryptoSnippets.cs" region="AllocateAllowance" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> AllocateAllowanceAsync(this ConsensusClient client, AllowanceParams allowanceParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(allowanceParams, configure);
    }
}