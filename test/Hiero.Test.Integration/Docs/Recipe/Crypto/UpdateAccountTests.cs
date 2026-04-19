
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Docs.Recipe.Crypto;

public class UpdateAccountTests
{
    // Code Example:  Docs / Recipe / Crypto / Update Address
    static async Task Recipe(string[] args)
    {                                                 // For Example:
        var gatewayUrl = new Uri(args[0]);            //   http://2.testnet.hedera.com:50211
        var gatewayAccountNo = long.Parse(args[1]);   //   5 (gateway node 0.0.5)
        var payerAccountNo = long.Parse(args[2]);     //   20 (account 0.0.20)
        var payerPrivateKey = Hex.ToBytes(args[3]);   //   302e0201... (Ed25519 private in hex)
        var targetAccountNo = long.Parse(args[4]);    //   2023 (account 0.0.2023)
        var targetPrivateKey = Hex.ToBytes(args[5]);  //   302e0201... (Ed25519 private in hex)
        var targetAccountNewMemo = args[6];           //   New Memo to Associate with Target
        try
        {
            await using var client = new ConsensusClient(ctx =>
            {
                ctx.Endpoint = new ConsensusNodeEndpoint(new EntityId(0, 0, gatewayAccountNo), gatewayUrl);
                ctx.Payer = new EntityId(0, 0, payerAccountNo);
                ctx.Signatory = new Signatory(payerPrivateKey);
            });

            var updateParams = new UpdateAccountParams
            {
                Account = new EntityId(0, 0, targetAccountNo),
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

    [Test]
    public async Task Run_Test()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        var endpoint = await TestNetwork.GetConsensusNodeEndpointAsync();
        var arg0 = endpoint.Uri;
        var arg1 = endpoint.Node.AccountNum.ToString();
        var arg2 = TestNetwork.Payer.AccountNum.ToString();
        var arg3 = Hex.FromBytes(TestNetwork.PrivateKey);
        var arg4 = fxAccount.CreateReceipt!.Address.AccountNum.ToString();
        var arg5 = Hex.FromBytes(fxAccount.PrivateKey);
        var arg6 = Generator.String(10, 20);
        await Recipe(new string[] { arg0.ToString(), arg1, arg2, arg3, arg4, arg5, arg6 });
    }
}
