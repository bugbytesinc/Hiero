using Grpc.Core;
using Grpc.Net.Client;
using Hiero.Implementation;

namespace Proto;

public sealed partial class ScheduleCreateTransactionBody : INetworkTransaction
{
    SchedulableTransactionBody INetworkTransaction.CreateSchedulableTransactionBody()
    {
        throw new InvalidOperationException("Unable to Schedule a Scheduling transaction.");
    }
    TransactionBody INetworkTransaction.CreateTransactionBody()
    {
        return new TransactionBody { ScheduleCreate = this };
    }
    Func<Transaction, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TransactionResponse>> INetworkTransaction.InstantiateNetworkRequestMethod(GrpcChannel channel)
    {
        return new ScheduleService.ScheduleServiceClient(channel).createScheduleAsync;
    }
}