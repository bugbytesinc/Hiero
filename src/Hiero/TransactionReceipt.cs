// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf.Collections;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;

/// <summary>
/// The details returned from the network after consensus 
/// has been reached for a network request.
/// </summary>
public record TransactionReceipt
{
    /// <summary>
    /// The Transaction ID associated with the request.
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
    /// Internal Constructor of the record.
    /// </summary>
    /// <param name="transactionId">The transaction ID associated with this receipt.</param>
    /// <param name="receipt">Network Receipt Containing Info</param>
    internal TransactionReceipt(TransactionID transactionId, Proto.TransactionReceipt receipt)
    {
        TransactionId = transactionId.AsTransactionId();
        Status = (ResponseCode)receipt.Status;
        if (receipt.ExchangeRate is not null)
        {
            CurrentExchangeRate = receipt.ExchangeRate.CurrentRate?.ToExchangeRate();
            NextExchangeRate = receipt.ExchangeRate.NextRate?.ToExchangeRate();
        }
    }
}
/// <summary>
/// Extension methods for retrieving transaction receipts from the network.
/// </summary>
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
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <remarks>
    /// The receipt is returned as the most specific type the network's response
    /// supports (<see cref="CreateAccountReceipt"/>, <see cref="TokenReceipt"/>
    /// and so on). That resolution is <b>best-effort</b>: four receipt kinds are
    /// identified by fields the wire format cannot distinguish from unset when
    /// their value is zero, and resolve to the base
    /// <see cref="TransactionReceipt"/> in that case — see
    /// <see cref="TokenReceipt"/>, <see cref="NftMintReceipt"/>,
    /// <see cref="ConsensusNodeReceipt"/> and
    /// <see cref="RegisteredNodeReceipt"/>. Prefer an <c>is</c> pattern over a
    /// hard cast, which would throw in those cases.
    /// </remarks>
    /// <returns>
    /// The receipt matching the transaction id. With
    /// <see cref="IConsensusContext.ThrowIfNotSuccess"/> false it is returned
    /// whatever its status, and when the node has no receipt for the id it
    /// is a stand-in whose status is <see cref="ResponseCode.ReceiptNotFound"/>.
    /// </returns>
    /// <exception cref="TransactionException">If the transaction reached consensus and failed: its receipt's status is not <see cref="ResponseCode.Success"/>.</exception>
    /// <exception cref="ConsensusException">If the outcome is unknown: the transaction has not reached consensus within the retry window, or the node has no receipt for it (<see cref="ResponseCode.ReceiptNotFound"/>) because it never saw it or no longer holds the receipt. Nodes keep receipts for about three minutes after consensus; settle an older or unknown transaction on a mirror node.</exception>
    public static async Task<TransactionReceipt> GetReceiptAsync(this ConsensusClient client, TransactionId transaction, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        await using var context = client.BuildChildContext(configure);
        var transactionId = new TransactionID(transaction);
        var receipt = FromProtobuf(transactionId, await Engine.GetReceiptAsync(context, transactionId, cancellationToken).ConfigureAwait(false));
        if (receipt.Status != ResponseCode.Success && context.ThrowIfNotSuccess)
        {
            throw new TransactionException($"Unable to retrieve receipt, status: {receipt.Status}", receipt);
        }
        return receipt;
    }
    /// <summary>
    /// Retrieves all known receipts from the network having the given
    /// transaction ID.  The method will wait for the disposition of at
    /// least one receipt to be known.  Receipts with failure codes do
    /// not cause an exception to be raised.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The result separates the receipts into the three roles the network
    /// reports: the submitted transaction's own receipt
    /// (<see cref="TransactionReceipts.Parent"/>), the receipts of any
    /// child transactions it spawned
    /// (<see cref="TransactionReceipts.Children"/>), and the receipts of
    /// any duplicate submissions of the same transaction ID
    /// (<see cref="TransactionReceipts.Duplicates"/>).  Any of them may
    /// be empty.
    /// </para>
    /// <para>
    /// Work the network performs on a transaction's behalf reports
    /// through the children, not the parent — for example the account
    /// created by a transfer to an alias:
    /// </para>
    /// <code>
    /// var receipts = await client.GetAllReceiptsAsync(transferReceipt.TransactionId);
    /// var address = receipts.Children.OfType&lt;CreateAccountReceipt&gt;().Single().Address;
    /// </code>
    /// <para>
    /// Each receipt is returned as the most specific type the network's
    /// response supports (<see cref="CreateAccountReceipt"/>,
    /// <see cref="CreateTokenReceipt"/> and so on), falling back to
    /// <see cref="TransactionReceipt"/> when the response carries no
    /// type-specific identifier.
    /// </para>
    /// <para>
    /// That resolution is <b>best-effort</b>. Four receipt kinds are identified
    /// by fields the wire format cannot distinguish from unset when their value
    /// is zero — <see cref="TokenReceipt"/> (a supply burned to nothing),
    /// <see cref="NftMintReceipt"/>, <see cref="ConsensusNodeReceipt"/> (node
    /// id 0) and <see cref="RegisteredNodeReceipt"/> — and resolve to the base
    /// type in those cases. The <c>OfType</c> idiom above quietly returns
    /// nothing rather than throwing, and <see cref="Enumerable.Single{T}(IEnumerable{T})"/>
    /// will then fail; where a supply may reach zero, match with an <c>is</c>
    /// pattern and treat the base type as the zero case. No data is lost, since
    /// the fallback occurs only when the value is zero.
    /// </para>
    /// <para>
    /// When the node has no verdict to give — it holds no receipt for the
    /// ID, stays too busy to answer, or still reports the receipt as
    /// unknown when the retries run out — the outcome is unknown and a
    /// <see cref="ConsensusException"/> is thrown.  With
    /// <see cref="IConsensusContext.ThrowIfNotSuccess"/> set to
    /// <code>false</code>, the method returns instead, with a
    /// <see cref="TransactionReceipts.Parent"/> whose status is
    /// <see cref="ResponseCode.ReceiptNotFound"/> or
    /// <see cref="ResponseCode.Unknown"/>.  Nodes keep receipts for only
    /// about three minutes after consensus, so neither proves the
    /// transaction never executed; settle it on a mirror node.  A parent
    /// receipt that reached consensus with a failure status is returned as
    /// it is, whatever the setting.
    /// </para>
    /// </remarks>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="transaction">
    /// TransactionId identifier of the receipt.
    /// </param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify
    /// the execution configuration for just this method call.
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// The receipts associated with the identified transaction, grouped
    /// into the transaction's own receipt, its child receipts and any
    /// duplicate-submission receipts.  Any of those groups may be empty.
    /// </returns>
    /// <exception cref="ConsensusException">If the node gave no verdict: it holds no receipt for the
    /// transaction, stayed too busy to answer, or still reported it as unknown when the retries ran out.
    /// Not thrown when <see cref="IConsensusContext.ThrowIfNotSuccess"/> is <code>false</code>.</exception>
    public static async Task<TransactionReceipts> GetAllReceiptsAsync(this ConsensusClient client, TransactionId transaction, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        await using var context = client.BuildChildContext(configure);
        var transactionId = new TransactionID(transaction);
        INetworkQuery query = new TransactionGetReceiptQuery
        {
            TransactionID = transactionId,
            IncludeDuplicates = true,
            IncludeChildReceipts = true
        };
        var response = await Engine.SubmitMessageAsync(context, query.CreateEnvelope(), query.InstantiateNetworkRequestMethod, shouldRetry, cancellationToken).ConfigureAwait(false);
        var answer = response.TransactionGetReceipt;
        var responseCode = answer.Header.NodeTransactionPrecheckCode;
        var parent = answer.Receipt;
        // The same reading of an answer without a verdict as GetReceiptAsync:
        // a node too busy to answer gave none (Unknown), and a node with no
        // receipt to give reports its code (ReceiptNotFound).
        ResponseCodeEnum? noVerdict = responseCode switch
        {
            ResponseCodeEnum.Busy => ResponseCodeEnum.Unknown,
            ResponseCodeEnum.Unknown or ResponseCodeEnum.ReceiptNotFound when parent is null => responseCode,
            ResponseCodeEnum.Ok when parent?.Status == ResponseCodeEnum.Unknown => ResponseCodeEnum.Unknown,
            _ => null
        };
        if (noVerdict is not null)
        {
            if (context.ThrowIfNotSuccess)
            {
                var message = responseCode == ResponseCodeEnum.Busy
                    ? "Network failed to respond to request for a transaction receipt, it is too busy. It is possible the network may still reach consensus for this transaction."
                    : $"Network failed to return a transaction receipt with a verdict, Status Code Returned: {noVerdict.Value}";
                // As GetReceiptAsync does, the exception carries the node's own
                // answer (BUSY included); only the stand-in maps BUSY to Unknown.
                throw new ConsensusException(message, transactionId.AsTransactionId(), (ResponseCode)(responseCode == ResponseCodeEnum.Ok ? noVerdict.Value : responseCode));
            }
            parent ??= new Proto.TransactionReceipt { Status = noVerdict.Value };
        }
        return Create(transactionId, parent, answer.ChildTransactionReceipts, answer.DuplicateTransactionReceipts);

        static bool shouldRetry(Response response)
        {
            return
                response.TransactionGetReceipt?.Header?.NodeTransactionPrecheckCode == ResponseCodeEnum.Busy ||
                response.TransactionGetReceipt?.Receipt?.Status == ResponseCodeEnum.Unknown;
        }
    }

    internal static TransactionReceipts Create(TransactionID transactionId, Proto.TransactionReceipt? parentReceipt, RepeatedField<Proto.TransactionReceipt>? childReceipts, RepeatedField<Proto.TransactionReceipt>? duplicateReceipts)
    {
        var childCount = childReceipts?.Count ?? 0;
        var duplicateCount = duplicateReceipts?.Count ?? 0;
        var children = Array.Empty<TransactionReceipt>();
        var duplicates = Array.Empty<TransactionReceipt>();
        if (childCount > 0)
        {
            // The network DOES NOT return the child transaction ID, so we have to synthesize it.
            children = new TransactionReceipt[childCount];
            for (var i = 0; i < childCount; i++)
            {
                var childTransactionId = transactionId.Clone();
                childTransactionId.Nonce = i + 1;
                children[i] = FromProtobuf(childTransactionId, childReceipts![i]);
            }
        }
        if (duplicateCount > 0)
        {
            duplicates = new TransactionReceipt[duplicateCount];
            for (var i = 0; i < duplicateCount; i++)
            {
                duplicates[i] = FromProtobuf(transactionId, duplicateReceipts![i]);
            }
        }
        return new TransactionReceipts
        {
            Parent = parentReceipt is not null ? FromProtobuf(transactionId, parentReceipt) : null,
            Children = children,
            Duplicates = duplicates
        };
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
        else if (receipt.ScheduleID != null)
        {
            return new ScheduleReceipt(transactionId, receipt);
        }
        else if (receipt.NodeId > 0)
        {
            return new ConsensusNodeReceipt(transactionId, receipt);
        }
        else if (receipt.RegisteredNodeId > 0)
        {
            return new RegisteredNodeReceipt(transactionId, receipt);
        }
        else
        {
            return new TransactionReceipt(transactionId, receipt);
        }
    }
}
