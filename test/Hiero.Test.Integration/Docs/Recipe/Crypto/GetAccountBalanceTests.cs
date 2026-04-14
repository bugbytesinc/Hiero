using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Docs.Recipe.Crypto;

public class GetAccountBalanceTests
{
    // Code Example:  Docs / Recipe / Get Address Balance
    static async Task Recipe(string[] args)
    {                                                 // For Example:
        var gatewayUrl = new Uri(args[0]);            //   http://2.testnet.hedera.com:50211
        var gatewayAccountNo = long.Parse(args[1]);   //   5 (gateway node 0.0.5)
        var queryAccountNo = long.Parse(args[2]);     //   2300 (account 0.0.2300)
        try
        {
            await using var client = new ConsensusClient(ctx =>
            {
                ctx.Endpoint = new ConsensusNodeEndpoint(new EntityId(0, 0, gatewayAccountNo), gatewayUrl);
            });
            var account = new EntityId(0, 0, queryAccountNo);
            var balance = await client.GetAccountBalanceAsync(account);
            Console.WriteLine($"Account Balance for {account.AccountNum} is {balance:#,#} tinybars.");
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
        await Recipe(new string[] { arg0.ToString(), arg1, arg2 });
    }
}
