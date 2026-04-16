// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for Minting Tokens to the treasury account.
/// </summary>
/// <example>
/// Mint additional units of a fungible token into its treasury. The supply key
/// must sign the transaction:
/// <code source="../../../samples/DocSnippets/TokenSnippets.cs" region="MintToken" language="csharp"/>
/// </example>
public sealed class MintTokenParams : TransactionParams<TokenReceipt>, INetworkParams<TokenReceipt>
{
    /// <summary>
    /// The identifier of the fungible token to mint.
    /// </summary>
    public EntityId Token { get; set; } = default!;
    /// <summary>
    /// The Amount of fungible tokens to mint, specified in the smallest denomination.
    /// </summary>
    public ulong Amount { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method 
    /// required to authorize the mint.  Typically matches the
    /// Endorsement assigned to the supply key for the token if it is not already
    /// set as the payer for the transaction.
    /// </summary>
    /// <remarks>
    /// Keys/callbacks added here will be combined with those already
    /// identified in the client object's context when signing this 
    /// transaction to change the state of this account.
    /// </remarks>
    public Signatory? Signatory { get; set; }
    /// <summary>
    /// Optional cancellation token to interrupt the token
    /// minting submission process.
    /// </summary>
    public CancellationToken? CancellationToken { get; set; }
    INetworkTransaction INetworkParams<TokenReceipt>.CreateNetworkTransaction()
    {
        if (Amount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(Amount), "The token amount must be greater than zero.");
        }
        return new TokenMintTransactionBody
        {
            Token = new TokenID(Token),
            Amount = Amount
        };
    }
    TokenReceipt INetworkParams<TokenReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TokenReceipt(transactionId, receipt);
    }
    string INetworkParams<TokenReceipt>.OperationDescription => "Mint Tokens";
}
/// <summary>
/// Extension methods for minting fungible tokens to the treasury.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class MintTokenExtensions
{
    /// <summary>
    /// Adds token coins to the treasury.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the mint.
    /// </param>
    /// <param name="token">
    /// The identifier of the token to increase.
    /// </param>
    /// <param name="amount">
    /// The amount of tokens to add, denoted in the smallest denomination.
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
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission, for example, if the token is already deleted.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/TokenSnippets.cs" region="MintToken" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TokenReceipt> MintTokenAsync(this ConsensusClient client, EntityId token, ulong amount, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new MintTokenParams { Token = token, Amount = amount }, configure);
    }
    /// <summary>
    /// Adds token coins to the treasury.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the mint.
    /// </param>
    /// <param name="mintParams">
    /// The details identifying the token and amount to mint.
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
    /// <exception cref="PrecheckException">If the gateway node rejected the request upon submission, for example, if the token is already deleted.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the request as invalid or had missing data.</exception>
    /// <example>
    /// <code source="../../../samples/DocSnippets/TokenSnippets.cs" region="MintToken" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TokenReceipt> MintTokenAsync(this ConsensusClient client, MintTokenParams mintParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(mintParams, configure);
    }
}