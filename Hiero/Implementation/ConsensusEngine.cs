using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Proto;

namespace Hiero.Implementation;

internal static class ConsensusEngine
{
    internal static async Task<T> ExecuteNetworkParamsAsync<T>(this ConsensusClient client, INetworkParams networkParams, Action<IConsensusContext>? configure) where T : TransactionReceipt
    {
        await using var context = client.CreateChildContext(configure);
        var (networkTransaction, signedTransactionBytes, transactionId, cancellationToken) = await CreateSignedTransactionBytesAsync(context, networkParams, true);
        return await ExecuteSignedTransactionBytesAsync<T>(context, signedTransactionBytes, networkParams, networkTransaction, transactionId, cancellationToken).ConfigureAwait(false);
    }
    internal async static Task<(INetworkTransaction, ByteString, TransactionID, CancellationToken)> CreateSignedTransactionBytesAsync(GossipContextStack context, INetworkParams networkParams, bool failIfNoSignatures)
    {
        var gateway = context.Endpoint;
        if (gateway is null)
        {
            throw new InvalidOperationException("The Network Gateway Node has not been configured. Please check that 'Gateway' is set in the Client context.");
        }
        var networkTransaction = networkParams.CreateNetworkTransaction();
        var cancellationToken = networkParams.CancellationToken ?? default;
        var signatory = GatherSignatories(context, networkParams.Signatory);
        var schedule = signatory.GetSchedule();
        if (schedule is not null)
        {
            var scheduledTransactionBody = networkTransaction.CreateSchedulableTransactionBody();
            scheduledTransactionBody.TransactionFee = (ulong)context.FeeLimit;
            networkTransaction = new ScheduleCreateTransactionBody
            {
                ScheduledTransactionBody = scheduledTransactionBody,
                AdminKey = schedule.Administrator is null ? null : new Key(schedule.Administrator),
                PayerAccountID = schedule.PendingPayer is null ? null : new AccountID(schedule.PendingPayer),
                ExpirationTime = schedule.Expiration is null ? null : new Proto.Timestamp(schedule.Expiration.Value),
                WaitForExpiry = schedule.DelayExecution,
                Memo = schedule.Memo ?? ""
            };
        }
        var transactionBody = networkTransaction.CreateTransactionBody();
        transactionBody.TransactionFee = (ulong)context.FeeLimit;
        transactionBody.NodeAccountID = new AccountID(gateway);
        transactionBody.TransactionID = GetOrCreateTransactionID(context);
        transactionBody.TransactionValidDuration = new Proto.Duration(context.TransactionDuration);
        transactionBody.Memo = context.Memo ?? "";
        var invoice = new Invoice(transactionBody, context.SignaturePrefixTrimLimit, cancellationToken);
        await signatory.SignAsync(invoice).ConfigureAwait(false);
        return (networkTransaction, invoice.GenerateSignedTransactionFromSignatures(failIfNoSignatures).ToByteString(), transactionBody.TransactionID, cancellationToken);
    }
    internal static async Task<T> ExecuteSignedTransactionBytesAsync<T>(GossipContextStack context, ByteString signedTransactionBytes, INetworkParams networkParams, INetworkTransaction networkTransaction, TransactionID transactionId, CancellationToken cancellationToken) where T : TransactionReceipt
    {
        var transaction = new Transaction { SignedTransactionBytes = signedTransactionBytes };
        var precheck = await ExecuteSignedRequestWithRetryImplementationAsync(context, transaction, networkTransaction.InstantiateNetworkRequestMethod, getResponseCode, cancellationToken).ConfigureAwait(false);
        if (precheck.NodeTransactionPrecheckCode != ResponseCodeEnum.Ok)
        {
            var responseCode = (ResponseCode)precheck.NodeTransactionPrecheckCode;
            throw new PrecheckException($"Transaction Failed Pre-Check: {responseCode}", transactionId.AsTxId(), responseCode, precheck.Cost);
        }
        var result = await GetReceiptAsync(context, transactionId, cancellationToken).ConfigureAwait(false);
        var receipt = networkParams.CreateReceipt(transactionId, result);
        if (receipt.Status != ResponseCode.Success && context.ThrowIfNotSuccess)
        {
            if (networkTransaction is ScheduleCreateTransactionBody)
            {
                throw new TransactionException($"Scheduling {networkParams.OperationDescription} failed with status: {receipt.Status}", receipt);
            }
            throw new TransactionException($"{networkParams.OperationDescription} failed with status: {receipt.Status}", receipt);
        }
        return (T)receipt;

        static ResponseCodeEnum getResponseCode(TransactionResponse response)
        {
            return response.NodeTransactionPrecheckCode;
        }
    }
    internal static Task<Response> CreateAndExecuteQueryAsync(this ConsensusClient client, IQueryParams queryParams, Action<IConsensusContext>? configure)
    {
        return client.ExecuteQueryAsync(queryParams.CreateNetworkQuery(), queryParams.CancellationToken ?? default, configure);
    }
    internal static async Task<Response> ExecuteQueryAsync(this ConsensusClient client, INetworkQuery networkQuery, CancellationToken cancellationToken, Action<IConsensusContext>? configure)
    {
        await using var context = client.CreateChildContext(configure);
        return await ExecuteQueryInContextAsync(networkQuery, context, cancellationToken).ConfigureAwait(false);

    }
    internal static async Task<Response> ExecuteQueryInContextAsync(INetworkQuery networkQuery, GossipContextStack context, CancellationToken cancellationToken)
    {
        var envelope = networkQuery.CreateEnvelope();
        networkQuery.SetHeader(new QueryHeader
        {
            Payment = new Transaction { SignedTransactionBytes = ByteString.Empty },
            ResponseType = ResponseType.CostAnswer
        });
        var response = await executeAskQuery().ConfigureAwait(false);
        ulong cost = response.ResponseHeader?.Cost ?? 0UL;
        if (cost > 0)
        {
            var transactionId = ConsensusEngine.GetOrCreateTransactionID(context);
            networkQuery.SetHeader(await ConsensusEngine.CreateSignedQueryHeader(context, (long)cost, transactionId, cancellationToken).ConfigureAwait(false));
            response = await executeSignedQuery().ConfigureAwait(false);
            networkQuery.CheckResponse(transactionId, response);
        }
        return response;

        async Task<Response> executeAskQuery()
        {
            var answer = await ConsensusEngine.ExecuteNetworkRequestWithRetryAsync(context, envelope, networkQuery.InstantiateNetworkRequestMethod, shouldRetryRequest, cancellationToken).ConfigureAwait(false);
            var code = answer.ResponseHeader?.NodeTransactionPrecheckCode ?? ResponseCodeEnum.Unknown;
            if (code != ResponseCodeEnum.Ok)
            {
                if (code == ResponseCodeEnum.NotSupported)
                {
                    // This may be a backdoor call that must be signed by a superuser account.
                    // It will not answer a COST_ASK without a signature.  Try signing with an
                    // empty transfer instead, this is not the most efficient, but we're already
                    // in a failure mode and performance is already broken.
                    var transactionId = ConsensusEngine.GetOrCreateTransactionID(context);
                    networkQuery.SetHeader(await ConsensusEngine.CreateSignedQueryHeader(context, 0, transactionId, cancellationToken).ConfigureAwait(false));
                    answer = await executeSignedQuery().ConfigureAwait(false);
                    // If we get a valid repsonse back, it turns out that we needed to identify
                    // ourselves with the signature, the rest of the process can proceed as normal.
                    // If it was a failure then we fall back to the original NOT_SUPPORTED error
                    // we received on the first attempt.
                    if (answer.ResponseHeader?.NodeTransactionPrecheckCode == ResponseCodeEnum.Ok)
                    {
                        return answer;
                    }
                }
                throw new PrecheckException($"Transaction Failed Pre-Check: {code}", TransactionId.None, (ResponseCode)code, 0);
            }
            return answer;

            static bool shouldRetryRequest(Response response)
            {
                return ResponseCodeEnum.Busy == response.ResponseHeader?.NodeTransactionPrecheckCode;
            }
        }

        Task<Response> executeSignedQuery()
        {
            return ConsensusEngine.ExecuteSignedRequestWithRetryImplementationAsync(context, envelope, networkQuery.InstantiateNetworkRequestMethod, getResponseCode, cancellationToken);

            static ResponseCodeEnum getResponseCode(Response response)
            {
                return response.ResponseHeader?.NodeTransactionPrecheckCode ?? ResponseCodeEnum.Unknown;
            }
        }
    }
    internal static ISignatory GatherSignatories(GossipContextStack context, params Signatory?[] extraSignatories)
    {
        var signatories = new List<Signatory>(1 + extraSignatories.Length);
        var contextSignatory = context.Signatory;
        if (contextSignatory is not null)
        {
            signatories.Add(contextSignatory);
        }
        foreach (var extraSignatory in extraSignatories)
        {
            if (extraSignatory is not null)
            {
                signatories.Add(extraSignatory);
            }
        }
        return signatories.Count == 1 ?
            signatories[0] :
            new Signatory(signatories.ToArray());
    }
    internal static TransactionID GetOrCreateTransactionID(GossipContextStack context)
    {
        var preExistingTransaction = context.TransactionId;
        if (preExistingTransaction is null)
        {
            var payer = context.Payer;
            return payer is null
                ? throw new InvalidOperationException("The Payer address has not been configured. Please check that 'Payer' is set in the Client context.")
                : createTransactionID(context, payer);
        }
        else if (preExistingTransaction.Scheduled)
        {
            throw new ArgumentException("Can not set the context's Transaction ID's Pending field of a transaction to true.", nameof(context.TransactionId));
        }
        else
        {
            return new TransactionID(preExistingTransaction);
        }

        static TransactionID createTransactionID(GossipContextStack context, EntityId payer)
        {
            var (seconds, nanos) = Epoch.UniqueSecondsAndNanos(context.AdjustForLocalClockDrift);
            return new TransactionID
            {
                AccountID = new AccountID(payer),
                TransactionValidStart = new Proto.Timestamp
                {
                    Seconds = seconds,
                    Nanos = nanos
                }
            };
        }
    }
    internal static Task<TResponse> ExecuteSignedRequestWithRetryImplementationAsync<TRequest, TResponse>(GossipContextStack context, TRequest request, Func<GrpcChannel, Func<TRequest, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TResponse>>> instantiateRequestMethod, Func<TResponse, ResponseCodeEnum> getResponseCode, CancellationToken cancellationToken) where TRequest : IMessage where TResponse : IMessage
    {
        var trackTimeDrift = context.AdjustForLocalClockDrift && context.TransactionId is null;
        var startingInstant = trackTimeDrift ? Epoch.UniqueClockNanos() : 0;

        return ExecuteNetworkRequestWithRetryAsync(context, request, instantiateRequestMethod, shouldRetryRequest, cancellationToken);

        bool shouldRetryRequest(TResponse response)
        {
            var code = getResponseCode(response);
            if (trackTimeDrift && code == ResponseCodeEnum.InvalidTransactionStart)
            {
                Epoch.AddToClockDrift(Epoch.UniqueClockNanos() - startingInstant);
            }
            return
                code == ResponseCodeEnum.Busy ||
                code == ResponseCodeEnum.InvalidTransactionStart;
        }
    }
    internal static async Task<TResponse> ExecuteNetworkRequestWithRetryAsync<TRequest, TResponse>(GossipContextStack context, TRequest request, Func<GrpcChannel, Func<TRequest, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TResponse>>> instantiateRequestMethod, Func<TResponse, bool> shouldRetryRequest, CancellationToken cancellationToken) where TRequest : IMessage where TResponse : IMessage
    {
        try
        {
            var retryCount = 0;
            var maxRetries = context.RetryCount;
            var retryDelay = context.RetryDelay;
            var callOnSendingHandlers = InstantiateOnSendingRequestHandler(context);
            var callOnResponseReceivedHandlers = InstantiateOnResponseReceivedHandler(context);
            var sendRequest = instantiateRequestMethod(context.GetChannel());
            callOnSendingHandlers(request);
            cancellationToken.ThrowIfCancellationRequested();
            for (; retryCount < maxRetries; retryCount++)
            {
                try
                {
                    var tenativeResponse = await sendRequest(request, null, null, cancellationToken);
                    callOnResponseReceivedHandlers(retryCount, tenativeResponse);
                    if (!shouldRetryRequest(tenativeResponse))
                    {
                        return tenativeResponse;
                    }
                }
                catch (RpcException rpcex) when (request is Query query && query.QueryCase == Query.QueryOneofCase.TransactionGetReceipt)
                {
                    var channel = context.GetChannel();
                    var message = channel.State == ConnectivityState.Connecting ?
                        $"Unable to communicate with network node {channel.Target} while retrieving receipt, it may be down or not reachable." :
                        $"Unable to communicate with network node {channel.Target} while retrieving receipt: {rpcex.Status}";
                    callOnResponseReceivedHandlers(retryCount, new StringValue { Value = message });
                }
                catch (RpcException rpcex) when (rpcex.StatusCode == StatusCode.Unavailable || rpcex.StatusCode == StatusCode.Unknown || rpcex.StatusCode == StatusCode.Cancelled)
                {
                    var channel = context.GetChannel();
                    var message = channel.State == ConnectivityState.Connecting ?
                        $"Unable to communicate with network node {channel.Target}, it may be down or not reachable." :
                        $"Unable to communicate with network node {channel.Target}: {rpcex.Status}";
                    callOnResponseReceivedHandlers(retryCount, new StringValue { Value = message });

                    if (request is Transaction transaction)
                    {
                        // If this was a networkTransaction, it may have actully successfully been processed, in which case 
                        // the receipt will already be in the system.  Check to see if it is there.
                        await Task.Delay(retryDelay * retryCount, cancellationToken).ConfigureAwait(false);
                        var receiptResponse = await CheckForReceipt(transaction).ConfigureAwait(false);
                        callOnResponseReceivedHandlers(retryCount, receiptResponse);
                        if (receiptResponse.NodeTransactionPrecheckCode != ResponseCodeEnum.ReceiptNotFound &&
                            receiptResponse is TResponse tenativeResponse &&
                            !shouldRetryRequest(tenativeResponse))
                        {
                            return tenativeResponse;
                        }
                    }
                    else if (request is Query query)
                    {
                        // If this was a networkQuery, it may not have made it to the node and we can retry.  However,
                        // if we receive a duplicate networkTransaction error, it means the node accepted the payment
                        // and terminated the connection before returning results, in which case funds are lost.
                        // For that scenario, re-throw the original exception and it will be caught and translated
                        // into a PrecheckException with the appropriate error message.
                        var retryQueryResponse = await RetryQuery();
                        if ((retryQueryResponse as Response)?.ResponseHeader?.NodeTransactionPrecheckCode == ResponseCodeEnum.DuplicateTransaction)
                        {
                            throw;
                        }
                        if (!shouldRetryRequest(retryQueryResponse))
                        {
                            return retryQueryResponse;
                        }
                    }
                }
                await Task.Delay(retryDelay * (retryCount + 1), cancellationToken).ConfigureAwait(false);
            }
            var finalResponse = await sendRequest(request, null, null, cancellationToken);
            callOnResponseReceivedHandlers(maxRetries, finalResponse);
            return finalResponse;

            async Task<TransactionResponse> CheckForReceipt(Transaction transaction)
            {
                // In the case we submitted a networkTransaction, the receipt may actually
                // be in the system.  Unpacking the networkTransaction is not necessarily efficient,
                // however we are here due to edge case error condition due to poor network 
                // performance or grpc connection issues already.
                if (transaction != null)
                {
                    var transactionId = ExtractTransactionID(transaction);
                    var query = new Query
                    {
                        TransactionGetReceipt = new TransactionGetReceiptQuery
                        {
                            TransactionID = transactionId
                        }
                    };
                    for (; retryCount < maxRetries; retryCount++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            var client = new CryptoService.CryptoServiceClient(context.GetChannel());
                            var receipt = await client.getTransactionReceiptsAsync(query, null, null, cancellationToken);
                            return new TransactionResponse { NodeTransactionPrecheckCode = receipt.TransactionGetReceipt.Header.NodeTransactionPrecheckCode };
                        }
                        catch (RpcException rpcex) when (rpcex.StatusCode == StatusCode.Unavailable || rpcex.StatusCode == StatusCode.Unknown || rpcex.StatusCode == StatusCode.Cancelled)
                        {
                            var channel = context.GetChannel();
                            var message = channel.State == ConnectivityState.Connecting ?
                                $"Unable to communicate with network node {channel.Target}, it may be down or not reachable." :
                                $"Unable to communicate with network node {channel.Target}: {rpcex.Status}";
                            callOnResponseReceivedHandlers(retryCount, new StringValue { Value = message });
                        }
                        await Task.Delay(retryDelay * (retryCount + 1), cancellationToken).ConfigureAwait(false);
                    }
                }
                return new TransactionResponse { NodeTransactionPrecheckCode = ResponseCodeEnum.Unknown };
            }

            async Task<TResponse> RetryQuery()
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        await Task.Delay(retryDelay * retryCount, cancellationToken).ConfigureAwait(false);
                        return await sendRequest(request, null, null, cancellationToken);
                    }
                    catch (RpcException rpcex) when ((rpcex.StatusCode == StatusCode.Unavailable || rpcex.StatusCode == StatusCode.Unknown || rpcex.StatusCode == StatusCode.Cancelled) && retryCount < maxRetries - 1)
                    {
                        var channel = context.GetChannel();
                        var message = channel.State == ConnectivityState.Connecting ?
                            $"Unable to communicate with network node {channel.Target}, it may be down or not reachable." :
                            $"Unable to communicate with network node {channel.Target}: {rpcex.Status}";
                        callOnResponseReceivedHandlers(retryCount, new StringValue { Value = message });
                    }
                    retryCount++;
                }
            }
        }
        catch (RpcException rpcex) when (request is Query query)
        {
            var channel = context.GetChannel();
            var message = rpcex.StatusCode == StatusCode.Unavailable && channel.State == ConnectivityState.Connecting ?
                $"Unable to communicate with network node {channel.Target}, it may be down or not reachable, or accepted payment and terminated the connection before returning Query results." :
                $"Unable to communicate with network node {channel.Target}: {rpcex.Status}, it may have accepted payment and terminated the connection before returning Query results.";
            throw new PrecheckException(message, TransactionId.None, ResponseCode.RpcError, 0, rpcex);
        }
        catch (RpcException rpcex)
        {
            var transactionId = (request is Transaction transaction) ? ExtractTransactionID(transaction) : null;
            var channel = context.GetChannel();
            var message = rpcex.StatusCode == StatusCode.Unavailable && channel.State == ConnectivityState.Connecting ?
                $"Unable to communicate with network node {channel.Target}, it may be down or not reachable." :
                $"Unable to communicate with network node {channel.Target}: {rpcex.Status}";
            throw new PrecheckException(message, transactionId.AsTxId(), ResponseCode.RpcError, 0, rpcex);
        }
    }
    internal static async Task<QueryHeader> CreateSignedQueryHeader(GossipContextStack context, long queryFee, TransactionID transactionId, CancellationToken cancellationToken)
    {
        var payer = context.Payer;
        if (payer is null)
        {
            throw new InvalidOperationException("The Payer address has not been configured. Please check that 'Payer' is set in the Client context.");
        }
        var signatory = context.Signatory as ISignatory;
        if (signatory is null)
        {
            throw new InvalidOperationException("The Payer's signatory (signing key/callback) has not been configured. This is required for retreiving records and other general network Queries. Please check that 'Signatory' is set in the Client context.");
        }
        var gateway = context.Endpoint;
        if (gateway is null)
        {
            throw new InvalidOperationException("The Network Gateway Node has not been configured. Please check that 'Gateway' is set in the Client context.");
        }
        queryFee += context.QueryTip;
        var feeLimit = context.FeeLimit;
        if (feeLimit < queryFee)
        {
            throw new InvalidOperationException($"The user specified fee limit is not enough for the anticipated query required fee of {queryFee:n0} tinybars.");
        }
        var transactionBody = new TransactionBody
        {
            TransactionID = transactionId,
            NodeAccountID = new AccountID(gateway),
            TransactionFee = (ulong)context.FeeLimit,
            TransactionValidDuration = new Proto.Duration(context.TransactionDuration),
            Memo = context.Memo ?? "",
            CryptoTransfer = new CryptoTransferTransactionBody { Transfers = createTransferList() }
        };
        var invoice = new Invoice(transactionBody, context.SignaturePrefixTrimLimit, cancellationToken);
        await signatory.SignAsync(invoice).ConfigureAwait(false);
        return new QueryHeader
        {
            Payment = new Transaction
            {
                SignedTransactionBytes = invoice.GenerateSignedTransactionFromSignatures(true).ToByteString()
            }
        };

        TransferList? createTransferList()
        {
            if (queryFee > 0)
            {
                var transfers = new TransferList();
                transfers.AccountAmounts.Add(new AccountAmount { AccountID = new AccountID(payer), Amount = -queryFee });
                transfers.AccountAmounts.Add(new AccountAmount { AccountID = new AccountID(gateway), Amount = queryFee });
                return transfers;
            }
            return null;
        }
    }
    private static Action<IMessage> InstantiateOnSendingRequestHandler(GossipContextStack context)
    {
        var handlers = context.GetAll<Action<IMessage>>(nameof(context.OnSendingRequest)).Where(h => h != null).ToArray();
        if (handlers.Length > 0)
        {
            return (IMessage request) => ExecuteHandlers(handlers, request);
        }
        else
        {
            return NoOp;
        }
        static void ExecuteHandlers(Action<IMessage>[] handlers, IMessage request)
        {
            var data = new ReadOnlyMemory<byte>(request.ToByteArray());
            foreach (var handler in handlers)
            {
                handler(request);
            }
        }
        static void NoOp(IMessage request)
        {
        }
    }
    private static Action<int, IMessage> InstantiateOnResponseReceivedHandler(GossipContextStack context)
    {
        var handlers = context.GetAll<Action<int, IMessage>>(nameof(context.OnResponseReceived)).Where(h => h != null).ToArray();
        if (handlers.Length > 0)
        {
            return (int tryNumber, IMessage response) => ExecuteHandlers(handlers, tryNumber, response);
        }
        else
        {
            return NoOp;
        }
        static void ExecuteHandlers(Action<int, IMessage>[] handlers, int tryNumber, IMessage response)
        {
            foreach (var handler in handlers)
            {
                handler(tryNumber, response);
            }
        }
        static void NoOp(int tryNumber, IMessage response)
        {
        }
    }
    /// <summary>
    /// Internal Helper function to retrieve receipt record provided by 
    /// the network following network consensus regarding a networkQuery or networkTransaction.
    /// </summary>
    internal static async Task<Proto.TransactionReceipt> GetReceiptAsync(GossipContextStack context, TransactionID transactionId, CancellationToken cancellationToken)
    {
        INetworkQuery query = new TransactionGetReceiptQuery { TransactionID = transactionId };
        var response = await ExecuteNetworkRequestWithRetryAsync(context, query.CreateEnvelope(), query.InstantiateNetworkRequestMethod, shouldRetry, cancellationToken).ConfigureAwait(false);
        if (!context.ThrowIfNotSuccess)
        {
            return response.TransactionGetReceipt.Receipt;
        }
        var responseCode = response.TransactionGetReceipt.Header.NodeTransactionPrecheckCode;
        switch (responseCode)
        {
            case ResponseCodeEnum.Ok:
                break;
            case ResponseCodeEnum.Busy:
                throw new ConsensusException("Network failed to respond to request for a transaction receipt, it is too busy. It is possible the network may still reach concensus for this transaction.", transactionId.AsTxId(), (ResponseCode)responseCode);
            case ResponseCodeEnum.Unknown:
            case ResponseCodeEnum.ReceiptNotFound:
                throw new TransactionException($"Network failed to return a transaction receipt, Status Code Returned: {responseCode}", new TransactionReceipt(transactionId, new() { Status = responseCode }));
        }
        var status = response.TransactionGetReceipt.Receipt.Status;
        switch (status)
        {
            case ResponseCodeEnum.Unknown:
                throw new ConsensusException("Network failed to reach concensus within the configured retry time window, It is possible the network may still reach concensus for this transaction.", transactionId.AsTxId(), (ResponseCode)status);
            case ResponseCodeEnum.TransactionExpired:
                throw new ConsensusException("Network failed to reach concensus before transaction request expired.", transactionId.AsTxId(), (ResponseCode)status);
            case ResponseCodeEnum.ReceiptNotFound:
                throw new ConsensusException("Network failed to find a receipt for given transaction.", transactionId.AsTxId(), (ResponseCode)status);
            default:
                return response.TransactionGetReceipt.Receipt;
        }

        static bool shouldRetry(Response response)
        {
            return
                response.TransactionGetReceipt?.Header?.NodeTransactionPrecheckCode == ResponseCodeEnum.Busy ||
                response.TransactionGetReceipt?.Receipt?.Status == ResponseCodeEnum.Unknown;
        }
    }
    private static TransactionID ExtractTransactionID(Transaction transaction)
    {
        var signedTransaction = SignedTransaction.Parser.ParseFrom(transaction.SignedTransactionBytes);
        var transactionBody = TransactionBody.Parser.ParseFrom(signedTransaction.BodyBytes);
        return transactionBody.TransactionID;
    }
}
