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
    {
        // Usage: dotnet run -- https://2.testnet.hedera.com:50211 0.0.5 0.0.98
        var endpointUrl = args[0];
        if (!EntityId.TryParseShardRealmNum(args[1], out var nodeAccount))
            throw new ArgumentException($"Invalid node account: {args[1]}");
        if (!EntityId.TryParseShardRealmNum(args[2], out var queryAccount))
            throw new ArgumentException($"Invalid query account: {args[2]}");

        try
        {
            await using var client = new ConsensusClient(ctx =>
            {
                ctx.Endpoint = new ConsensusNodeEndpoint(nodeAccount, new Uri(endpointUrl));
            });
            var balance = await client.GetAccountBalanceAsync(queryAccount);
            Console.WriteLine($"Account Balance for {queryAccount} is {balance:#,#} tinybars.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
        }
    }
}
```

Hiero provides access to the Hedera network via the [`ConsensusClient`](xref:Hiero.ConsensusClient) object.  The [`ConsensusClient`](xref:Hiero.ConsensusClient) object orchestrates the request construction and communication with the Hedera network.  During creation, it requires a small amount of configuration.  At a minimum to retrieve an account balance, the client must be configured with an [`Endpoint`](xref:Hiero.IConsensusContext.Endpoint).  The [`ConsensusNodeEndpoint`](xref:Hiero.ConsensusNodeEndpoint) object represents the internet network address and account for the node processing requests.  The [`EntityId`](xref:Hiero.EntityId) identifies accounts, tokens, topics, and other entities using the standard `shard.realm.num` format (e.g. `0.0.98`). Use [`EntityId.TryParseShardRealmNum`](xref:Hiero.EntityId.TryParseShardRealmNum*) to parse them from strings — this accepts the same format the Hedera portal gives you.

## How do I learn more?

* **Tutorials**: Step-by-step guides with code for every major workflow:
  * Crypto Transactions
    * [Get Account Balance](crypto/balance.md)
    * [Transfer Crypto](crypto/transfer.md)
    * [Get Account Info](crypto/info.md)
    * [Create New Account](crypto/create.md)
    * [Update Account](crypto/update.md)
    * [Delete Account](crypto/delete.md)
  * Fungible Tokens
    * [Create a Token](token/create.md)
    * [Mint Tokens](token/mint.md)
    * [Transfer Tokens](token/transfer.md)
    * [Associate and Dissociate](token/associate.md)
  * Non-Fungible Tokens (NFTs)
    * [Create an NFT Collection](nft/create.md)
    * [Mint an NFT](nft/mint.md)
    * [Transfer an NFT](nft/transfer.md)
  * Consensus Service (HCS)
    * [Create a Topic](consensus/createtopic.md)
    * [Submit a Message](consensus/submit.md)
    * [Subscribe to a Topic](consensus/subscribe.md)
  * Smart Contracts
    * [Deploy and Call a Contract](contract/deploy.md)
  * File Manipulation
    * [Create File](file/createfile.md)
  * Scheduled Transactions
    * [Schedule a Transaction](schedule/create.md)
  * Airdrops
    * [Airdrop Tokens](airdrop/send.md)
  * Mirror Node
    * [Query Historical State](mirror/query.md)
  * Miscellaneous
    * [Fee Schedule](misc/fees.md)
    * [Exchange Rates](misc/rate.md)
    * [Suspend Network](misc/suspend.md)

* **Developer Guides**:
  * [Network Configuration](network.md) — testnet vs mainnet, node rotation patterns
  * [Key Management](security/keymanagement.md) — environment variables, vaults, async signing
  * [Dependency Injection](di.md) — registering clients with IServiceCollection
  * [Error Handling](errorhandling.md) — exception hierarchy, transient vs permanent codes, retry patterns
  * [Logging](logging.md) — gRPC-level diagnostics

* [API Documentation](~/api/Hiero.yml): Generated API reference with code examples for every type and method.

* [API Cookbook](https://github.com/bugbytesinc/Hiero/blob/main/docs/api-cookbook.md): Quick reference for all SDK operations in one flat list.

## Is this project Open Source?

Yes, this is an open source project released under the [Apache-2.0 License](https://github.com/bugbytesinc/Hiero/blob/main/LICENSE), the source code can be found at https://github.com/bugbytesinc/Hiero.
