using Grpc.Core;
using Grpc.Net.Client;
using Hiero.Implementation;
using System;
using System.Threading;

namespace Proto;

public sealed partial class AtomicBatchTransactionBody : INetworkTransaction
{
    SchedulableTransactionBody INetworkTransaction.CreateSchedulableTransactionBody()
    {
        throw new InvalidOperationException("Unable to Schedule a batch transactions.");
    }
    TransactionBody INetworkTransaction.CreateTransactionBody()
    {
        return new TransactionBody { AtomicBatch = this };
    }
    Func<Transaction, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TransactionResponse>> INetworkTransaction.InstantiateNetworkRequestMethod(GrpcChannel channel)
    {
        return new UtilService.UtilServiceClient(channel).atomicBatchAsync;
    }
}