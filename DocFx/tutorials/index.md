---
title: Hiero - .NET Client Library for Hiero Network and Hedera Hashgraph
---
# What is Hiero?

Hiero provides access to the [Hedera Hashgraph](https://www.hedera.com/) Network for the .NET platform.  It manages the communication details with participating [network nodes](https://docs.hedera.com/hedera/networks/mainnet/mainnet-nodes) and provides an efficient set of asynchronous interface methods for consumption by .NET programs.

Hiero is built with [.NET 10](https://dotnet.microsoft.com/)

## How do I Install It?

Hiero is published in [NuGet](https://www.nuget.org/packages/Hiero/).  You can install it with your favorite NuGet client, for example from the command line:

```sh
dotnet add package Hiero
```

The library references a minimum of dependencies.  It relies on .NET's native [gRPC](https://docs.microsoft.com/en-us/aspnet/core/grpc/) libraries to access the Hedera network and utilizes the cryptographic services provided by the [Bouncy Castle Project](http://www.bouncycastle.org/).

## What does 'Hello World' for this Library Look like?

The most simple thing one can ask of the Hedera network is the balance of an account.  Here is an example console program:

```csharp
class Program
{
    static async Task Main(string[] args)
    {                                                 // For Example:
        var endpointUrl = args[0];                    //   https://2.testnet.hedera.com:50211
        var nodeAccountNo = long.Parse(args[1]);      //   5 (node 0.0.5)
        var queryAccountNo = long.Parse(args[2]);     //   2300 (account 0.0.2300)
        try
        {
            await using var client = new ConsensusClient(ctx =>
            {
                ctx.Endpoint = new ConsensusNodeEndpoint(
                    new EntityId(0, 0, nodeAccountNo),
                    new Uri(endpointUrl));
            });
            var account = new EntityId(0, 0, queryAccountNo);
            var balance = await client.GetAccountBalanceAsync(account);
            Console.WriteLine($"Account Balance for 0.0.{queryAccountNo} is {balance:#,#} tinybars.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
        }
    }
}
```

Hiero provides access to the Hedera network via the [`ConsensusClient`](xref:Hiero.ConsensusClient) object.  The [`ConsensusClient`](xref:Hiero.ConsensusClient) object orchestrates the request construction and communication with the Hedera network.  During creation, it requires a small amount of configuration.  At a minimum to retrieve an account balance, the client must be configured with an [`Endpoint`](xref:Hiero.IConsensusContext.Endpoint).  The [`ConsensusNodeEndpoint`](xref:Hiero.ConsensusNodeEndpoint) object represents the internet network address and account for the node processing requests.  The [`EntityId`](xref:Hiero.EntityId) is the identifier of the account to be queried.

## How do I learn more?

* [Examples](crypto/balance.md):  If you prefer to start with code you can copy then modify, we are working up simple examples for the major ways to interact with the network. So far we have examples for the following:
  * Crypto Transactions
    * [Get Account Balance](crypto/balance.md)
    * [Transfer Crypto](crypto/transfer.md)
    * [Get Account Info](crypto/info.md)
    * [Create New Account](crypto/create.md)
    * [Update Account](crypto/update.md)
    * [Delete Account](crypto/delete.md)
  * File Manipulation
    * [Create File](file/createfile.md)
  * Miscellaneous
    * [Fee Schedule](misc/fees.md)
    * [Exchange Rates](misc/rate.md)
    * [Suspend Network](misc/suspend.md)

* [API Documentation](~/api/Hiero.yml): We have API Documentation generated from the source code itself.  This is useful if you are looking for a low-level understanding of the moving pieces.

Our documentation is a work in progress and will be adding to it and improving over time as bandwidth permits.

## Is this project Open Source?

Yes, this is an open source project released under the [Apache-2.0 License](https://github.com/bugbytesinc/Hiero/blob/main/LICENSE), the source code can be found at https://github.com/bugbytesinc/Hiero.
