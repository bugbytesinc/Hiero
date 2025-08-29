using Grpc.Core;
using Grpc.Net.Client;
using Hiero.Implementation;

namespace Proto;

public sealed partial class ScheduleGetInfoQuery : INetworkQuery
{
    Query INetworkQuery.CreateEnvelope()
    {
        return new Query { ScheduleGetInfo = this };
    }
    Func<Query, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<Response>> INetworkQuery.InstantiateNetworkRequestMethod(GrpcChannel channel)
    {
        return new ScheduleService.ScheduleServiceClient(channel).getScheduleInfoAsync;
    }
    void INetworkQuery.SetHeader(QueryHeader header)
    {
        Header = header;
    }
}