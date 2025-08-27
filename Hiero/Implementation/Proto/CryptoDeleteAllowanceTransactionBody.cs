using Grpc.Core;
using Grpc.Net.Client;
using Hiero.Implementation;
using System;
using System.Threading;

namespace Proto;

public sealed partial class CryptoDeleteAllowanceTransactionBody : INetworkTransaction
{
    SchedulableTransactionBody INetworkTransaction.CreateSchedulableTransactionBody()
    {
        return new SchedulableTransactionBody { CryptoDeleteAllowance = this };
    }
    TransactionBody INetworkTransaction.CreateTransactionBody()
    {
        return new TransactionBody { CryptoDeleteAllowance = this };
    }
    Func<Transaction, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TransactionResponse>> INetworkTransaction.InstantiateNetworkRequestMethod(GrpcChannel channel)
    {
        return new CryptoService.CryptoServiceClient(channel).deleteAllowancesAsync;
    }
}