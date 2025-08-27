﻿using Grpc.Core;
using Grpc.Net.Client;
using Hiero.Implementation;
using System;
using System.Threading;

namespace Proto;

public sealed partial class GetAccountDetailsQuery : INetworkQuery
{
    Query INetworkQuery.CreateEnvelope()
    {
        return new Query { AccountDetails = this };
    }
    Func<Query, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<Response>> INetworkQuery.InstantiateNetworkRequestMethod(GrpcChannel channel)
    {
        return new NetworkService.NetworkServiceClient(channel).getAccountDetailsAsync;
    }
    void INetworkQuery.SetHeader(QueryHeader header)
    {
        Header = header;
    }
}