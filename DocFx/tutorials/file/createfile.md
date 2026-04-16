---
title: Create File
---

# Create File

In preparation for creating a file on the Hedera network, the first step is to create a Hiero [`ConsensusClient`](xref:Hiero.ConsensusClient) object.  The [`ConsensusClient`](xref:Hiero.ConsensusClient) object orchestrates the request construction and communication with the Hedera network. It requires a small amount of configuration when created. At a minimum to create a file, the client must be configured with a [`ConsensusNodeEndpoint`](xref:Hiero.ConsensusNodeEndpoint) and [`Payer`](xref:Hiero.IConsensusContext.Payer). The [`ConsensusNodeEndpoint`](xref:Hiero.ConsensusNodeEndpoint) object represents the internet network address and account for the node processing requests, and the [`Payer`](xref:Hiero.IConsensusContext.Payer) represents the account that will sign and pay for the transaction.

The next step is to create a [`CreateFileParams`](xref:Hiero.CreateFileParams) object; it holds the details of the file to create.  The key properties are [`Contents`](xref:Hiero.CreateFileParams.Contents) (the file data as bytes), [`Endorsements`](xref:Hiero.CreateFileParams.Endorsements) (the keys required to modify the file later), [`Expiration`](xref:Hiero.CreateFileParams.Expiration) and [`Memo`](xref:Hiero.CreateFileParams.Memo).

After creating and configuring the client object, the [`CreateFileAsync`](xref:Hiero.CreateFileExtensions.CreateFileAsync*) method submits the request to the network and returns a [`FileReceipt`](xref:Hiero.FileReceipt) containing the [`File`](xref:Hiero.FileReceipt.File) entity ID of the newly created file.  The following code example illustrates a small program that creates a file containing UTF-8 text:

```csharp
class Program
{
    static async Task Main(string[] args)
    {
        // Usage: dotnet run -- https://2.testnet.hedera.com:50211 0.0.5 0.0.20 302e... "Hello Hedera!"
        var endpointUrl = args[0];
        EntityId.TryParseShardRealmNum(args[1], out var nodeAccount);
        EntityId.TryParseShardRealmNum(args[2], out var payerAccount);
        var payerPrivateKey = Hex.ToBytes(args[3]);
        var fileContent = args[4];
        try
        {
            await using var client = new ConsensusClient(ctx =>
            {
                ctx.Endpoint = new ConsensusNodeEndpoint(nodeAccount!, new Uri(endpointUrl));
                ctx.Payer = payerAccount;
                ctx.Signatory = new Signatory(payerPrivateKey);
            });

            var payerPublicKey = new Signatory(payerPrivateKey)
                .GetEndorsements().First();

            var createParams = new CreateFileParams
            {
                Contents = Encoding.UTF8.GetBytes(fileContent),
                Endorsements = new[] { payerPublicKey },
                Memo = "Example File"
            };

            var receipt = await client.CreateFileAsync(createParams);
            Console.WriteLine($"New File ID: {receipt.File}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
        }
    }
}
```

One should note that to create a [`Signatory`](xref:Hiero.Signatory) associated with the payer account, one does need to have access to the account's private key(s) to sign the transaction authorizing payment to the network request.

While outside the scope of this example, it is possible to create a signatory that invokes an external method to sign the transaction instead; this is useful for scenarios where the private key is held outside of the system using this library.  Through this mechanism it is possible for the library to _never_ see a private signing key.
