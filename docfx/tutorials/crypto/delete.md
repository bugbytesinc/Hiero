---
title: Delete Crypto Account
---

# Delete Crypto Account

Delete a crypto account, sending the remaining balance of hBars and tokens to the specified crypto account.

Example:

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        // Usage: dotnet run -- https://2.testnet.hedera.com:50211 0.0.5 0.0.20 302e... 0.0.2300 302e...
        var endpointUrl = args[0];
        EntityId.TryParseShardRealmNum(args[1], out var nodeAccount);
        EntityId.TryParseShardRealmNum(args[2], out var payerAccount);
        var payerPrivateKey = Hex.ToBytes(args[3]);
        EntityId.TryParseShardRealmNum(args[4], out var deleteAccount);
        var deleteAccountKey = Hex.ToBytes(args[5]);
        try
        {
            await using var client = new ConsensusClient(ctx =>
            {
                ctx.Endpoint = new ConsensusNodeEndpoint(nodeAccount!, new Uri(endpointUrl));
                ctx.Payer = payerAccount;
                ctx.Signatory = new Signatory(payerPrivateKey);
            });

            var deleteParams = new DeleteAccountParams
            {
                Account = deleteAccount!,
                FundsReceiver = payerAccount!,
                Signatory = new Signatory(deleteAccountKey)
            };

            var receipt = await client.DeleteAccountAsync(deleteParams);
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
