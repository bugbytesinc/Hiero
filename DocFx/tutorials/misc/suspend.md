---
title: Suspend Network Services
---

# Suspend Network Services

Requires a _Payer Account_ with system-wide administrative privileges.

```csharp
class Program
{
    static async Task Main(string[] args)
    {                                                 // For Example:
        var endpointUrl = args[0];                    //   https://2.testnet.hedera.com:50211
        var nodeAccountNo = long.Parse(args[1]);      //   5 (node 0.0.5)
        var payerAccountNo = long.Parse(args[2]);     //   20 (account 0.0.20)
        var payerPrivateKey = Hex.ToBytes(args[3]);   //   302e0201... (Ed25519 private in hex)
        try
        {
            await using var client = new ConsensusClient(ctx =>
            {
                ctx.Endpoint = new ConsensusNodeEndpoint(
                    new EntityId(0, 0, nodeAccountNo),
                    new Uri(endpointUrl));
                ctx.Payer = new EntityId(0, 0, payerAccountNo);
                ctx.Signatory = new Signatory(payerPrivateKey);
            });

            var suspendParams = new SuspendNetworkParams
            {
                Consensus = new ConsensusTimeStamp(DateTime.UtcNow.AddSeconds(60))
            };

            var receipt = await client.SuspendNetworkAsync(suspendParams);
            Console.WriteLine($"Status: {receipt.Status}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
        }
    }
}
```
