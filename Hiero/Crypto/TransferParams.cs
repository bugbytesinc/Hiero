using Hiero.Implementation;
using Proto;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Hiero;
/// <summary>
/// Supports complex atomic multi-party multi-token and crypto transfers requests.
/// Can support multi-account crypto transfers and/or multi-account token transfers
/// in the same transaction.  The crypto transfer list or token transfer list may
/// be null if not used, however at least one transfer of some type must be defined 
/// to be valid.  
/// </summary>
public sealed class TransferParams : TransactionParams, INetworkParams
{
    /// <summary>
    /// Transfer tinybars from an arbitray set of accounts to
    /// another arbitrary set of accounts.
    /// </summary>
    public IEnumerable<CryptoTransfer>? CryptoTransfers { get; set; }
    /// <summary>
    /// A list of tokens transfered from an arbitray set of accounts to
    /// another arbitrary set of accounts.
    /// </summary>
    public IEnumerable<TokenTransfer>? TokenTransfers { get; set; }
    /// <summary>
    /// A list of NFTs transfered from an arbitray set of accounts to
    /// another arbitrary set of accounts.
    /// </summary>
    public IEnumerable<NftTransfer>? NftTransfers { get; set; }
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
    INetworkTransaction INetworkParams.CreateNetworkTransaction()
    {
        var result = new CryptoTransferTransactionBody();
        if (CryptoTransfers is not null)
        {
            long sum = 0;
            var netRequests = new Dictionary<EntityId, (long Amount, bool Delegated)>();
            foreach (var transfer in CryptoTransfers)
            {
                if (transfer.Amount == 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(CryptoTransfers), $"The amount to transfer crypto to/from {transfer.Address} must be a value, negative for transfers out, and positive for transfers in. A value of zero is not allowed.");
                }
                ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(netRequests, transfer.Address, out bool exists);
                if (!exists)
                {
                    entry = (transfer.Amount, transfer.Delegated);
                }
                else
                {
                    entry = (entry.Amount + transfer.Amount, entry.Delegated || transfer.Delegated);
                }
                sum += transfer.Amount;
            }
            if (netRequests.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(CryptoTransfers), "The list of crypto transfers can not be empty.");
            }
            if (sum != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(CryptoTransfers), "The sum of crypto sends and receives does not balance.");
            }
            var xferList = new TransferList();
            foreach (var transfer in netRequests)
            {
                if (transfer.Value.Amount != 0)
                {
                    xferList.AccountAmounts.Add(new AccountAmount(transfer.Key, transfer.Value.Amount, transfer.Value.Delegated));
                }
            }
            result.Transfers = xferList;
        }
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
                    throw new ArgumentOutOfRangeException(nameof(xfer.Amount), $"The amount to transfer tokens to/from {xfer.Account} must be a value, negative for transfers out, and positive for transfers in. A value of zero is not allowed.");
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
                    throw new ArgumentException("Nft", "The list of NFT transfers cannot contain a null or empty Token address.");
                }
                if (xfer.From.IsNullOrNone())
                {
                    throw new ArgumentException(nameof(xfer.From), "The list of NFT transfers cannot contain a null or empty from account value.");
                }
                if (xfer.To.IsNullOrNone())
                {
                    throw new ArgumentException(nameof(xfer.To), "The list of NFT transfers cannot contain a null or empty to account value.");
                }
                if (!nftXferList.TryAdd(xfer.Nft, new Proto.NftTransfer
                {
                    SenderAccountID = new AccountID(xfer.From),
                    ReceiverAccountID = new AccountID(xfer.To),
                    SerialNumber = xfer.Nft.SerialNumber,
                    IsApproval = xfer.Delegated
                }))
                {
                    throw new ArgumentException(nameof(xfer.Nft), "The list of NFT transfers cannot contain the same NFT in multiple transfers at once.");
                }
            }
            var netTransfers = new Dictionary<EntityId, TokenTransferList>();
            foreach (var record in nftXferList)
            {
                ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(netTransfers, record.Key, out bool exists)!;
                if (!exists)
                {
                    entry = new TokenTransferList
                    {
                        Token = new TokenID(record.Key)
                    };
                }
                entry.NftTransfers.Add(record.Value);
            }
            result.TokenTransfers.AddRange(netTransfers.Values);
        }
        if ((result.Transfers is null || result.Transfers.AccountAmounts.Count == 0) && result.TokenTransfers.Count == 0)
        {
            throw new ArgumentException(nameof(TransferParams), "Both crypto, token and NFT transfer lists are null or empty.  At least one must include net transfers.");
        }
        return result;
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "Transfer";
}
/// <summary>
/// Shortcut Internal Transfer Params for transferring tinybars only.
/// </summary>
/// <remarks>
/// Bypasses most of the checks for the gneric CryptoTransferTransactionBody construction.
/// </remarks>
internal sealed class TransferOnlyCryptoParams : INetworkParams
{
    private readonly EntityId _fromAddress;
    private readonly EntityId _toAddress;
    private readonly long _amount;

