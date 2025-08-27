using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Threading;

namespace Hiero.Implementation;

internal interface INetworkTransaction
{
    Proto.TransactionBody CreateTransactionBody();
    Proto.SchedulableTransactionBody CreateSchedulableTransactionBody();
    Func<Proto.Transaction, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<Proto.TransactionResponse>> InstantiateNetworkRequestMethod(GrpcChannel channel);
}