---
title: Get Account Balance
---

# Get Account Balance

In preparation for querying an account for its crypto balance, the first step is to create a Hiero [`ConsensusClient`](xref:Hiero.ConsensusClient) object.  The [`ConsensusClient`](xref:Hiero.ConsensusClient) object orchestrates the request construction and communication with the Hedera network. It requires a small amount of configuration when created. At a minimum to retrieve an account balance, the client must be configured with a [`ConsensusNodeEndpoint`](xref:Hiero.ConsensusNodeEndpoint). The [`ConsensusNodeEndpoint`](xref:Hiero.ConsensusNodeEndpoint) object represents the internet network address and account for the node processing requests. Querying the balance of an account is free. After creating and configuring the client object, the [`GetAccountBalanceAsync`](xref:Hiero.ContractBalancesExtensions.GetAccountBalanceAsync(Hiero.ConsensusClient,Hiero.EntityId,System.Threading.CancellationToken,System.Action{Hiero.IConsensusContext})) method submits the request to the network and returns the balance of the account in [_tinybars_](https://help.hedera.com/hc/en-us/articles/360000674317-What-are-the-official-HBAR-cryptocurrency-denominations-).  The following code example illustrates a small program performing these actions:

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