    public TransferOnlyCryptoParams(EntityId fromAddress, EntityId toAddress, long amount)
    {
        _fromAddress = fromAddress;
        _toAddress = toAddress;
        _amount = amount;
    }
    public Signatory? Signatory => null;
    public CancellationToken? CancellationToken => null;
    INetworkTransaction INetworkParams.CreateNetworkTransaction()
    {
        if (_fromAddress.IsNullOrNone())
        {
            throw new ArgumentNullException("fromAddress", "Account to transfer from is missing. Please check that it is not null.");
        }
        if (_toAddress.IsNullOrNone())
        {
            throw new ArgumentNullException("toAddress", "Account to transfer to is missing. Please check that it is not null.");
        }
        if (_amount < 1)
        {
            throw new ArgumentOutOfRangeException("amount", "The amount to transfer must be non-negative.");
        }
        var xferList = new TransferList();
        xferList.AccountAmounts.Add(new AccountAmount(_fromAddress, -_amount, false));
        xferList.AccountAmounts.Add(new AccountAmount(_toAddress, _amount, false));
        return new CryptoTransferTransactionBody()
        {
            Transfers = xferList
        };
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "Transfer";
}
/// <summary>
/// Shortcut Internal Transfer Params for transferring NFTs only.
/// </summary>
/// <remarks>
/// Bypasses most of the checks for the gneric CryptoTransferTransactionBody construction.
/// </remarks>
internal sealed class TransferOnlyNftParams : INetworkParams
{
    private readonly Nft _nft;
    private readonly EntityId _fromAddress;
    private readonly EntityId _toAddress;

    public TransferOnlyNftParams(Nft nft, EntityId fromAddress, EntityId toAddress)
    {
        _nft = nft;
        _fromAddress = fromAddress;
        _toAddress = toAddress;
    }
    public Signatory? Signatory => null;
    public CancellationToken? CancellationToken => null;
    INetworkTransaction INetworkParams.CreateNetworkTransaction()
    {
        if (_nft is null)
        {
            throw new ArgumentNullException("nft", "Asset to transfer is missing. Please check that it is not null.");
        }
        if (_fromAddress is null)
        {
            throw new ArgumentNullException("fromAddress", "Account to transfer from is missing. Please check that it is not null.");
        }
        if (_toAddress is null)
        {
            throw new ArgumentNullException("toAddress", "Account to transfer to is missing. Please check that it is not null.");
        }
        var transfers = new TokenTransferList
        {
            Token = new TokenID(_nft)
        };
        transfers.NftTransfers.Add(new Proto.NftTransfer
        {
            SenderAccountID = new AccountID(_fromAddress),
            ReceiverAccountID = new AccountID(_toAddress),
            SerialNumber = _nft.SerialNumber
        });
        var result = new CryptoTransferTransactionBody();
        result.TokenTransfers.Add(transfers);
        return result;
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "NFT Transfer";
}
/// <summary>
/// Shortcut Internal Transfer Params for transferring Tokens only.
/// </summary>
/// <remarks>
/// Bypasses most of the checks for the gneric CryptoTransferTransactionBody construction.
/// </remarks>
internal sealed class TransferOnlyTokenParams : INetworkParams
{
    private readonly EntityId _token;
    private readonly EntityId _fromAddress;
    private readonly EntityId _toAddress;
    private readonly long _amount;

