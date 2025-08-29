using Grpc.Core;
using Grpc.Net.Client;
using Hiero.Implementation;

namespace Proto;

public sealed partial class SystemUndeleteTransactionBody : INetworkTransaction
{
    SchedulableTransactionBody INetworkTransaction.CreateSchedulableTransactionBody()
    {
        return new SchedulableTransactionBody { SystemUndelete = this };
    }
    TransactionBody INetworkTransaction.CreateTransactionBody()
    {
        return new TransactionBody { SystemUndelete = this };
    }
    Func<Transaction, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TransactionResponse>> INetworkTransaction.InstantiateNetworkRequestMethod(GrpcChannel channel)
    {
#pragma warning disable CS0612 // Type or member is obsolete
        return FileID is not null ?
            new FileService.FileServiceClient(channel).systemUndeleteAsync :
            new SmartContractService.SmartContractServiceClient(channel).systemUndeleteAsync;
#pragma warning restore CS0612 // Type or member is obsolete
    }
}