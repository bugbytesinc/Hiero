﻿using Grpc.Core;
using Grpc.Net.Client;
using Hiero;
using Hiero.Implementation;
using System;
using System.Threading;

namespace Proto;

public sealed partial class TransactionGetRecordQuery : INetworkQuery
{
    Query INetworkQuery.CreateEnvelope()
    {
        return new Query { TransactionGetRecord = this };
    }
    Func<Query, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<Response>> INetworkQuery.InstantiateNetworkRequestMethod(GrpcChannel channel)
    {
        return new CryptoService.CryptoServiceClient(channel).getTxRecordByTxIDAsync;
    }
    void INetworkQuery.SetHeader(QueryHeader header)
    {
        Header = header;
    }
    void INetworkQuery.CheckResponse(TransactionID transactionId, Response response)
    {
        // NOTE: Exceptions thrown from this check use the target
        // ID of the record being retrieved.  This is different than
        // what usually happens.  This is because most of the time, this
        // method is part of a XxxWithRecordAsync call, where we DO want
        // the TransactionId ID of the transaction we just executed.
        // (The TransactionId ID of the query to pay for this is not important 
        // in this context if we want to retry getting the record)
        var header = response.ResponseHeader;
        if (header == null)
        {
            throw new PrecheckException($"The Network failed to produce a response while retrieving the record.", TransactionID.AsTxId(), ResponseCode.Unknown, 0);
        }
        else
        {
            var precheckCode = header.NodeTransactionPrecheckCode;
            if (IncludeDuplicates)
            {
                if (precheckCode == ResponseCodeEnum.InsufficientTxFee)
                {
                    throw new TransactionException("The Network Changed the price of Retrieving Records while making this request, please try again.", new Hiero.TransactionReceipt(TransactionID, new() { Status = precheckCode }));
                }
                else if (precheckCode != ResponseCodeEnum.Ok && precheckCode != ResponseCodeEnum.RecordNotFound)
                {
                    throw new TransactionException("Unable to retrieve transaction record(s).", new Hiero.TransactionReceipt(TransactionID, new() { Status = precheckCode }));
                }
            }
            else
            {
                if (precheckCode == ResponseCodeEnum.InsufficientTxFee)
                {
                    throw new TransactionException("The Network Changed the price of Retrieving a Record while attempting to retrieve this record, the transaction likely succeeded, please try to retrieve the record again.", new Hiero.TransactionReceipt(TransactionID, new() { Status = precheckCode }));
                }
                else if (precheckCode != ResponseCodeEnum.Ok)
                {
                    throw new TransactionException("Unable to retrieve transaction record.", new Hiero.TransactionReceipt(TransactionID, new() { Status = precheckCode }));
                }
            }
        }
    }
}