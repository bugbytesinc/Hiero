using Hiero.Mirror;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Docs.Recipe.Crypto;

public class GetAccountBalanceTests
{
    // Code Example:  Docs / Recipe / Get Address Balance
    static async Task Recipe(string[] args)
    {                                                 // For Example:
        var mirrorUrl = new Uri(args[0]);             //   https://testnet.mirrornode.hedera.com
        var queryAccountNo = long.Parse(args[1]);     //   2300 (account 0.0.2300)
        try
        {
            var mirror = new MirrorRestClient(new HttpClient { BaseAddress = mirrorUrl });
            var account = new EntityId(0, 0, queryAccountNo);
            var balance = await mirror.GetAccountBalanceAsync(account);
            Console.WriteLine(balance is null
                ? $"Account {account} was not found."
                : $"Account Balance for {account.AccountNum} is {balance:#,#} tinybars.");
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
        var arg0 = TestNetwork.MirrorRestUri.ToString();
        var arg1 = TestNetwork.Payer.AccountNum.ToString();
        await Recipe(new string[] { arg0, arg1 });
    }
}
