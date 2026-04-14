using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Contract;

public class ContractAccountTests
{
    [Test]
    public async Task Can_Get_Account_Balance()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var balance = await client.GetAccountBalanceAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(balance).IsEqualTo(0ul);
    }

    [Test]
    public async Task Can_Send_Cryptoto_Account()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.TransferAsync(TestNetwork.Payer, fxContract, 500);
        var balance = await client.GetAccountBalanceAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(balance).IsEqualTo(500ul);
    }

    [Test]
    public async Task Can_Send_Crypto_From_Account()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.TransferAsync(TestNetwork.Payer, fxContract, 500);
        var balance = await client.GetAccountBalanceAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(balance).IsEqualTo(500ul);

        var receipt = await client.TransferAsync(fxContract, TestNetwork.Payer, 500, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxContract.PrivateKey));
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        balance = await client.GetAccountBalanceAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(balance).IsEqualTo(0ul);
    }

    [Test]
    public async Task Can_Send_Tokento_Account()
    {
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxContract, fxToken, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxContract.PrivateKey);
        });
        await client.TransferTokensAsync(fxToken, fxToken.TreasuryAccount, fxContract, 500, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });
        await Assert.That(await fxContract.GetTokenBalanceAsync(fxToken)).IsEqualTo(500);
    }

    [Test]
    public async Task Can_Send_Token_From_Account()
    {
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxContract, fxToken, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxContract.PrivateKey);
        });
        await client.TransferTokensAsync(fxToken, fxToken.TreasuryAccount, fxContract, 500, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });
        await Assert.That(await fxContract.GetTokenBalanceAsync(fxToken)).IsEqualTo(500);

        await client.TransferTokensAsync(fxToken, fxContract, fxToken.TreasuryAccount, 400, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxContract.PrivateKey);
        });
        await Assert.That(await fxContract.GetTokenBalanceAsync(fxToken)).IsEqualTo(100);
    }

    [Test]
    public async Task Can_Set_Allowance_For_Token()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxContract, fxToken, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxContract.PrivateKey);
        });
        await client.TransferTokensAsync(fxToken, fxToken.TreasuryAccount, fxContract, 500, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });
        await Assert.That(await fxContract.GetTokenBalanceAsync(fxToken)).IsEqualTo(500);
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);

        await client.AllocateAllowanceAsync(new AllowanceParams
        {
            TokenAllowances = [new TokenAllowance(fxToken, fxContract, TestNetwork.Payer, 400)],
            Signatory = fxContract.PrivateKey
        });

        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = [
                new TokenTransfer(fxToken, fxContract, -400, true),
                new TokenTransfer(fxToken, fxAccount, 400, false)
            ]
        });
        await Assert.That(await fxContract.GetTokenBalanceAsync(fxToken)).IsEqualTo(100);
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo(400);
    }
}
