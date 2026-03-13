---
title: Get Exchange Rates
---

# Get Exchange Rates

Queries the network for current and 'next' USD/hBar exchange rate (for fee payment calculations).

Note: this information can also be retrieved from any recently executed receipt via the [`CurrentExchangeRate`](xref:Hiero.TransactionReceipt.CurrentExchangeRate) and [`NextExchangeRate`](xref:Hiero.TransactionReceipt.NextExchangeRate) properties.

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

            var rates = await client.GetExchangeRatesAsync();

            if (rates.Current is not null)
            {
                Console.Write($"Current: cent/hBar = {rates.Current.USDCentEquivalent}");
                Console.Write($"/{rates.Current.HBarEquivalent}");
                Console.Write($"  Expires {rates.Current.Expiration}");
                Console.WriteLine();
            }
            if (rates.Next is not null)
            {
                Console.Write($"Next: cent/hBar = {rates.Next.USDCentEquivalent}");
                Console.Write($"/{rates.Next.HBarEquivalent}");
                Console.Write($"  Expires {rates.Next.Expiration}");
                Console.WriteLine();
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
Example output:
```txt
    Current: cent/hBar = 12/1  Expires 1/1/2100 12:00:00 AM
    Next: cent/hBar = 15/1  Expires 1/1/2100 12:00:00 AM
```
