using Grpc.Core;
using Grpc.Net.Client;
using Hiero;
using Hiero.Implementation;

namespace Proto;

public sealed partial class CryptoGetAccountRecordsQuery : INetworkQuery
{
    Query INetworkQuery.CreateEnvelope()
    {
        return new Query { CryptoGetAccountRecords = this };
    }
    Func<Query, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<Response>> INetworkQuery.InstantiateNetworkRequestMethod(GrpcChannel channel)
    {
        return new CryptoService.CryptoServiceClient(channel).getAccountRecordsAsync;
    }
    void INetworkQuery.SetHeader(QueryHeader header)
    {
        Header = header;
    }
    void INetworkQuery.CheckResponse(TransactionID transactionId, Response response)
    {
        var precheckCode = response.ResponseHeader?.NodeTransactionPrecheckCode ?? ResponseCodeEnum.Unknown;
        if (precheckCode != ResponseCodeEnum.Ok)
        {
            throw new TransactionException("Unable to retrieve transaction records.", new Hiero.TransactionReceipt(transactionId, new() { Status = precheckCode }));
        }
    }
}