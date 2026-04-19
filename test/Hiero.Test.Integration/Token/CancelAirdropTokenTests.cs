using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Token;

public class CancelAirdropTokenTests
{
    [Test]
    public async Task Can_Schedule_And_Sign_Cancel_Airdrop()
    {
        await using var fxSender = await TestAccount.CreateAsync();
        await using var fxReceiver = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 0);
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxSender);

        // Transfer tokens to the sender so they have a balance to airdrop.
        var xferAmount = (long)(fxToken.CreateParams.Circulation / 3);
        await using var client = await TestNetwork.CreateClientAsync();
        await client.TransferTokenAsync(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, fxSender.CreateReceipt!.Address, xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        // Perform an airdrop to a receiver with no auto-association slots,
        // creating a pending airdrop.
        await client.AirdropAsync(new AirdropParams
        {
            TokenTransfers =
            [
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxSender.CreateReceipt!.Address, -xferAmount),
                new TokenTransfer(fxToken.CreateReceipt!.Token, fxReceiver.CreateReceipt!.Address, xferAmount),
            ],
            Signatory = fxSender.PrivateKey,
        });

        // Schedule a cancel airdrop WITHOUT the sender signing.
        var airdrop = new Airdrop(fxToken.CreateReceipt!.Token, fxSender.CreateReceipt!.Address, fxReceiver.CreateReceipt!.Address);
        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new CancelAirdropParams
                {
                    Airdrops = [airdrop],
                },
                Memo = Generator.Memo(20),
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<PrecheckException>();
        await Assert.That(((PrecheckException)tex!).Status).IsEqualTo(ResponseCode.Busy);
    }
}
