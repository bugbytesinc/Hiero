using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Docs.Recipe.Crypto;

public class GetAccountInfoTests
{
    // Code Example:  Docs / Recipe / Get Address Info
    static async Task Recipe(string[] args)
    {                                                 // For Example:
        var gatewayUrl = new Uri(args[0]);            //   http://2.testnet.hedera.com:50211
        var gatewayAccountNo = long.Parse(args[1]);   //   5 (gateway node 0.0.5)
        var payerAccountNo = long.Parse(args[2]);     //   20 (account 0.0.20)
        var payerPrivateKey = Hex.ToBytes(args[3]);   //   302e0201... (48 byte Ed25519 private in hex)
        var queryAccountNo = long.Parse(args[4]);     //   2300 (account 0.0.2300)
        try
        {
            await using var client = new ConsensusClient(ctx =>
            {
                ctx.Endpoint = new ConsensusNodeEndpoint(new EntityId(0, 0, gatewayAccountNo), gatewayUrl);
                ctx.Payer = new EntityId(0, 0, payerAccountNo);
                ctx.Signatory = new Signatory(payerPrivateKey);
            });
            var account = new EntityId(0, 0, queryAccountNo);
            var info = await client.GetAccountInfoAsync(account);
            Console.WriteLine($"Account:               0.0.{info.Address.AccountNum}");
            Console.WriteLine($"Smart Contract ID:     {info.EvmAddress}");
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

    [Test]
    public async Task Run_Test()
    {
        var endpoint = await TestNetwork.GetConsensusNodeEndpointAsync();
        var arg0 = endpoint.Uri;
        var arg1 = endpoint.Node.AccountNum.ToString();
        var arg2 = TestNetwork.Payer.AccountNum.ToString();
        var arg3 = Hex.FromBytes(TestNetwork.PrivateKey);
        var arg4 = TestNetwork.Payer.AccountNum.ToString();
        await Recipe(new string[] { arg0.ToString(), arg1, arg2, arg3, arg4 });
    }
}
