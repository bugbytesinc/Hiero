using Grpc.Core;
using Grpc.Net.Client;
using Hiero.Implementation;

namespace Proto;

public sealed partial class ConsensusUpdateTopicTransactionBody : INetworkTransaction
{
    SchedulableTransactionBody INetworkTransaction.CreateSchedulableTransactionBody()
    {
        return new SchedulableTransactionBody { ConsensusUpdateTopic = this };
    }
    TransactionBody INetworkTransaction.CreateTransactionBody()
    {
        return new TransactionBody { ConsensusUpdateTopic = this };
    }
    Func<Transaction, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TransactionResponse>> INetworkTransaction.InstantiateNetworkRequestMethod(GrpcChannel channel)
    {
        return new ConsensusService.ConsensusServiceClient(channel).updateTopicAsync;
    }
}