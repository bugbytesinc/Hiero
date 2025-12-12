using Google.Protobuf.Collections;
using Hiero.Implementation;
using Proto;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Hiero;

/// <summary>
/// The details returned from the network after consensus 
/// has been reached for a network request.
/// </summary>
public record TransactionRecord : TransactionReceipt
{
    /// <summary>
    /// Hash of the TransactionId.
    /// </summary>
    public ReadOnlyMemory<byte> Hash { get; internal init; }
    /// <summary>
    /// The consensus timestamp.
    /// </summary>
    public ConsensusTimeStamp? Consensus { get; internal init; }
    /// <summary>
    /// The memo that was submitted with the transaction request.
    /// </summary>
    public string Memo { get; internal init; }
    /// <summary>
    /// The fee that was charged by the network for processing the 
    /// transaction and generating associated receipts and records.
    /// </summary>
    public ulong Fee { get; internal init; }
    /// <summary>
    /// A map of tinybar transfers to and from accounts associated with
    /// the records represented by this transaction.
    /// <see cref="IConsensusContext.Payer"/>.
    /// </summary>
    public ReadOnlyDictionary<EntityId, long> Transfers { get; internal init; }
    /// <summary>
    /// A list of token transfers to and from accounts associated with
    /// the records represented by this transaction.
    /// </summary>
    public IReadOnlyList<TokenTransfer> TokenTransfers { get; internal init; }
    /// <summary>
    /// A list of asset transfers to and from accounts associated with
    /// the records represented by this transaction.
    /// </summary>
    public IReadOnlyList<NftTransfer> NftTransfers { get; internal init; }
    /// <summary>
    /// A list of token transfers applied by the network as royalties
    /// for executing the original transaction.  Typically in the form
    /// of royalties for transferring custom tokens and assets as defined
    /// by the respective token definition's fees.
    /// </summary>
    public IReadOnlyList<RoyaltyTransfer> Royalties { get; internal init; }
    /// <summary>
    /// A list of token associations that were created 
    /// as a result of this transaction.
    /// </summary>
    public IReadOnlyList<Association> Associations { get; internal init; }

