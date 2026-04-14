using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Docs.Recipe.Misc;

public class SuspendNetworkTests
{
    // Code Example:  Docs / Recipe / Misc / Suspend Network
    static async Task Recipe(string[] args)
    {                                                 // For Example:
        var gatewayUrl = new Uri(args[0]);            //   http://2.testnet.hedera.com:50211
        var gatewayAccountNo = long.Parse(args[1]);   //   5 (gateway node 0.0.5)
        var payerAccountNo = long.Parse(args[2]);     //   20 (account 0.0.20)
        var payerPrivateKey = Hex.ToBytes(args[3]);   //   302e0201... (Ed25519 private in hex)
        try
        {
            await using var client = new ConsensusClient(ctx =>
            {
                ctx.Endpoint = new ConsensusNodeEndpoint(new EntityId(0, 0, gatewayAccountNo), gatewayUrl);
                ctx.Payer = new EntityId(0, 0, payerAccountNo);
                ctx.Signatory = new Signatory(payerPrivateKey);
            });
            var receipt = await client.SuspendNetworkAsync(new SuspendNetworkParams
            {
                Consensus = new ConsensusTimeStamp(DateTime.UtcNow.AddSeconds(60))
            });
            Console.WriteLine($"Status: {receipt.Status}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine(ex.StackTrace);
        }
    }

    [Test]
    [Skip("Requires elevated system account access.")]
    public async Task Run_Test()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fxAccount.PrivateKey
        });
        var endpoint = await TestNetwork.GetConsensusNodeEndpointAsync();
        var arg0 = endpoint.Uri;
        var arg1 = endpoint.Node.AccountNum.ToString();
        var arg2 = fxAccount.CreateReceipt!.Address.AccountNum.ToString();
        var arg3 = Hex.FromBytes(fxAccount.PrivateKey);
        await Recipe(new string[] { arg0.ToString(), arg1, arg2, arg3 });
    }
}
