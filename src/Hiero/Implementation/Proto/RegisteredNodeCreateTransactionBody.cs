// SPDX-License-Identifier: Apache-2.0
using Grpc.Core;
using Grpc.Net.Client;
using Hiero.Implementation;

namespace Com.Hedera.Hapi.Node.Addressbook;

public sealed partial class RegisteredNodeCreateTransactionBody : INetworkTransaction
{
    Proto.SchedulableTransactionBody INetworkTransaction.CreateSchedulableTransactionBody()
    {
        return new Proto.SchedulableTransactionBody { RegisteredNodeCreate = this };
    }
    Proto.TransactionBody INetworkTransaction.CreateTransactionBody()
    {
        return new Proto.TransactionBody { RegisteredNodeCreate = this };
    }
    Func<Proto.Transaction, Metadata?, DateTime?, CancellationToken, AsyncUnaryCall<Proto.TransactionResponse>> INetworkTransaction.InstantiateNetworkRequestMethod(GrpcChannel channel)
    {
        return new Proto.AddressBookService.AddressBookServiceClient(channel).createRegisteredNodeAsync;
    }
}
