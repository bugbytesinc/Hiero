using Hiero.Test.Integration.Fixtures;
using System.Text;

namespace Hiero.Test.Integration.Docs.Recipe;

public class GetFileContentTests
{
    // Code Example:  Docs / Recipe / Get File Contents
    static async Task Recipe(string[] args)
    {                                                 // For Example:
        var gatewayUrl = new Uri(args[0]);            //   http://2.testnet.hedera.com:50211
        var gatewayAccountNo = long.Parse(args[1]);   //   5 (gateway node 0.0.5)
        var payerAccountNo = long.Parse(args[2]);     //   20 (account 0.0.20)
        var payerPrivateKey = Hex.ToBytes(args[3]);   //   302e0201... (48 byte Ed25519 private in hex)
        var fileNo = long.Parse(args[4]);             //   1234 (account 0.0.1234)
        try
        {
            await using var client = new ConsensusClient(ctx =>
            {
                ctx.Endpoint = new ConsensusNodeEndpoint(new EntityId(0, 0, gatewayAccountNo), gatewayUrl);
                ctx.Payer = new EntityId(0, 0, payerAccountNo);
                ctx.Signatory = new Signatory(payerPrivateKey);
            });
            var file = new EntityId(0, 0, fileNo);
            var bytes = await client.GetFileContentAsync(file);
            Console.Write(Encoding.UTF8.GetString(bytes.ToArray()));
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
        await using var client = await TestNetwork.CreateClientAsync();
        var file = await client.CreateFileAsync(new CreateFileParams
        {
            Expiration = DateTime.UtcNow.AddSeconds(7890000),
            Contents = Encoding.UTF8.GetBytes("Hello Hedera"),
            Endorsements = new Endorsement[] { TestNetwork.Endorsement }
        });
        var endpoint = await TestNetwork.GetConsensusNodeEndpointAsync();
        var arg0 = endpoint.Uri;
        var arg1 = endpoint.Node.AccountNum.ToString();
        var arg2 = TestNetwork.Payer.AccountNum.ToString();
        var arg3 = Hex.FromBytes(TestNetwork.PrivateKey);
        var arg4 = file.File.AccountNum.ToString();
        await Recipe(new string[] { arg0.ToString(), arg1, arg2, arg3, arg4 });
    }
}
