using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Token;

public class AirdropTokenTests
{
    [Test]
    public async Task Can_Not_Schedule_Airdrop_Token()
    {
        await using var fxSender = await TestAccount.CreateAsync();
        await using var fxReceiver = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 0);
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxSender);

        // Transfer some tokens to the sender so they have a balance to airdrop.
        var xferAmount = (long)(fxToken.CreateParams.Circulation / 3);
        await using var client = await TestNetwork.CreateClientAsync();
        await client.TransferTokenAsync(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, fxSender.CreateReceipt!.Address, xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        // An airdrop can't be scheduled: TokenAirdrop is not in the
        // network's scheduling whitelist.
        var ex = await Assert.That(async () =>
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
            }, ctx =>
            {
                // Testnet answers BUSY rather than naming the whitelist.
                // Keep the retry window short so that BUSY surfaces, rather
                // than the default ~3 minute window outliving the transaction's
                // validity and ending in TRANSACTION_EXPIRED.
                ctx.RetryCount = 5;
            });
        }).ThrowsException();
        // SOLO VS TESTNET
        if(ex is PrecheckException pex)
        {
            await Assert.That(pex.Status).IsEqualTo(ResponseCode.Busy);
        }
        else if(ex is TransactionException tex)
        {
            await Assert.That(tex.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
        }
        else
        {
            Assert.Fail($"Unexpected Exception Type {ex}");
        }
    }
}
