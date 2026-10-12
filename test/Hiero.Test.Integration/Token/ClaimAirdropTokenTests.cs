using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Token;

public class ClaimAirdropTokenTests
{
    [Test]
    public async Task Can_Claim_Airdrop()
    {
        // The receiver has no auto-association slots and is not associated
        // with the token, so the airdrop is left pending until claimed.
        await using var fxReceiver = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 0);
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var client = await TestNetwork.CreateClientAsync();

        var token = fxToken.CreateReceipt!.Token;
        var sender = fxToken.TreasuryAccount.CreateReceipt!.Address;
        var receiver = fxReceiver.CreateReceipt!.Address;
        var amount = (long)(fxToken.CreateParams.Circulation / 3);

        var airdropReceipt = await client.AirdropTokenAsync(token, sender, receiver, amount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });
        await Assert.That(airdropReceipt.Status).IsEqualTo(ResponseCode.Success);

        var airdropRecord = await client.GetTransactionRecordAsync(airdropReceipt.TransactionId) as AirdropRecord;
        await Assert.That(airdropRecord).IsNotNull();
        await Assert.That(airdropRecord!.Airdrops.Count).IsEqualTo(1);
        var pending = airdropRecord.Airdrops[0];
        await Assert.That(pending.Airdrop).IsEqualTo(new Airdrop(token, sender, receiver));
        await Assert.That(pending.Amount).IsEqualTo((ulong)amount);
        await Assert.That(await fxReceiver.GetTokenBalanceAsync(token)).IsNull();

        // Claiming associates the token with the receiver, so no separate
        // association is needed. Only the receiver signs.
        var claimReceipt = await client.ClaimAirdropAsync(pending.Airdrop, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxReceiver.PrivateKey);
        });
        await Assert.That(claimReceipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.TokenBalanceAsync(fxToken, fxReceiver, (ulong)amount);
        await AssertHg.TokenBalanceAsync(fxToken, fxToken.TreasuryAccount, fxToken.CreateParams.Circulation - (ulong)amount);
    }

    [Test]
    public async Task Can_Not_Schedule_Claim_Airdrop()
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

        // A claim can't be scheduled: TokenClaimAirdrop is not in the
        // network's scheduling whitelist.
        var airdrop = new Airdrop(fxToken.CreateReceipt!.Token, fxSender.CreateReceipt!.Address, fxReceiver.CreateReceipt!.Address);
        var ex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new ClaimAirdropParams
                {
                    Airdrops = [airdrop],
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
        if (ex is PrecheckException pex)
        {
            await Assert.That(pex.Status).IsEqualTo(ResponseCode.Busy);
        }
        else if (ex is TransactionException tex)
        {
            await Assert.That(tex.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
        }
        else
        {
            Assert.Fail($"Unexpected Exception Type {ex}");
        }
    }
}
