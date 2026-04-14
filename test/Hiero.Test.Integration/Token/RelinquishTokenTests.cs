using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Token;

public class RelinquishTokenTests
{
    [Test]
    public async Task Can_Schedule_And_Sign_Relinquish_Tokens()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);

        // Transfer tokens to the account so it has a balance to relinquish.
        var xferAmount = (long)(fxToken.CreateParams.Circulation / 3);
        await using var client = await TestNetwork.CreateClientAsync();
        await client.TransferTokensAsync(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        // Schedule a relinquish WITHOUT the owner signing.
        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new RelinquishTokensParams
                {
                    Owner = fxAccount.CreateReceipt!.Address,
                    Tokens = [fxToken.CreateReceipt!.Token],
                },
                Memo = Generator.Memo(20),
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<TransactionException>();
        await Assert.That(((TransactionException)tex!).Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    }
}
