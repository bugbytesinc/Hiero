---
title: Create New Account
---

# Create New Account

All accounts within the Hedera network can create new accounts.  To create a new account, the existing account holder must provide to the Hedera network a public key matching a private key that will secure the funds held by the new account.  The holder of the new account must protect this private key, losing control of this private key will result in a loss of control of the created account.  The creating account holder must also decide on how much crypto to provide to the account for its initial balance.  The network will generate the entity ID in the form of 0.0.x (shard.realm.number).  This is the identifier for the newly created account (unlike other systems where the public key is the identifier).

## Example

The first step in this process is to create a Hiero [`ConsensusClient`](xref:Hiero.ConsensusClient) object.  The [`ConsensusClient`](xref:Hiero.ConsensusClient) object orchestrates the request construction and communication with the Hedera network. It requires a small amount of configuration when created.  At a minimum to create the new account, the client must be configured with a [`ConsensusNodeEndpoint`](xref:Hiero.ConsensusNodeEndpoint) and [`Payer`](xref:Hiero.IConsensusContext.Payer). The [`ConsensusNodeEndpoint`](xref:Hiero.ConsensusNodeEndpoint) object represents the internet network address and account for the node processing the transaction request, and the [`Payer`](xref:Hiero.IConsensusContext.Payer) identifies the account that will sign and pay for the transaction.  The [`Payer`](xref:Hiero.IConsensusContext.Payer) consists of an [`EntityId`](xref:Hiero.EntityId) identifying the account paying transaction fees (which includes the value of the account's initial balance); and a [`Signatory`](xref:Hiero.Signatory) holding the signing key associated with the Payer account.

The next step is to create a [`CreateAccountParams`](xref:Hiero.CreateAccountParams) object; it holds the details of the create request.  The two most important properties to set on this object are the [`Endorsement`](xref:Hiero.CreateAccountParams.Endorsement) and [`InitialBalance`](xref:Hiero.CreateAccountParams.InitialBalance) properties.  In the simplest case, the endorsement is a single Ed25519 public key (discussed above).  The value of the initial balance will be drawn from the payer account and placed into the newly created account.  The default values for the remaining properties need not be altered.

Finally, to create the Hedera account, invoke the client's [`CreateAccountAsync`](xref:Hiero.CreateAccountExtensions.CreateAccountAsync*) method to submit the request to the network.  The method returns a [`CreateAccountReceipt`](xref:Hiero.CreateAccountReceipt) containing a property, [`Address`](xref:Hiero.CreateAccountReceipt.Address), identifying the newly created account.  The following code example illustrates a small program performing these actions:

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        // Usage: dotnet run -- https://2.testnet.hedera.com:50211 0.0.5 0.0.20 302e... 302a... 100000000
        var endpointUrl = args[0];
        EntityId.TryParseShardRealmNum(args[1], out var nodeAccount);
        EntityId.TryParseShardRealmNum(args[2], out var payerAccount);
        var payerPrivateKey = Hex.ToBytes(args[3]);
        var newPublicKey = Hex.ToBytes(args[4]);      //   302a3005... (44 byte Ed25519 public in hex)
        var initialBalance = ulong.Parse(args[5]);    //   100_000_000 (1 hbar initial balance)
        try
        {
            await using var client = new ConsensusClient(ctx =>
            {
                ctx.Endpoint = new ConsensusNodeEndpoint(nodeAccount!, new Uri(endpointUrl));
                ctx.Payer = payerAccount;
                ctx.Signatory = new Signatory(payerPrivateKey);
            });
            var createParams = new CreateAccountParams
            {
                Endorsement = new Endorsement(newPublicKey),
                InitialBalance = initialBalance
            };
            var receipt = await client.CreateAccountAsync(createParams);
            var address = receipt.Address;
            Console.WriteLine($"New Account ID: {address}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
        }
    }
}
```
