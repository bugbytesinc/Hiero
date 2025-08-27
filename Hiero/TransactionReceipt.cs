using Google.Protobuf.Collections;
using Hiero.Implementation;
using Proto;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Hiero;

/// <summary>
/// The details returned from the network after consensus 
/// has been reached for a network request.
/// </summary>
public record TransactionReceipt
{
    /// <summary>
    /// The TransactionId ID associated with the request.
    /// </summary>
    public TransactionId TransactionId { get; internal init; }
    /// <summary>
    /// The response code returned from the server.
    /// </summary>
    public ResponseCode Status { get; internal init; }
    /// <summary>
    /// The current exchange between USD and hBars as
    /// broadcast by the hedera Network.
    /// </summary>
    /// <remarks>
    /// Not all Receipts and Records will have this information
    /// returned from the network.  This value can be <code>null</code>.
    /// </remarks>
    public ExchangeRate? CurrentExchangeRate { get; internal init; }
    /// <summary>
    /// The next/future exchange between USD and 
    /// hBars as broadcast by the hedera Network.
    /// </summary>
    /// <remarks>
    /// Not all Receipts and Records will have this information
    /// returned from the network.  This value can be <code>null</code>.
    /// </remarks>
    public ExchangeRate? NextExchangeRate { get; internal init; }
    /// <summary>
    /// If this transaction resulted in the pending (to be scheduled)
    /// transaction retained by the network, this property will contain
    /// the identifier of the pending transaction record.  This includes 
    /// the identifier of the pending transaction as well as the bytes
    /// representing the transaction which must be signed by the 
    /// remaining parties.
    /// </summary>
    public PendingTransaction? Pending { get; internal init; }
    /// <summary>
    /// Internal Constructor of the record.
    /// </summary>
    /// <param name="receipt">Network Receipt Containing Info</param>
    internal TransactionReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        TransactionId = transactionId.AsTxId();
        Status = (ResponseCode)receipt.Status;
        if (receipt.ExchangeRate is not null)
        {
            CurrentExchangeRate = receipt.ExchangeRate.CurrentRate?.ToExchangeRate();
            NextExchangeRate = receipt.ExchangeRate.NextRate?.ToExchangeRate();
        }
        if (receipt.ScheduledTransactionID is not null || receipt.ScheduleID is not null)
        {
            Pending = new PendingTransaction
            {
                Id = receipt.ScheduleID?.ToAddress() ?? EntityId.None,
                TxId = receipt.ScheduledTransactionID.AsTxId()
            };
        }
    }
}
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TransactionReceiptExtensions
{
    /// <summary>
    /// Retrieves the receipt from the network matching the transaction
    /// id.  Will wait for the disposition of the receipt to be known.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="transaction">
    /// TransactionId identifier of the receipt.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// The receipt matching the transaction id, if found and marked
    /// sucessfull, otherwise a <see cref="TransactionException"/> is 
    /// not found or returns an error status.
    /// </returns>
    /// <exception cref="TransactionException">If the network has no record of the transaction or request has invalid or had missing data.</exception>
    public static async Task<TransactionReceipt> GetReceiptAsync(this ConsensusClient client, TransactionId transaction, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        await using var context = client.CreateChildContext(configure);
        var transactionId = new TransactionID(transaction);
        var receipt = FromProtobuf(transactionId, await ConsensusEngine.GetReceiptAsync(context, transactionId, cancellationToken).ConfigureAwait(false));
        if (receipt.Status != ResponseCode.Success && context.ThrowIfNotSuccess)
        {
            throw new TransactionException($"Unable to retreive receipt, status: {receipt.Status}", receipt);
        }
        return receipt;
    }
    /// <summary>
    /// Retreives all known receipts from the network having the given
    /// transaction ID.  The method will wait for the disposition of at
    /// least one receipt to be known.  Receipts with failure codes do
    /// not cause an exception to be raised.
    /// </summary>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="transaction">
    /// TransactionId identifier of the receipt.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// A list of receipts having the identified transaction.  The list
    /// may be empty or contain multiple entries.
    /// </returns>
    /// <exception cref="ConsensusException">if the receipt is known to exist but has not reached consensus 
    /// within the alloted time to wait for a return from this method or the network has been too busy to 
    /// respond.</exception>
    public static async Task<IReadOnlyList<TransactionReceipt>> GetAllReceiptsAsync(this ConsensusClient client, TransactionId transaction, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        await using var context = client.CreateChildContext(configure);
        var transactionId = new TransactionID(transaction);
        INetworkQuery query = new TransactionGetReceiptQuery
        {
            TransactionID = transactionId,
            IncludeDuplicates = true,
            IncludeChildReceipts = true
        };
        var response = await ConsensusEngine.ExecuteNetworkRequestWithRetryAsync(context, query.CreateEnvelope(), query.InstantiateNetworkRequestMethod, shouldRetry, cancellationToken).ConfigureAwait(false);
        var responseCode = response.TransactionGetReceipt.Header.NodeTransactionPrecheckCode;
        if (responseCode == ResponseCodeEnum.Busy)
        {
            throw new ConsensusException("Network failed to respond to request for a transaction receipt, it is too busy. It is possible the network may still reach concensus for this transaction.", transactionId.AsTxId(), (ResponseCode)responseCode);
        }
        return createList(transactionId, response.TransactionGetReceipt.Receipt, response.TransactionGetReceipt.ChildTransactionReceipts, response.TransactionGetReceipt.DuplicateTransactionReceipts);

        static bool shouldRetry(Response response)
        {
            return
                response.TransactionGetReceipt?.Header?.NodeTransactionPrecheckCode == ResponseCodeEnum.Busy ||
                response.TransactionGetReceipt?.Receipt?.Status == ResponseCodeEnum.Unknown;
        }
        static IReadOnlyList<TransactionReceipt> createList(TransactionID transactionId, Proto.TransactionReceipt rootReceipt, RepeatedField<Proto.TransactionReceipt> childrenReceipts, RepeatedField<Proto.TransactionReceipt> failedReceipts)
        {
            var count = (rootReceipt != null ? 1 : 0) + (childrenReceipts?.Count ?? 0) + (failedReceipts?.Count ?? 0);
            if (count == 0)
            {
                return [];
            }
            var result = new List<TransactionReceipt>(count);
            if (rootReceipt is not null)
            {
                result.Add(FromProtobuf(transactionId, rootReceipt));
            }
            if (childrenReceipts is not null && childrenReceipts.Count > 0)
            {
                // The network DOES NOT return the
                // child transaction ID, so we have
                // to synthesize it.
                var nonce = 1;
                foreach (var entry in childrenReceipts)
                {
                    var childTransactionId = transactionId.Clone();
                    childTransactionId.Nonce = nonce;
                    result.Add(FromProtobuf(childTransactionId, entry));
                    nonce++;
                }
            }
            if (failedReceipts is not null && failedReceipts.Count > 0)
            {
                foreach (var entry in failedReceipts)
                {
                    result.Add(FromProtobuf(transactionId, entry));
                }
            }
            return result;
        }
    }
    internal static TransactionReceipt FromProtobuf(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        if (receipt.AccountID != null)
        {
            return new CreateAccountReceipt(transactionId, receipt);
        }
        else if (receipt.FileID != null)
        {
            return new FileReceipt(transactionId, receipt);
        }
        else if (receipt.TopicID != null)
        {
            return new CreateTopicReceipt(transactionId, receipt);
        }
        else if (receipt.ContractID != null)
        {
            return new CreateContractReceipt(transactionId, receipt);
        }
        else if (receipt.TokenID != null)
        {
            return new CreateTokenReceipt(transactionId, receipt);
        }
        else if (!receipt.TopicRunningHash.IsEmpty)
        {
            return new SubmitMessageReceipt(transactionId, receipt);
        }
        else if (receipt.SerialNumbers != null && receipt.SerialNumbers.Count > 0)
        {
            return new NftMintReceipt(transactionId, receipt);
        }
        else if (receipt.NewTotalSupply != 0)
        {
            return new TokenReceipt(transactionId, receipt);
        }
        else
        {
            return new TransactionReceipt(transactionId, receipt);
        }
    }
}