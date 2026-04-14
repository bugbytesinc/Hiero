
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Docs.Recipe.Crypto;

public class CreateAccountTests
{
    // Code Example:  Docs / Recipe / Crypto / Creeate Address
    static async Task Recipe(string[] args)
    {                                                 // For Example:
        var gatewayUrl = new Uri(args[0]);            //   http://2.testnet.hedera.com:50211
        var gatewayAccountNo = long.Parse(args[1]);   //   5 (gateway node 0.0.5)
        var payerAccountNo = long.Parse(args[2]);     //   20 (account 0.0.20)
        var payerPrivateKey = Hex.ToBytes(args[3]);   //   302e0201... (48 byte Ed25519 private in hex)
        var newPublicKey = Hex.ToBytes(args[4]);      //   302a3005... (44 byte Ed25519 public in hex)
        var initialBalance = ulong.Parse(args[5]);    //   100_000_000 (1ℏ initial balance)
        try
        {
            await using var client = new ConsensusClient(ctx =>
            {
                ctx.Endpoint = new ConsensusNodeEndpoint(new EntityId(0, 0, gatewayAccountNo), gatewayUrl);
                ctx.Payer = new EntityId(0, 0, payerAccountNo);
                ctx.Signatory = new Signatory(payerPrivateKey);
            });
            var createParams = new CreateAccountParams
            {
                Endorsement = new Endorsement(newPublicKey),
                InitialBalance = initialBalance
            };
            var account = await client.CreateAccountAsync(createParams);
            var address = account.Address;
            Console.WriteLine($"New Account ID: {address.ShardNum}.{address.RealmNum}.{address.AccountNum}");
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
        var (publicKey, _) = Generator.KeyPair();
        var endpoint = await TestNetwork.GetConsensusNodeEndpointAsync();
        var arg0 = endpoint.Uri;
        var arg1 = endpoint.Node.AccountNum.ToString();
        var arg2 = TestNetwork.Payer.AccountNum.ToString();
        var arg3 = Hex.FromBytes(TestNetwork.PrivateKey);
        var arg4 = Hex.FromBytes(publicKey);
        var arg5 = "1";
        await Recipe(new string[] { arg0.ToString(), arg1, arg2, arg3, arg4, arg5 });
    }
}
