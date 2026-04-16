---
title: Transfer Crypto
---

# Transfer Crypto

In preparation for transferring crypto from one account to another, the first step is to create a Hiero [`ConsensusClient`](xref:Hiero.ConsensusClient) object.  The [`ConsensusClient`](xref:Hiero.ConsensusClient) object orchestrates the transfer request construction and communication with the Hedera network. It requires a small amount of configuration when created. At a minimum to transfer crypto, the client must be configured with a [`ConsensusNodeEndpoint`](xref:Hiero.ConsensusNodeEndpoint) and [`Payer`](xref:Hiero.IConsensusContext.Payer). The [`ConsensusNodeEndpoint`](xref:Hiero.ConsensusNodeEndpoint) object represents the internet network address and account for the node processing the request, and the [`Payer`](xref:Hiero.IConsensusContext.Payer) represents the account that will sign and pay the crypto transfer processing fees.  The [`Payer`](xref:Hiero.IConsensusContext.Payer) consists of an [`EntityId`](xref:Hiero.EntityId) identifying the account paying transaction fees; and a [`Signatory`](xref:Hiero.Signatory) holding the signing key associated with the [`Payer`](xref:Hiero.IConsensusContext.Payer) account.

The next step is to identify the account to debit (send funds from) and the account to credit (send funds to).  The debit account must also sign the crypto transfer transaction (which may or may not be the same account as the Payer) and provide a [`Signatory`](xref:Hiero.Signatory) holding the signing key associated with the debit account.  In some cases, the credit account may also require a signature to accept funds, but for this example we will assume this is not the case.  The amount transferred is denoted in [_tinybars_](https://help.hedera.com/hc/en-us/articles/360000674317-What-are-the-official-HBAR-cryptocurrency-denominations-).  After creating and configuring the client object, the [`TransferAsync`](xref:Hiero.TransferExtensions.TransferAsync*) method submits the request to the network and returns a [`TransactionReceipt`](xref:Hiero.TransactionReceipt) indicating success or failure of the request.  The following code example illustrates a small program performing these actions:


```csharp
class Program
{
    static async Task Main(string[] args)
    {
        // Usage: dotnet run -- https://2.testnet.hedera.com:50211 0.0.5 0.0.20 302e... 0.0.2300 302e... 0.0.4500 100000000
        var endpointUrl = args[0];
        EntityId.TryParseShardRealmNum(args[1], out var nodeAccount);
        EntityId.TryParseShardRealmNum(args[2], out var payerAccount);
        var payerPrivateKey = Hex.ToBytes(args[3]);
        EntityId.TryParseShardRealmNum(args[4], out var fromAccount);
        var fromPrivateKey = Hex.ToBytes(args[5]);
        EntityId.TryParseShardRealmNum(args[6], out var toAccount);
        var amount = long.Parse(args[7]);             //   100000000 (1 hBar)
        try
        {
            await using var client = new ConsensusClient(ctx =>
            {
                ctx.Endpoint = new ConsensusNodeEndpoint(nodeAccount!, new Uri(endpointUrl));
                ctx.Payer = payerAccount;
                ctx.Signatory = new Signatory(payerPrivateKey, new Signatory(fromPrivateKey));
            });

            var receipt = await client.TransferAsync(fromAccount!, toAccount!, amount);
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

While outside the scope of this example, it is possible to create a signatory that invokes an external method to sign the crypto transfer transaction instead; this is useful for scenarios where the private key is held outside of the system using this library. Through this mechanism it is possible for the library to never see a private signing key.
