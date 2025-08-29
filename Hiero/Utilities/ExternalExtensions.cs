using Google.Protobuf;
using Hiero.Implementation;
using Proto;
using System.ComponentModel;

namespace Hiero;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class ExternalExtensions
{
    /// <summary>
    /// Submits the given query represented in the protbuf encoded byte message to
    /// the network, returning the <see cref="Proto.Response"/> protobuf encoded as bytes.
    /// </summary>
    /// <remarks>
    /// Payment information attached to the query will be ignored, the algorithm will attempt 
    /// to query for free first, then sign with the Payer/Signatory pair contained within the 
    /// configuration if the query requires a payment.  Additionally, querying for a receipt 
    /// is a special case, it is never charged a query fee and the algorithm, when it recognizes 
    /// a receipt query, will wait for consensus if necessary before returning results.
    /// </remarks>
    /// <param name="client">
    /// The Consensus Node Client to query.
    /// </param>
    /// <param name="queryBytes">
    /// The encoded protobuf bytes of the query to perform.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify 
    /// the execution configuration for just this method call. 
    /// It is executed prior to submitting the request to the network.
    /// </param>
    /// <returns>
    /// The bytes of the <see cref="Proto.Response"/> representing the query results.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// If the bytes do not represent a valid Protobuf Encoded QueryAsync.
    /// </exception>
    public static async Task<ReadOnlyMemory<byte>> QueryExternalAsync(this ConsensusClient client, ReadOnlyMemory<byte> queryBytes, CancellationToken cancellationToken = default, Action<IConsensusContext>? configure = null)
    {
        try
        {
            if (queryBytes.IsEmpty)
            {
                throw new ArgumentException("Missing Query Bytes (was empty).", nameof(queryBytes));
            }
            var envelope = Query.Parser.ParseFrom(queryBytes.Span);
            var query = envelope.GetNetworkQuery();
            if (query is null)
            {
                throw new ArgumentException("The Query did not contain a request.", nameof(queryBytes));
            }
            await using var context = client.CreateChildContext(configure);
            query.SetHeader(new QueryHeader
            {
                Payment = new Transaction { SignedTransactionBytes = ByteString.Empty },
                ResponseType = ResponseType.CostAnswer
            });
            var response = envelope.QueryCase == Query.QueryOneofCase.TransactionGetReceipt ?
                await Engine.SubmitGrpcMessageWithRetry(context, envelope, query.InstantiateNetworkRequestMethod, shouldRetryReceiptQuery, cancellationToken).ConfigureAwait(false) :
                await Engine.SubmitGrpcMessageWithRetry(context, envelope, query.InstantiateNetworkRequestMethod, shouldRetryGenericQuery, cancellationToken).ConfigureAwait(false);
            if (response.ResponseHeader?.NodeTransactionPrecheckCode == ResponseCodeEnum.Ok && response.ResponseHeader?.Cost > 0)
            {
                var transactionId = Engine.GetOrCreateTransactionID(context);
                query.SetHeader(await Engine.CreateSignedQueryHeader(context, (long)response.ResponseHeader.Cost, transactionId, cancellationToken).ConfigureAwait(false));
                response = await Engine.SubmitTimeBoxedGrpcMessageWithRetry(context, envelope, query.InstantiateNetworkRequestMethod, getResponseCode, cancellationToken);
            }
            return response.ToByteArray();
        }
        catch (InvalidProtocolBufferException ipbe)
        {
            throw new ArgumentException("Query Bytes not recognized as valid Protobuf.", nameof(queryBytes), ipbe);
        }
        static bool shouldRetryReceiptQuery(Response response)
        {
            return
                response.TransactionGetReceipt?.Header?.NodeTransactionPrecheckCode == ResponseCodeEnum.Busy ||
                response.TransactionGetReceipt?.Receipt?.Status == ResponseCodeEnum.Unknown;
        }
        static bool shouldRetryGenericQuery(Response response)
        {
            return ResponseCodeEnum.Busy == response.ResponseHeader?.NodeTransactionPrecheckCode;
        }
        static ResponseCodeEnum getResponseCode(Response response)
        {
            return response.ResponseHeader?.NodeTransactionPrecheckCode ?? ResponseCodeEnum.Unknown;
        }
    }
    public static async Task<ReadOnlyMemory<byte>> PrepareExternalTransactionAsync(this ConsensusClient client, TransactionParams transactionParams, Action<IConsensusContext>? configure = null)
    {
        if (transactionParams is BatchedTransactionMetadata batchMetadata)
        {
            await using var configuredClient = client.Clone(configure);
            var batchParams = new BatchedTransactionParams { TransactionParams = [batchMetadata] };
            var outerNetworkParams = await BatchedParamsOrchestrator.CreateAsync(batchParams, configuredClient);
            var atomicBatch = (AtomicBatchTransactionBody)outerNetworkParams.CreateNetworkTransaction();
            return atomicBatch.Transactions[0].Memory;
        }
        await using var context = client.CreateChildContext(configure);
        var networkParams = transactionParams as INetworkParams ?? throw new ArgumentNullException(nameof(transactionParams), "External Transaction Params can not be null.");
        var (_, signedTransactionBytes, _, _) = await Engine.CreateSignedTransactionBytesAsync(context, networkParams, false);
        return signedTransactionBytes.Memory;
    }
}
