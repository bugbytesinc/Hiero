using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Docs.Recipe.Crypto;

public class DeleteCryptoAccountTests
{
    // Code Example:  Docs / Recipe / Delete Crypto Address
    static async Task Recipe(string[] args)
    {                                                 // For Example:
        var gatewayUrl = new Uri(args[0]);            //   http://k2.testnet.hedera.com:50211
        var gatewayAccountNo = long.Parse(args[1]);   //   5 (gateway node 0.0.5)
        var payerAccountNo = long.Parse(args[2]);     //   20 (account 0.0.20)
        var payerPrivateKey = Hex.ToBytes(args[3]);   //   302e0201... (Ed25519 private in hex)
        var deleteAccountNo = long.Parse(args[4]);    //   2300 (account 0.0.2300)
        var deleteAccountKey = Hex.ToBytes(args[5]);  //   302e0201... (Ed25519 private in hex)
        try
        {
            var payerAccount = new EntityId(0, 0, payerAccountNo);
            var payerSignatory = new Signatory(payerPrivateKey);
            var accountToDelete = new EntityId(0, 0, deleteAccountNo);
            var deleteAccountSignatory = new Signatory(deleteAccountKey);

            await using var client = new ConsensusClient(ctx =>
            {
                ctx.Endpoint = new ConsensusNodeEndpoint(new EntityId(0, 0, gatewayAccountNo), gatewayUrl);
                ctx.Payer = payerAccount;
                ctx.Signatory = payerSignatory;
            });

            var receipt = await client.DeleteAccountAsync(new DeleteAccountParams
            {
                Account = accountToDelete,
                FundsReceiver = payerAccount,
                Signatory = deleteAccountSignatory
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
    public async Task Run_Test()
    {
        await using var fxFrom = await TestAccount.CreateAsync();
        await using var fxTo = await TestAccount.CreateAsync();
        var endpoint = await TestNetwork.GetConsensusNodeEndpointAsync();
        var arg0 = endpoint.Uri;
        var arg1 = endpoint.Node.AccountNum.ToString();
        var arg2 = TestNetwork.Payer.AccountNum.ToString();
        var arg3 = Hex.FromBytes(TestNetwork.PrivateKey);
        var arg4 = fxFrom.CreateReceipt!.Address.AccountNum.ToString();
        var arg5 = Hex.FromBytes(fxFrom.PrivateKey);
        await Recipe(new string[] { arg0.ToString(), arg1, arg2, arg3, arg4, arg5 });
    }
}
