using Grpc.Core;
using Grpc.Net.Client;
using Hiero.Implementation;

namespace Com.Hedera.Hapi.Node.Addressbook;

public sealed partial class NodeUpdateTransactionBody : INetworkTransaction
{
    Proto.SchedulableTransactionBody INetworkTransaction.CreateSchedulableTransactionBody()
    {
        return new Proto.SchedulableTransactionBody { NodeUpdate = this };
    }
    Proto.TransactionBody INetworkTransaction.CreateTransactionBody()
    {
        return new Proto.TransactionBody { NodeUpdate = this };
    }
    Func<Proto.Transaction, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<Proto.TransactionResponse>> INetworkTransaction.InstantiateNetworkRequestMethod(GrpcChannel channel)
    {
        return new Proto.AddressBookService.AddressBookServiceClient(channel).updateNodeAsync;
    }
}
