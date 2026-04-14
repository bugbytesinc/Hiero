using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Token;

public class AirdropTokenTests
{
    [Test]
    public async Task Can_Schedule_And_Sign_Airdrop_Token()
    {
        await using var fxSender = await TestAccount.CreateAsync();
        await using var fxReceiver = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 0);
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxSender);

        // Transfer some tokens to the sender so they have a balance to airdrop.
        var xferAmount = (long)(fxToken.CreateParams.Circulation / 3);
        await using var client = await TestNetwork.CreateClientAsync();
        await client.TransferTokensAsync(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, fxSender.CreateReceipt!.Address, xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        // Schedule an airdrop WITHOUT the sender signing.
        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new AirdropParams
                {
                    TokenTransfers =
                    [
                        new TokenTransfer(fxToken.CreateReceipt!.Token, fxSender.CreateReceipt!.Address, -xferAmount),
                        new TokenTransfer(fxToken.CreateReceipt!.Token, fxReceiver.CreateReceipt!.Address, xferAmount),
                    ],
                },
                Memo = Generator.Memo(20),
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<PrecheckException>();
        await Assert.That(((PrecheckException)tex!).Status).IsEqualTo(ResponseCode.Busy);
    }
}
