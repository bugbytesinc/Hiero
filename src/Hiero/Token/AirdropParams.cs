// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Hiero;
/// <summary>
/// Transaction Parameters for airdropping tokens to one or more recipient accounts.
/// </summary>
/// <remarks>
/// An airdrop distributes tokens from a sender to recipients. If a recipient
/// has available auto-association slots or is already associated with the token,
/// the transfer completes immediately. Otherwise, a pending airdrop is created
/// that the recipient must claim with a <c>ClaimAirdrop</c> transaction.
/// </remarks>
/// <example>
/// Airdrop the same token to multiple recipients in a single atomic transaction:
/// <code source="../../../samples/DocSnippets/TokenSnippets.cs" region="AirdropMulti" language="csharp"/>
/// </example>
public sealed class AirdropParams : TransactionParams<TransactionReceipt>, INetworkParams<TransactionReceipt>
{
    /// <summary>
    /// A list of fungible token transfers for the airdrop.
    /// Each entry specifies the token, the sending account (negative amount),
    /// and the receiving account (positive amount). Entries must balance per token.
    /// </summary>
    public IEnumerable<TokenTransfer>? TokenTransfers { get; set; }
    /// <summary>
    /// A list of NFT transfers for the airdrop.
    /// Each entry specifies the NFT, the sending account, and the receiving account.
    /// </summary>
    public IEnumerable<NftTransfer>? NftTransfers { get; set; }
    /// <summary>
    /// Additional private key, keys or signing callback method
    /// required to authorize the airdrop. Typically matches the
    /// endorsement assigned to the sending account(s).
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
        var result = new TokenAirdropTransactionBody();
        if (TokenTransfers is not null)
        {
            var netTransfers = new Dictionary<EntityId, (long sum, TokenTransferList list)>();
            foreach (var xfer in TokenTransfers)
            {
                if (xfer.Token.IsNullOrNone())
                {
                    throw new ArgumentException("Token", "The list of token transfers cannot contain a null or empty Token value.");
                }
                if (xfer.Account.IsNullOrNone())
                {
                    throw new ArgumentException(nameof(xfer.Account), "The list of token transfers cannot contain a null or empty account value.");
                }
                if (xfer.Amount == 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(xfer.Amount), $"The amount to airdrop to/from {xfer.Account} must be non-zero.");
                }
                ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(netTransfers, xfer.Token, out bool exists);
                if (!exists)
                {
                    entry.list = new TokenTransferList
                    {
                        Token = new TokenID(xfer.Token)
                    };
                }
                entry.sum += xfer.Amount;
                entry.list.Transfers.Add(new AccountAmount(xfer.Account, xfer.Amount, xfer.Delegated));
            }
            foreach (var record in netTransfers)
            {
                if (record.Value.sum != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(TokenTransfers), $"The sum of token sends and receives for {record.Key} does not balance.");
                }
                result.TokenTransfers.Add(record.Value.list);
            }
        }
        if (NftTransfers is not null)
        {
            var nftXferList = new Dictionary<Nft, Proto.NftTransfer>();
            foreach (var xfer in NftTransfers)
            {
                if (xfer.Nft.IsNullOrNone())
                {
                    throw new ArgumentException("Nft", "The list of NFT transfers cannot contain a null or empty NFT address.");
                }
                if (xfer.Sender.IsNullOrNone())
                {
                    throw new ArgumentException(nameof(xfer.Sender), "The list of NFT transfers cannot contain a null or empty sender account value.");
                }
                if (xfer.Receiver.IsNullOrNone())
                {
                    throw new ArgumentException(nameof(xfer.Receiver), "The list of NFT transfers cannot contain a null or empty receiver account value.");
                }
                if (!nftXferList.TryAdd(xfer.Nft, new Proto.NftTransfer
                {
                    SenderAccountID = new AccountID(xfer.Sender),
                    ReceiverAccountID = new AccountID(xfer.Receiver),
                    SerialNumber = xfer.Nft.SerialNumber,
                    IsApproval = xfer.Delegated
                }))
                {
                    throw new ArgumentException(nameof(xfer.Nft), "The list of NFT transfers cannot contain the same NFT in multiple transfers at once.");
                }
            }
            var netNftTransfers = new Dictionary<EntityId, TokenTransferList>();
            foreach (var record in nftXferList)
            {
                ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(netNftTransfers, record.Key, out bool exists)!;
                if (!exists)
                {
                    entry = new TokenTransferList
                    {
                        Token = new TokenID(record.Key)
                    };
                }
                entry.NftTransfers.Add(record.Value);
            }
            result.TokenTransfers.AddRange(netNftTransfers.Values);
        }
        if (result.TokenTransfers.Count == 0)
        {
            throw new ArgumentException(nameof(AirdropParams), "Both token and NFT airdrop lists are null or empty. At least one must include transfers.");
        }
        return result;
    }
    TransactionReceipt INetworkParams<TransactionReceipt>.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams<TransactionReceipt>.OperationDescription => "Airdrop";
}
/// <summary>
/// Extension methods for airdropping tokens and NFTs on the network.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AirdropExtensions
{
    /// <summary>
    /// Airdrops a fungible token from one account to another.
    /// </summary>
    /// <remarks>
    /// If the recipient is already associated with the token or has auto-association
    /// slots available, the transfer completes immediately. Otherwise, a pending
    /// airdrop is created that the recipient must claim.
    /// </remarks>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the airdrop.
    /// </param>
    /// <param name="token">
    /// The fungible token type to airdrop.
    /// </param>
    /// <param name="sender">
    /// The account sending the tokens.
    /// </param>
    /// <param name="receiver">
    /// The account receiving the tokens.
    /// </param>
    /// <param name="amount">
    /// The amount of tokens to airdrop.
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
    /// <example>
    /// <code source="../../../samples/DocSnippets/TokenSnippets.cs" region="AirdropToken" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> AirdropTokenAsync(this ConsensusClient client, EntityId token, EntityId sender, EntityId receiver, long amount, Action<IConsensusContext>? configure = null)
    {
        if (amount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "The amount to airdrop must be greater than zero.");
        }
        return client.ExecuteAsync(new AirdropParams
        {
            TokenTransfers =
            [
                new TokenTransfer(token, sender, -amount),
                new TokenTransfer(token, receiver, amount)
            ]
        }, configure);
    }
    /// <summary>
    /// Airdrops an NFT from one account to another.
    /// </summary>
    /// <remarks>
    /// If the recipient is already associated with the token or has auto-association
    /// slots available, the transfer completes immediately. Otherwise, a pending
    /// airdrop is created that the recipient must claim.
    /// </remarks>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the airdrop.
    /// </param>
    /// <param name="nft">
    /// The NFT instance to airdrop.
    /// </param>
    /// <param name="sender">
    /// The account sending the NFT.
    /// </param>
    /// <param name="receiver">
    /// The account receiving the NFT.
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
    /// <example>
    /// <code source="../../../samples/DocSnippets/TokenSnippets.cs" region="AirdropNft" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> AirdropNftAsync(this ConsensusClient client, Nft nft, EntityId sender, EntityId receiver, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(new AirdropParams
        {
            NftTransfers = [new NftTransfer(nft, sender, receiver)]
        }, configure);
    }
    /// <summary>
    /// Airdrops one or more tokens and/or NFTs using detailed parameters.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the airdrop.
    /// </param>
    /// <param name="airdropParams">
    /// The airdrop parameters containing the token and NFT transfers.
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
    /// <example>
    /// <code source="../../../samples/DocSnippets/TokenSnippets.cs" region="AirdropMulti" language="csharp"/>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<TransactionReceipt> AirdropAsync(this ConsensusClient client, AirdropParams airdropParams, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteAsync(airdropParams, configure);
    }
}
