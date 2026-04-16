---
title: Get Fee Schedule
---

# Get Fee Schedule

Queries the network for current and 'next' fee schedule.

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
