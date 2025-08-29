using Grpc.Core;
using Grpc.Net.Client;
using Hiero.Implementation;

namespace Proto;

public sealed partial class ConsensusCreateTopicTransactionBody : INetworkTransaction
{
    SchedulableTransactionBody INetworkTransaction.CreateSchedulableTransactionBody()
    {
        return new SchedulableTransactionBody { ConsensusCreateTopic = this };
    }
    TransactionBody INetworkTransaction.CreateTransactionBody()
    {
        return new TransactionBody { ConsensusCreateTopic = this };
    }
    Func<Transaction, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TransactionResponse>> INetworkTransaction.InstantiateNetworkRequestMethod(GrpcChannel channel)
    {
        return new ConsensusService.ConsensusServiceClient(channel).createTopicAsync;
    }
}