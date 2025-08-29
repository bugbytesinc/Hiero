using Grpc.Core;
using Grpc.Net.Client;
using Hiero.Implementation;

namespace Proto;

public sealed partial class ConsensusGetTopicInfoQuery : INetworkQuery
{
    Query INetworkQuery.CreateEnvelope()
    {
        return new Query { ConsensusGetTopicInfo = this };
    }
    Func<Query, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<Response>> INetworkQuery.InstantiateNetworkRequestMethod(GrpcChannel channel)
    {
        return new ConsensusService.ConsensusServiceClient(channel).getTopicInfoAsync;
    }
    void INetworkQuery.SetHeader(QueryHeader header)
    {
        Header = header;
    }
}