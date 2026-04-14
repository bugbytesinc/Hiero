using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Allowance;

public class UpdateAllowancesTests
{
    [Test]
    public async Task Can_Update_A_Crypto_Allowance()
    {
        await using var fxOwner = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 10_00_000_000);
        await using var fxAgent = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var amount = (long)fxOwner.CreateParams.InitialBalance / 4;
        await client.AllocateAllowanceAsync(new AllowanceParams
        {
            CryptoAllowances = new[] { new CryptoAllowance(fxOwner, fxAgent, amount) },
            Signatory = fxOwner
        }, ctx =>
        {
            ctx.Payer = fxOwner;
            ctx.Signatory = fxOwner;
        });

        var receipt = await client.AllocateAllowanceAsync(new AllowanceParams
        {
            CryptoAllowances = new[] { new CryptoAllowance(fxOwner, fxAgent, amount * 2) },
            Signatory = fxOwner
        }, ctx =>
        {
            ctx.Payer = fxOwner;
            ctx.Signatory = fxOwner;
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }
}
