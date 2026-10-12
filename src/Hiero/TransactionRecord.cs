// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf.Collections;
using Hiero.Implementation;
using Proto;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Hiero;

/// <summary>
/// The detailed results of a network request after consensus has been
/// reached, extending the <see cref="TransactionReceipt"/> with the
/// transaction hash, consensus timestamp, fee charged, and the full set
/// of hBar, token, NFT, royalty and staking-reward transfers it produced.
/// </summary>
public record TransactionRecord : TransactionReceipt
{
    /// <summary>
    /// Hash of the Transaction.
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
    /// the records represented by this transaction, keyed by account
    /// (including the <see cref="IConsensusContext.Payer"/> that funded
    /// the transaction).
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
    /// If the transaction affected a change in treasury for an NFT token
    /// it will be identified here.  It does not indicate which serial 
    /// numbers of NFTs were transferred to the new treasury, just that
    /// all of the NFTs held by the previously designated treasury are now
    /// owned by the new treasury.
    /// </summary>
    public TreasuryTransfer? TreasuryTransfer { get; internal init; }
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
    /// If this record represents a child transaction, the consensus timestamp
    /// of the parent transaction to this transaction, otherwise null.
    /// </summary>
    public ConsensusTimeStamp? ParentTransactionConsensus { get; internal init; }
    /// <summary>
    /// A List of account staking rewards paid as a result of this transaction.
    /// </summary>
    public ReadOnlyDictionary<EntityId, long> StakingRewards { get; internal init; }
    /// <summary>
    /// Internal Constructor of the record.
    /// </summary>
    internal TransactionRecord(Proto.TransactionRecord record) : base(record.TransactionID, record.Receipt)
    {
        var (tokenTransfers, assetTransfers, treasuryTransfer) = record.TokenTransferLists.AsTransferLists();
        Hash = record.TransactionHash.Memory;
        Consensus = record.ConsensusTimestamp?.ToConsensusTimeStamp();
        Memo = record.Memo;
        Fee = record.TransactionFee;
        Transfers = record.TransferList?.ToTransfers() ?? TransactionRecordExtensions.EMPTY_TRANSFERS;
        TokenTransfers = tokenTransfers;
        NftTransfers = assetTransfers;
        TreasuryTransfer = treasuryTransfer;
        Royalties = record.AssessedCustomFees.AsRoyaltyTransferList();
        Associations = record.AutomaticTokenAssociations.AsAssociationList();
        ParentTransactionConsensus = record.ParentConsensusTimestamp?.ToConsensusTimeStamp();
        StakingRewards = record.PaidStakingRewards.AsStakingRewards();
    }
}
/// <summary>
/// Extension methods for retrieving transaction records from the network.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TransactionRecordExtensions
{
    internal static readonly ReadOnlyDictionary<EntityId, long> EMPTY_TRANSFERS = new(new Dictionary<EntityId, long>(0));
    /// <summary>
    /// Retrieves the transaction records for a given transaction ID that was
    /// successfully processed, otherwise the first one to reach consensus.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="transaction">
    /// TransactionId identifier of the record
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A transaction record with the specified id, or an exception if not found.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Generally there is only one record per transaction, but in certain cases
    /// where there is a transaction ID collision (deliberate or accidental) there
    /// may be more, the <see cref="GetAllTransactionRecordsAsync"/>
    /// method may be used to retrieve all records.
    /// </para>
    /// <para>
    /// Record types are resolved on a <b>best-effort</b> basis. Four kinds are
    /// identified by fields the wire format cannot distinguish from unset when
    /// their value is zero — <see cref="TokenRecord"/>,
    /// <see cref="NftMintRecord"/>, <see cref="ConsensusNodeRecord"/> and
    /// <see cref="RegisteredNodeRecord"/> — and resolve to the base
    /// <see cref="TransactionRecord"/> in that case. Prefer an <c>is</c>
    /// pattern over a hard cast, which would throw. No data is lost, since the
    /// fallback occurs only when the value is zero.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the query.</exception>
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
    /// Retrieves every record the network holds for the given transaction
    /// ID, grouped into the submitted transaction's own record, the records
    /// of any child transactions it spawned, and the records of any
    /// duplicate submissions of the same transaction ID.  Typically only
    /// the parent record is present, but a transaction that causes the
    /// network to act on its behalf — or that was submitted more than once
    /// — will populate the other groups.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="transaction">
    /// TransactionId identifier of the record
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A collection of all the transaction records known to the system
    /// at the time of query having the identified transaction id.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Record types are resolved on a <b>best-effort</b> basis. Four kinds are
    /// identified by fields the wire format cannot distinguish from unset when
    /// their value is zero — <see cref="TokenRecord"/>,
    /// <see cref="NftMintRecord"/>, <see cref="ConsensusNodeRecord"/> and
    /// <see cref="RegisteredNodeRecord"/> — and resolve to the base
    /// <see cref="TransactionRecord"/> in that case. Prefer an <c>is</c>
    /// pattern over a hard cast, which would throw. No data is lost, since the
    /// fallback occurs only when the value is zero.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the query.</exception>
    /// <exception cref="TransactionException">If the node refuses the paid lookup with any status other than RECORD_NOT_FOUND, for example when the network changes the query's price mid-request. Its receipt is a stand-in carrying that status, not a verdict on the transaction. A transaction id the node holds no records for does not throw: <see cref="TransactionRecords.Parent"/> is then <code>null</code>.</exception>
    public static async Task<TransactionRecords> GetAllTransactionRecordsAsync(this ConsensusClient client, TransactionId transaction, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
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
    /// Retrieves the account records associated with an account that are presently
    /// held within the network because they exceeded the receive or send threshold
    /// values for autogeneration of records.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="account">
    /// The Hedera account to retrieve associated records for.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// An array of transaction records associated with the given account.
    /// </returns>
    /// <exception cref="ArgumentException">If required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">If required context configuration is missing.</exception>
    /// <exception cref="PrecheckException">If the gateway node refused the query.</exception>
    /// <remarks>
    /// <para>
    /// Record types are resolved on a <b>best-effort</b> basis. Four kinds are
    /// identified by fields the wire format cannot distinguish from unset when
    /// their value is zero — <see cref="TokenRecord"/>,
    /// <see cref="NftMintRecord"/>, <see cref="ConsensusNodeRecord"/> and
    /// <see cref="RegisteredNodeRecord"/> — and resolve to the base
    /// <see cref="TransactionRecord"/> in that case. Prefer an <c>is</c>
    /// pattern over a hard cast, which would throw. No data is lost, since the
    /// fallback occurs only when the value is zero.
    /// </para>
    /// </remarks>
    public static async Task<TransactionRecord[]> GetAccountRecordsAsync(this ConsensusClient client, EntityId account, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        var records = (await Engine.QueryAsync(client, new CryptoGetAccountRecordsQuery { AccountID = new AccountID(account) }, cancellationToken, configure).ConfigureAwait(false)).CryptoGetAccountRecords;
        var count = records.Records.Count;
        if (count != 0)
        {
            var result = new TransactionRecord[count];
            for (var i = 0; i < count; i++)
            {
                result[i] = FromProtobuf(records.Records[i]);
            }
            return result;
        }
        return [];
    }
    /// <summary>
    /// Helper Function to group the transaction records returned by a network query.
    /// </summary>
    /// <param name="parentRecord">The submitted transaction's own record, if the network has one.</param>
    /// <param name="childRecords">Records of the child transactions the network executed as part of the parent.</param>
    /// <param name="duplicateRecords">Records of additional submissions of the same transaction ID.</param>
    /// <returns>The records grouped by the role the network reported them in.</returns>
    internal static TransactionRecords Create(Proto.TransactionRecord? parentRecord, RepeatedField<Proto.TransactionRecord>? childRecords, RepeatedField<Proto.TransactionRecord>? duplicateRecords)
    {
        var childCount = childRecords?.Count ?? 0;
        var duplicateCount = duplicateRecords?.Count ?? 0;
        var children = Array.Empty<TransactionRecord>();
        var duplicates = Array.Empty<TransactionRecord>();
        if (childCount > 0)
        {
            // The network DOES NOT return the
            // child transaction ID, so we have
            // to synthesize it.
            children = new TransactionRecord[childCount];
            for (var i = 0; i < childCount; i++)
            {
                var entry = childRecords![i];
                var childTransactionId = entry.TransactionID.Clone();
                childTransactionId.Nonce = i + 1;
                entry.TransactionID = childTransactionId;
                children[i] = FromProtobuf(entry);
            }
        }
        if (duplicateCount > 0)
        {
            duplicates = new TransactionRecord[duplicateCount];
            for (var i = 0; i < duplicateCount; i++)
            {
                duplicates[i] = FromProtobuf(duplicateRecords![i]);
            }
        }
        return new TransactionRecords
        {
            Parent = parentRecord is not null ? FromProtobuf(parentRecord) : null,
            Children = children,
            Duplicates = duplicates
        };
    }

    internal static ReadOnlyDictionary<EntityId, long> AsStakingRewards(this RepeatedField<AccountAmount> rewards)
    {
        var count = rewards?.Count ?? 0;
        if (count == 0)
        {
            return EMPTY_TRANSFERS;
        }
        var results = new Dictionary<EntityId, long>(count);
        for (var i = 0; i < count; i++)
        {
            var reward = rewards![i];
            var account = reward.AccountID.AsAddress();
            ref var amount = ref CollectionsMarshal.GetValueRefOrAddDefault(results, account, out _);
            amount += reward.Amount;
        }
        return new ReadOnlyDictionary<EntityId, long>(results);
    }
    /// <summary>
    /// Internal Helper function used to wait for consensus regardless of the reported
    /// transaction outcome. We do not know if the transaction in question has come
    /// to consensus so we need to get the receipt first (and wait if necessary).
    /// The Receipt status returned does not matter in this case.
    /// We may be retrieving a failed record (the status would not equal OK).
    /// </summary>
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
        else if (receipt.ScheduleID != null)
        {
            return new ScheduleRecord(record);
        }
        else if (receipt.NodeId > 0)
        {
            return new ConsensusNodeRecord(record);
        }
        else if (receipt.RegisteredNodeId > 0)
        {
            return new RegisteredNodeRecord(record);
        }
        else if (record.ContractCreateResult != null)
        {
            return new CreateContractRecord(record);
        }
        else if (record.ContractCallResult != null)
        {
            return new CallContractRecord(record);
        }
        else if (record.NewPendingAirdrops.Count > 0)
        {
            return new AirdropRecord(record);
        }
        else if (!record.EthereumHash.IsEmpty)
        {
            return new EvmTransactionRecord(record);
        }
        else if (record.EntropyCase == Proto.TransactionRecord.EntropyOneofCase.PrngNumber)
        {
            return new RangedPseudoRandomNumberRecord(record);
        }
        else if (record.EntropyCase == Proto.TransactionRecord.EntropyOneofCase.PrngBytes)
        {
            return new BytesPseudoRandomNumberRecord(record);
        }
        else
        {
            return new TransactionRecord(record);
        }
    }
}
