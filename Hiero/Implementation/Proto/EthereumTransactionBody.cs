using Grpc.Core;
using Grpc.Net.Client;
using Hiero.Implementation;

namespace Proto;

public sealed partial class EthereumTransactionBody : INetworkTransaction
{
    SchedulableTransactionBody INetworkTransaction.CreateSchedulableTransactionBody()
    {
        throw new InvalidOperationException("This is not a schedulable transaction type.");
    }
    TransactionBody INetworkTransaction.CreateTransactionBody()
    {
        return new TransactionBody { EthereumTransaction = this };
    }
    Func<Transaction, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<TransactionResponse>> INetworkTransaction.InstantiateNetworkRequestMethod(GrpcChannel channel)
    {
        return new SmartContractService.SmartContractServiceClient(channel).callEthereumAsync;
    }
}