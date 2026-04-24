---
title: Update Crypto Account
---

# Update Crypto Account

Update the properties of a crypto account.

Example:

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        // Usage: dotnet run -- https://2.testnet.hedera.com:50211 0.0.5 0.0.20 302e... 0.0.2023 302e... "New Memo"
        var endpointUrl = args[0];
        EntityId.TryParseShardRealmNum(args[1], out var nodeAccount);
        EntityId.TryParseShardRealmNum(args[2], out var payerAccount);
        var payerPrivateKey = Hex.ToBytes(args[3]);
        EntityId.TryParseShardRealmNum(args[4], out var targetAccount);
        var targetPrivateKey = Hex.ToBytes(args[5]);
        var targetAccountNewMemo = args[6];
        try
        {
            await using var client = new ConsensusClient(ctx =>
            {
                ctx.Endpoint = new ConsensusNodeEndpoint(nodeAccount!, new Uri(endpointUrl));
                ctx.Payer = payerAccount;
                ctx.Signatory = new Signatory(payerPrivateKey);
            });

            var updateParams = new UpdateAccountParams
            {
                Account = targetAccount!,
                Signatory = new Signatory(targetPrivateKey),
                Memo = targetAccountNewMemo
            };

            var receipt = await client.UpdateAccountAsync(updateParams);
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
