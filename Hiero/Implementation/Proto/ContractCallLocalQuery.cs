using Grpc.Core;
using Grpc.Net.Client;
using Hiero;
using Hiero.Implementation;

namespace Proto;

public sealed partial class ContractCallLocalQuery : INetworkQuery
{
    internal bool ThrowOnFail { get; set; } = true;
    Query INetworkQuery.CreateEnvelope()
    {
        return new Query { ContractCallLocal = this };
    }
    Func<Query, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<Response>> INetworkQuery.InstantiateNetworkRequestMethod(GrpcChannel channel)
    {
        return new SmartContractService.SmartContractServiceClient(channel).contractCallLocalMethodAsync;
    }
    void INetworkQuery.SetHeader(QueryHeader header)
    {
        Header = header;
    }
    void INetworkQuery.CheckResponse(TransactionID transactionId, Response response)
    {
        var header = response.ResponseHeader;
        if (header == null)
        {
            throw new PrecheckException($"Transaction Failed to Produce a Response.", transactionId.AsTxId(), ResponseCode.Unknown, 0);
        }
        else if (response.ContractCallLocal?.FunctionResult == null)
        {
            throw new PrecheckException($"Transaction Failed Pre-Check: {header.NodeTransactionPrecheckCode}", transactionId.AsTxId(), (ResponseCode)header.NodeTransactionPrecheckCode, header.Cost);
        }
        else if (ThrowOnFail && header.NodeTransactionPrecheckCode != ResponseCodeEnum.Ok)
        {
            throw new ContractException(
                $"Contract Query Failed with Code: {header.NodeTransactionPrecheckCode}",
                transactionId.AsTxId(),
                (ResponseCode)header.NodeTransactionPrecheckCode,
                header.Cost,
                new ContractCallResult(response));
        }
    }
}