---
title: Get Fee Schedule
---

# Get Fee Schedule

Queries the network for current and 'next' fee schedule.

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

            var schedules = await client.GetFeeScheduleAsync();

            if (schedules.Current is not null)
            {
                Console.WriteLine($"Current Fee Schedule (Expires: {schedules.Current.Expires})");
                foreach (var schedule in schedules.Current.Data)
                {
                    Console.WriteLine(schedule.Key);
                    foreach (var detail in schedule.Value)
                    {
                        Console.WriteLine(detail);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
        }
    }
}
```
