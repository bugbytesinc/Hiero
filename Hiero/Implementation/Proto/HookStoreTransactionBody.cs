using Grpc.Core;
using Grpc.Net.Client;
using Hiero.Implementation;
using Proto;

namespace Com.Hedera.Hapi.Node.Hooks;

public sealed partial class HookStoreTransactionBody : INetworkTransaction
{
    SchedulableTransactionBody INetworkTransaction.CreateSchedulableTransactionBody()
    {
        throw new InvalidOperationException("This is not a schedulable transaction type.");
    }
    TransactionBody INetworkTransaction.CreateTransactionBody()
    {
        return new TransactionBody { HookStore = this };
    }
    Func<Transaction, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TransactionResponse>> INetworkTransaction.InstantiateNetworkRequestMethod(GrpcChannel channel)
    {
        return new SmartContractService.SmartContractServiceClient(channel).hookStoreAsync;
    }
}
