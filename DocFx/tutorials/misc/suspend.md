---
title: Suspend Network Services
---

# Suspend Network Services

Requires a _Payer Account_ with system-wide administrative privileges.

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        // Usage: dotnet run -- https://2.testnet.hedera.com:50211 0.0.5 0.0.20 302e...
        var endpointUrl = args[0];
        EntityId.TryParseShardRealmNum(args[1], out var nodeAccount);
        EntityId.TryParseShardRealmNum(args[2], out var payerAccount);
        var payerPrivateKey = Hex.ToBytes(args[3]);
        try
        {
            await using var client = new ConsensusClient(ctx =>
            {
                ctx.Endpoint = new ConsensusNodeEndpoint(nodeAccount!, new Uri(endpointUrl));
                ctx.Payer = payerAccount;
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
