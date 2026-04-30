using Hiero.Mirror;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Mirror;

public class MirrorAirdropDataTests
{
    [Test]
    public async Task Can_Get_Pending_Airdrop_From_Both_Perspectives()
    {
        // Construct a pending airdrop: the receiver has zero auto-association
        // slots and is not associated with the token, so the airdrop lands in
        // the "pending" state instead of completing as a transfer. The same
        // record then appears in:
        //   - sender's "outstanding" list (sender perspective)
        //   - receiver's "pending" list (receiver perspective)
        // Both endpoints share the same TokenAirdropData schema.
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var fxReceiver = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 0);
        await using var client = await TestNetwork.CreateClientAsync();

        var sender = fxToken.TreasuryAccount.CreateReceipt!.Address;
        var receiver = fxReceiver.CreateReceipt!.Address;
        var amount = (long)(fxToken.CreateParams.Circulation / 4);

        var receipt = await client.AirdropTokenAsync(fxToken.CreateReceipt!.Token, sender, receiver, amount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var mirror = await TestNetwork.GetMirrorRestClientAsync();

        var outstanding = new List<TokenAirdropData>();
        await foreach (var entry in mirror.GetAccountOutstandingAirdropsAsync(sender))
        {
            outstanding.Add(entry);
            if (outstanding.Count >= 5)
            {
                break;
            }
        }
        await Assert.That(outstanding.Count).IsEqualTo(1);
        var sent = outstanding[0];
        await Assert.That(sent.Sender).IsEqualTo(sender);
        await Assert.That(sent.Receiver).IsEqualTo(receiver);
        await Assert.That(sent.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(sent.Amount).IsEqualTo(amount);
        await Assert.That(sent.SerialNumber).IsNull();
        await Assert.That(sent.Timestamp).IsNotNull();

        var pending = new List<TokenAirdropData>();
        await foreach (var entry in mirror.GetAccountPendingAirdropsAsync(receiver))
        {
            pending.Add(entry);
            if (pending.Count >= 5)
            {
                break;
            }
        }
        await Assert.That(pending.Count).IsEqualTo(1);
        var got = pending[0];
        await Assert.That(got.Sender).IsEqualTo(sender);
        await Assert.That(got.Receiver).IsEqualTo(receiver);
        await Assert.That(got.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(got.Amount).IsEqualTo(amount);
        await Assert.That(got.SerialNumber).IsNull();
    }
}
