using Grpc.Core;
using Grpc.Net.Client;
using Hiero.Implementation;

namespace Proto;

public sealed partial class TokenUpdateTransactionBody : INetworkTransaction
{
    SchedulableTransactionBody INetworkTransaction.CreateSchedulableTransactionBody()
    {
        return new SchedulableTransactionBody { TokenUpdate = this };
    }
    TransactionBody INetworkTransaction.CreateTransactionBody()
    {
        return new TransactionBody { TokenUpdate = this };
    }
    Func<Transaction, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TransactionResponse>> INetworkTransaction.InstantiateNetworkRequestMethod(GrpcChannel channel)
    {
        return new TokenService.TokenServiceClient(channel).updateTokenAsync;
    }
}