    public TransferOnlyTokenParams(EntityId token, EntityId fromAddress, EntityId toAddress, long amount)
    {
        _token = token;
        _fromAddress = fromAddress;
        _toAddress = toAddress;
        _amount = amount;
    }
    public Signatory? Signatory => null;
    public CancellationToken? CancellationToken => null;
    INetworkTransaction INetworkParams.CreateNetworkTransaction()
    {
        if (_fromAddress is null)
        {
            throw new ArgumentNullException("fromAddress", "Account to transfer from is missing. Please check that it is not null.");
        }
        if (_toAddress is null)
        {
            throw new ArgumentNullException("toAddress", "Account to transfer to is missing. Please check that it is not null.");
        }
        if (_amount < 1)
        {
            throw new ArgumentOutOfRangeException("amount", "The amount to transfer must be non-negative.");
        }
        var transfers = new TokenTransferList
        {
            Token = new TokenID(_token)
        };
        transfers.Transfers.Add(new AccountAmount(_fromAddress, -_amount, false));
        transfers.Transfers.Add(new AccountAmount(_toAddress, _amount, false));
        var result = new CryptoTransferTransactionBody();
        result.TokenTransfers.Add(transfers);
        return result;
    }
    TransactionReceipt INetworkParams.CreateReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        return new TransactionReceipt(transactionId, receipt);
    }
    string INetworkParams.OperationDescription => "Token Transfer";
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TransferExtensions
{
    /// <summary>
    /// Transfer tinybars from one account to another.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the transfer.
    /// </param>
    /// <param name="fromAddress">
    /// The address to transfer the tinybars from.  Ensure that
    /// a signatory either in the context or passed with this
    /// call can fulfill the signing requrements to transfer 
    /// crypto out of the account identified by this address.
    /// </param>
    /// <param name="toAddress">
    /// The address receiving the tinybars.
    /// </param>
    /// <param name="amount">
    /// The amount of tinybars to transfer.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transfer receipt indicating success of the operation.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<TransactionReceipt> TransferAsync(this ConsensusClient client, EntityId fromAddress, EntityId toAddress, long amount, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<TransactionReceipt>(new TransferOnlyCryptoParams(fromAddress, toAddress, amount), configure);
    }
    /// <summary>
    /// Transfer assets (NFTs) from one account to another.
    /// </summary>
    /// <remarks>
    /// This conveience method does not support alloawnces, to
    /// perform transfers with allowances, use the long of this method,
    /// <see cref="CryptoExtensions.TransferAsync(ConsensusClient, TransferParams, Action{IConsensusContext}?)"/>,
    /// instead.
    /// </remarks>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the transfer.
    /// </param>
    /// <param name="nft">
    /// The identifier of the nft to transfer (shard, realm, num, serial).
    /// </param>
    /// <param name="fromAddress">
    /// The account to transfer the assets from.  Ensure that
    /// a signatory either in the context or passed with this
    /// call can fulfill the signing requrements to transfer 
    /// assets out of the account identified by this account.
    /// </param>
    /// <param name="toAddress">
    /// The account receiving the assets.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transfer receipt indicating success of the operation.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<TransactionReceipt> TransferNftAsync(this ConsensusClient client, Nft nft, EntityId fromAddress, EntityId toAddress, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<TransactionReceipt>(new TransferOnlyNftParams(nft, fromAddress, toAddress), configure);
    }
    /// <summary>
    /// Transfer cryptocurrency and tokens in the same transaction atomically among multiple Hedera accounts and contracts.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the transfer.
    /// </param>
    /// <param name="transfers">
    /// A transfers parameter object holding lists of crypto and token transfers to perform.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transfer receipt indicating success of the consensus operation.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<TransactionReceipt> TransferAsync(this ConsensusClient client, TransferParams transfers, Action<IConsensusContext>? configure = null)
    {
        if (transfers is null)
        {
            throw new ArgumentNullException(nameof(transfers), "The transfer parameters cannot not be null.");
        }
        return client.ExecuteNetworkParamsAsync<TransactionReceipt>(transfers, configure);
    }
    /// <summary>
    /// Transfer Fungible tokens from one account to another.
    /// </summary>
    /// <remarks>
    /// Does not support allowances, to use the allowance flag
    /// in the transfer list use the <see cref="TransferParams"/> 
    /// to construct the transfer.
    /// </remarks>
    /// <param name="client">
    /// The Consensus Node Client orchestrating the transfer.
    /// </param>
    /// <param name="token">
    /// The identifier of the token type being transferred.
    /// </param>
    /// <param name="fromAddress">
    /// The address to transfer the tokens from.  Ensure that
    /// a signatory either in the context or passed with this
    /// call can fulfill the signing requrements to transfer 
    /// tokens out of the account identified by this address.
    /// </param>
    /// <param name="toAddress">
    /// The address receiving the tokens.
    /// </param>
    /// <param name="amount">
    /// The amount of tokens to transfer.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transfer receipt indicating success of the operation.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="ConsensusException">If the network was unable to come to consensus before the duration of the transaction expired.</exception>
    /// <exception cref="TransactionException">If the network rejected the create request as invalid or had missing data.</exception>
    public static Task<TransactionReceipt> TransferTokensAsync(this ConsensusClient client, EntityId token, EntityId fromAddress, EntityId toAddress, long amount, Action<IConsensusContext>? configure = null)
    {
        return client.ExecuteNetworkParamsAsync<TransactionReceipt>(new TransferOnlyTokenParams(token, fromAddress, toAddress, amount), configure);
    }
}