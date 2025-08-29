using Grpc.Core;
using Grpc.Net.Client;
using Hiero.Implementation;

namespace Proto;

public sealed partial class SystemDeleteTransactionBody : INetworkTransaction
{
    SchedulableTransactionBody INetworkTransaction.CreateSchedulableTransactionBody()
    {
        return new SchedulableTransactionBody { SystemDelete = this };
    }
    TransactionBody INetworkTransaction.CreateTransactionBody()
    {
        return new TransactionBody { SystemDelete = this };
    }
    Func<Transaction, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TransactionResponse>> INetworkTransaction.InstantiateNetworkRequestMethod(GrpcChannel channel)
    {
#pragma warning disable CS0612 // Type or member is obsolete
        return FileID is not null ?
            new FileService.FileServiceClient(channel).systemDeleteAsync :
            new SmartContractService.SmartContractServiceClient(channel).systemDeleteAsync;
#pragma warning restore CS0612 // Type or member is obsolete
    }
}