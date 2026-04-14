using Hiero.Test.Integration.Fixtures;
using System.Net;

namespace Hiero.Test.Integration.Docs.Recipe.Misc;

public class GetAddressBookTests
{
    // Code Example:  Docs / Recipe / Misc / Get Payer Book
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

            var nodes = await client.GetAddressBookAsync();
            foreach (var node in nodes)
            {
                Console.Write($"Node {node.Id}: ");
                Console.Write($"{node.Address.ShardNum}.");
                Console.Write($"{node.Address.RealmNum}.");
                Console.WriteLine($"{node.Address.AccountNum}:");
                foreach (var endpoint in node.Endpoints)
                {
                    var address = new IPAddress(endpoint.IpAddress.ToArray());
                    var port = endpoint.Port;
                    Console.WriteLine($"     {address}:{port}");
                }
            }
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
        await Recipe(new string[] { arg0.ToString(), arg1, arg2, arg3 });
    }
}