    /// <summary>
    /// If this records represents a child transaction, the consensus timestamp
    /// of the parent transaction to this transaction, otherwise null.
    /// transaction 
    /// </summary>
    public ConsensusTimeStamp? ParentTransactionConsensus { get; internal init; }
    /// <summary>
    /// A List of account staking rewards paid  as a result of this transaction.
    /// </summary>
    public ReadOnlyDictionary<EntityId, long> StakingRewards { get; internal init; }
    /// <summary>
    /// Internal Constructor of the records.
    /// </summary>
    internal TransactionRecord(Proto.TransactionRecord record) : base(record.TransactionID, record.Receipt)
    {
        var (tokenTransfers, assetTransfers) = record.TokenTransferLists.AsTokenAndAssetTransferLists();
        Hash = record.TransactionHash.Memory;
        Consensus = record.ConsensusTimestamp?.ToConsensusTimeStamp();
        Memo = record.Memo;
        Fee = record.TransactionFee;
        Transfers = record.TransferList?.ToTransfers() ?? new ReadOnlyDictionary<EntityId, long>(new Dictionary<EntityId, long>());
        TokenTransfers = tokenTransfers;
        NftTransfers = assetTransfers;
        Royalties = record.AssessedCustomFees.AsRoyaltyTransferList();
        Associations = record.AutomaticTokenAssociations.AsAssociationList();
        ParentTransactionConsensus = record.ParentConsensusTimestamp?.ToConsensusTimeStamp();
        StakingRewards = record.PaidStakingRewards.AsStakingRewards();
    }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TransactionRecordExtensions
{
    /// <summary>
    /// Retrieves the transaction records for a given transaction ID that was
    /// successfully processed, otherwise the first one to reach consensus.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="transaction">
    /// TransactionId identifier of the records
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction records with the specified id, or an exception if not found.
    /// </returns>
    /// <remarks>
    /// Generally there is only one records per transaction, but in certain cases
    /// where there is a transaction ID collision (deliberate or accidental) there
    /// may be more, the <see cref="GetAllTransactionRecordsAsync(TransactionId, Action{IConsensusContext}?)"/>
    /// method may be used to retrieve all records.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    /// <exception cref="TransactionException">If the network has no records of the transaction or request has invalid or had missing data.</exception>
    public static async Task<TransactionRecord> GetTransactionRecordAsync(this ConsensusClient client, TransactionId transaction, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        await using var context = client.BuildChildContext(configure);
        var transactionId = new TransactionID(transaction);
        // For the public version of this method, we do not know
        // if the transaction in question has come to consensus so
        // we need to get the receipt first (and wait if necessary).
        await WaitForConsensusReceipt(context, transactionId, cancellationToken).ConfigureAwait(false);
        var record = (await Engine.QueryAsync(context, new TransactionGetRecordQuery { TransactionID = transactionId }, cancellationToken).ConfigureAwait(false)).TransactionGetRecord.TransactionRecord;
        return FromProtobuf(record);
    }
    /// <summary>
    /// Retrieves all records having the given transaction ID, including duplicates
    /// that were rejected or produced errors during execution.  Typically there is
    /// only one records per transaction, but in some cases, deliberate or accidental
    /// there may be more than one for a given transaction ID.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="transaction">
    /// TransactionId identifier of the records
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// An collection of all the transaction records known to the system
    /// at the time of query having the identified transaction id.
    /// </returns>
    public static async Task<ReadOnlyCollection<TransactionRecord>> GetAllTransactionRecordsAsync(this ConsensusClient client, TransactionId transaction, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        await using var context = client.BuildChildContext(configure);
        var transactionId = new TransactionID(transaction);
        // For the public version of this method, we do not know
        // if the transaction in question has come to consensus so
        // we need to get the receipt first (and wait if necessary).
        await WaitForConsensusReceipt(context, transactionId, cancellationToken).ConfigureAwait(false);
        var response = await Engine.QueryAsync(context, new TransactionGetRecordQuery
        {
            TransactionID = transactionId,
            IncludeDuplicates = true,
            IncludeChildRecords = true
        }, cancellationToken).ConfigureAwait(false);
        var records = response.TransactionGetRecord;
        return TransactionRecordExtensions.Create(records.TransactionRecord, records.ChildTransactionRecords, records.DuplicateTransactionRecords);
    }
    /// <summary>
    /// Internal Helper function used to wait for consensus regardless of the reported
    /// transaction outcome. We do not know if the transaction in question has come 
    /// to consensus so we need to get the receipt first (and wait if necessary).
    /// The Receipt status returned does not matter in this case.  
    /// We may be retrieving a failed records (the status would not equal OK).
    private static async Task WaitForConsensusReceipt(ConsensusContextStack context, TransactionID transactionId, CancellationToken cancellationToken)
    {
        INetworkQuery query = new TransactionGetReceiptQuery { TransactionID = transactionId };
        await Engine.SubmitMessageAsync(context, query.CreateEnvelope(), query.InstantiateNetworkRequestMethod, shouldRetry, cancellationToken).ConfigureAwait(false);

        static bool shouldRetry(Response response)
        {
            return
                response.TransactionGetReceipt?.Header?.NodeTransactionPrecheckCode == ResponseCodeEnum.Busy ||
                response.TransactionGetReceipt?.Receipt?.Status == ResponseCodeEnum.Unknown;
        }
    }
    /// <summary>
    /// Empty Result when no records are returned from the network.
    /// </summary>
    private static readonly ReadOnlyCollection<TransactionRecord> EMPTY_RESULT = new List<TransactionRecord>().AsReadOnly();
    /// <summary>
    /// Retrieves the account records associated with an account that are presently
    /// held within the network because they exceeded the receive or send threshold
    /// values for autogeneration of records.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="address">
    /// The Hedera Network Payer to retrieve associated records.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A detailed description of the account.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">If required arguments are missing.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node create rejected the request upon submission.</exception>
    public static async Task<TransactionRecord[]> GetAccountRecordsAsync(this ConsensusClient client, EntityId address, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        var records = (await Engine.QueryAsync(client, new CryptoGetAccountRecordsQuery { AccountID = new AccountID(address) }, cancellationToken, configure).ConfigureAwait(false)).CryptoGetAccountRecords;
        if (records.Records.Count != 0)
        {
            return [.. records.Records.Select(FromProtobuf)];
        }
        return [];
    }
    /// <summary>
    /// Helper Function to create a collection of transaction records from a network query.
    /// </summary>
    /// <param name="rootRecord">If exists, the parent records</param>
    /// <param name="childrenRecords"></param>
    /// <param name="failedRecords"></param>
    /// <returns></returns>
    internal static ReadOnlyCollection<TransactionRecord> Create(Proto.TransactionRecord? rootRecord, RepeatedField<Proto.TransactionRecord>? childrenRecords, RepeatedField<Proto.TransactionRecord>? failedRecords)
    {
        var count = (rootRecord != null ? 1 : 0) + (childrenRecords != null ? childrenRecords.Count : 0) + (failedRecords != null ? failedRecords.Count : 0);
        if (count > 0)
        {
            var result = new List<TransactionRecord>(count);
            if (rootRecord is not null)
            {
                result.Add(FromProtobuf(rootRecord));
            }
            if (childrenRecords is not null && childrenRecords.Count > 0)
            {
                // The network DOES NOT return the
                // child transaction ID, so we have
                // to synthesize it.
                var nonce = 1;
                foreach (var entry in childrenRecords)
                {
                    var childTransactionId = entry.TransactionID.Clone();
                    childTransactionId.Nonce = nonce;
                    result.Add(FromProtobuf(entry));
                    nonce++;
                }
            }
            if (failedRecords is not null && failedRecords.Count > 0)
            {
                foreach (var entry in failedRecords)
                {
                    result.Add(FromProtobuf(entry));
                }
            }
            return result.AsReadOnly();
        }
        return EMPTY_RESULT;
    }

    internal static ReadOnlyDictionary<EntityId, long> AsStakingRewards(this RepeatedField<AccountAmount> rewards)
    {
        var results = new Dictionary<EntityId, long>();
        if (rewards is not null)
        {
            foreach (var xfer in rewards)
            {
                var account = xfer.AccountID.AsAddress();
                results.TryGetValue(account, out long amount);
                results[account] = amount + xfer.Amount;
            }
        }
        return new ReadOnlyDictionary<EntityId, long>(results);
    }
    private static TransactionRecord FromProtobuf(Proto.TransactionRecord record)
    {
        var receipt = record.Receipt;
        if (receipt.AccountID != null)
        {
            return new CreateAccountRecord(record);
        }
        else if (receipt.FileID != null)
        {
            return new FileRecord(record);
        }
        else if (receipt.TopicID != null)
        {
            return new CreateTopicRecord(record);
        }
        else if (receipt.TokenID != null)
        {
            return new CreateTokenRecord(record);
        }
        else if (!receipt.TopicRunningHash.IsEmpty)
        {
            return new SubmitMessageRecord(record);
        }
        else if (receipt.SerialNumbers != null && receipt.SerialNumbers.Count > 0)
        {
            return new NftMintRecord(record);
        }
        else if (receipt.NewTotalSupply != 0)
        {
            return new TokenRecord(record);
        }
        else if (record?.ContractCreateResult != null)
        {
            return new CreateContractRecord(record);
        }
        else if (record?.ContractCallResult != null)
        {
            return new CallContractRecord(record);
        }
        else if (record is not null && !record.EthereumHash.IsEmpty)
        {
            return new EvmTransactionRecord(record);
        }
        else if (record?.EntropyCase == Proto.TransactionRecord.EntropyOneofCase.PrngNumber)
        {
            return new RangedPseudoRandomNumberRecord(record);
        }
        else if (record?.EntropyCase == Proto.TransactionRecord.EntropyOneofCase.PrngBytes)
        {
            return new BytesPseudoRandomNumberRecord(record);
        }
        else
        {
            return new TransactionRecord(record!);
        }
    }
}