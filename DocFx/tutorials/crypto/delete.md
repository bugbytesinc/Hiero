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
    {                                                 // For Example:
        var endpointUrl = args[0];                    //   https://2.testnet.hedera.com:50211
        var nodeAccountNo = long.Parse(args[1]);      //   5 (node 0.0.5)
        var payerAccountNo = long.Parse(args[2]);     //   20 (account 0.0.20)
        var payerPrivateKey = Hex.ToBytes(args[3]);   //   302e0201... (Ed25519 private in hex)
        var deleteAccountNo = long.Parse(args[4]);    //   2300 (account 0.0.2300)
        var deleteAccountKey = Hex.ToBytes(args[5]);  //   302e0201... (Ed25519 private in hex)
        try
        {
            var payerAccount = new EntityId(0, 0, payerAccountNo);

            await using var client = new ConsensusClient(ctx =>
            {
                ctx.Endpoint = new ConsensusNodeEndpoint(
                    new EntityId(0, 0, nodeAccountNo),
                    new Uri(endpointUrl));
                ctx.Payer = payerAccount;
                ctx.Signatory = new Signatory(payerPrivateKey);
            });

            var deleteParams = new DeleteAccountParams
            {
                Account = new EntityId(0, 0, deleteAccountNo),
                FundsReceiver = payerAccount,
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
