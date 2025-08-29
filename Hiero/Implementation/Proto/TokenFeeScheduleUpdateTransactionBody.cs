using Grpc.Core;
using Grpc.Net.Client;
using Hiero.Implementation;

namespace Proto;

public sealed partial class TokenFeeScheduleUpdateTransactionBody : INetworkTransaction
{
    SchedulableTransactionBody INetworkTransaction.CreateSchedulableTransactionBody()
    {
        return new SchedulableTransactionBody { TokenFeeScheduleUpdate = this };
    }
    TransactionBody INetworkTransaction.CreateTransactionBody()
    {
        return new TransactionBody { TokenFeeScheduleUpdate = this };
    }
    Func<Transaction, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TransactionResponse>> INetworkTransaction.InstantiateNetworkRequestMethod(
        GrpcChannel channel)
    {
        return new TokenService.TokenServiceClient(channel).updateTokenFeeScheduleAsync;
    }
}