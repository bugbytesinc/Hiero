using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Token;

public class ClaimAirdropTokenTests
{
    [Test]
    public async Task Can_Schedule_And_Sign_Claim_Airdrop()
    {
        await using var fxSender = await TestAccount.CreateAsync();
        await using var fxReceiver = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 0);
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxSender);

        // Transfer tokens to the sender so they have a balance to airdrop.
        var xferAmount = (long)(fxToken.CreateParams.Circulation / 3);
        await using var client = await TestNetwork.CreateClientAsync();
        await client.TransferTokensAsync(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, fxSender.CreateReceipt!.Address, xferAmount, ctx =>
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

        // Associate the receiver with the token so the claim can succeed.
        await client.AssociateTokenAsync(fxReceiver.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxReceiver.PrivateKey);
        });

        // Schedule a claim airdrop WITHOUT the receiver signing.
        var pendingAirdrop = new Airdrop(fxSender.CreateReceipt!.Address, fxReceiver.CreateReceipt!.Address, fxToken.CreateReceipt!.Token);
        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new ClaimAirdropParams
                {
                    Airdrops = [pendingAirdrop],
                },
                Memo = Generator.Memo(20),
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<PrecheckException>();
        await Assert.That(((PrecheckException)tex!).Status).IsEqualTo(ResponseCode.Busy);
    }
}
