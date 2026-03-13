---
title: Get Account Info
---

# Get Account Info

In preparation for querying detailed information about an account, the first step is to create a Hiero [`ConsensusClient`](xref:Hiero.ConsensusClient) object.  The [`ConsensusClient`](xref:Hiero.ConsensusClient) object orchestrates the request construction and communication with the Hedera network. It requires a small amount of configuration when created. At a minimum to retrieve the information, the client must be configured with a [`ConsensusNodeEndpoint`](xref:Hiero.ConsensusNodeEndpoint) and [`Payer`](xref:Hiero.IConsensusContext.Payer). The [`ConsensusNodeEndpoint`](xref:Hiero.ConsensusNodeEndpoint) object represents the internet network address and account for the node processing requests, and the [`Payer`](xref:Hiero.IConsensusContext.Payer) represents the account that will sign and pay for the query.  The [`Payer`](xref:Hiero.IConsensusContext.Payer) consists of an [`EntityId`](xref:Hiero.EntityId) identifying the account paying transaction fees for the request; and a [`Signatory`](xref:Hiero.Signatory) holding the signing key associated with the [`Payer`](xref:Hiero.IConsensusContext.Payer) account.  Retrieving information about an account is not free, the network requires a nominal payment of a few [_tinybars_](https://help.hedera.com/hc/en-us/articles/360000674317-What-are-the-official-HBAR-cryptocurrency-denominations-) from the paying account to process the request.  After creating and configuring the client object, the [`GetAccountInfoAsync`](xref:Hiero.AccountInfoExtensions.GetAccountInfoAsync(Hiero.ConsensusClient,Hiero.EntityId,System.Threading.CancellationToken,System.Action{Hiero.IConsensusContext})) method submits the request to the network and returns an [`AccountInfo`](xref:Hiero.AccountInfo) object describing the details of the account, including its balance in [_tinybars_](https://help.hedera.com/hc/en-us/articles/360000674317-What-are-the-official-HBAR-cryptocurrency-denominations-).  The following code example illustrates a small program performing these actions:

```csharp
class Program
{
    static async Task Main(string[] args)
    {                                                 // For Example:
        var endpointUrl = args[0];                    //   https://2.testnet.hedera.com:50211
        var nodeAccountNo = long.Parse(args[1]);      //   5 (node 0.0.5)
        var payerAccountNo = long.Parse(args[2]);     //   20 (account 0.0.20)
        var payerPrivateKey = Hex.ToBytes(args[3]);   //   302e0201... (48 byte Ed25519 private in hex)
        var queryAccountNo = long.Parse(args[4]);     //   2300 (account 0.0.2300)
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
            var account = new EntityId(0, 0, queryAccountNo);
            var info = await client.GetAccountInfoAsync(account);
            Console.WriteLine($"Account:               {info.Address}");
            Console.WriteLine($"EVM Address:           {info.EvmAddress}");
            Console.WriteLine($"Balance:               {info.Balance:#,#} tb");
            Console.WriteLine($"Receive Sig. Required: {info.ReceiveSignatureRequired}");
            Console.WriteLine($"Auto-Renewal Period:   {info.AutoRenewPeriod}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
        }
    }
}
```

One should note that to create a [`Signatory`](xref:Hiero.Signatory) associated with the payer account, one does need to have access to the account's private key(s) to sign the transaction authorizing payment to the network query request.  For the account being queried, only the entity ID need be known.

While outside the scope of this example, it is possible to create a signatory that invokes an external method to sign the transaction instead; this is useful for scenarios where the private key is held outside of the system using this library.  Through this mechanism it is possible for the library to _never_ see a private signing key.
