using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Allowance;

public class CreateAllowancesTests
{
    [Test]
    public async Task Can_Create_A_Crypto_Allowance()
    {
        await using var fxOwner = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 10_00_000_000);
        await using var fxAgent = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var amount = (long)fxOwner.CreateParams.InitialBalance;
        var receipt = await client.AllocateAllowanceAsync(new AllowanceParams
        {
            CryptoAllowances = new[] { new CryptoAllowance(fxOwner, fxAgent, amount) },
            Signatory = fxOwner
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Create_A_Token_Allowance()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var fxAgent = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var amount = (long)fxToken.CreateParams.Circulation / 3 + 1;
        var receipt = await client.AllocateAllowanceAsync(new AllowanceParams
        {
            TokenAllowances = new[] { new TokenAllowance(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount, fxAgent, amount) },
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Create_An_Nft_Allowance()
    {
        await using var fxAgent = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(null, fxAgent);
        await using var client = await TestNetwork.CreateClientAsync();

        var serialNumbers = new long[] { 1 };
        var receipt = await client.AllocateAllowanceAsync(new AllowanceParams
        {
            NftAllowances = new[] { new NftAllowance(fxNft.CreateReceipt!.Token, fxNft.TreasuryAccount, fxAgent, serialNumbers) },
            Signatory = fxNft.TreasuryAccount.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Create_An_All_Nft_Allowance()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var fxAgent = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.AllocateAllowanceAsync(new AllowanceParams
        {
            NftAllowances = new[] { new NftAllowance(fxNft.CreateReceipt!.Token, fxNft.TreasuryAccount, fxAgent) },
            Signatory = fxNft.TreasuryAccount.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Create_A_Crypto_And_Token_Allowance()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var fxAgent = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        long tokenAmount = (long)fxToken.CreateParams.Circulation / 3 + 1;
        long hbarAmount = 500_00_000_000;
        await client.TransferAsync(TestNetwork.Payer, fxToken.TreasuryAccount, hbarAmount);

        var receipt = await client.AllocateAllowanceAsync(new AllowanceParams
        {
            CryptoAllowances = new[] { new CryptoAllowance(fxToken.TreasuryAccount, fxAgent, hbarAmount) },
            TokenAllowances = new[] { new TokenAllowance(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount, fxAgent, tokenAmount) },
            Signatory = fxToken.TreasuryAccount
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Create_A_Crypto_And_Token_And_Nft_Allowance()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var fxAgent = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxToken.TreasuryAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var serialNumbers = new long[] { 1 };
        long tokenAmount = (long)fxToken.CreateParams.Circulation / 3 + 1;
        long hbarAmount = 500_00_000_000;
        await client.TransferAsync(TestNetwork.Payer, fxToken.TreasuryAccount, hbarAmount);
        await client.TransferNftAsync(new Nft(fxNft, 1), fxNft.TreasuryAccount, fxToken.TreasuryAccount, ctx =>
        {
            ctx.Signatory = new Signatory([ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey]);
        });

        var receipt = await client.AllocateAllowanceAsync(new AllowanceParams
        {
            CryptoAllowances = new[] { new CryptoAllowance(fxToken.TreasuryAccount, fxAgent, hbarAmount) },
            TokenAllowances = new[] { new TokenAllowance(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount, fxAgent, tokenAmount) },
            NftAllowances = new[] { new NftAllowance(fxNft.CreateReceipt!.Token, fxToken.TreasuryAccount, fxAgent, serialNumbers) },
            Signatory = fxToken.TreasuryAccount
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Create_A_Crypto_And_Token_And_All_Nft_Allowance()
    {
        await using var fxAllowances = await TestAllowance.CreateAsync();
        await Assert.That(fxAllowances.AllowanceReceipt).IsNotNull();
        await Assert.That(fxAllowances.AllowanceReceipt!.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Create_Crypto_Allowance()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxOwner = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 10_00_000_000);
        await using var fxAgent = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var amount = (long)fxOwner.CreateParams.InitialBalance;
        var scheduledReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new AllowanceParams
            {
                CryptoAllowances = new[] { new CryptoAllowance(fxOwner, fxAgent, amount) },
            },
            Payer = fxPayer,
        });
        await Assert.That(scheduledReceipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(scheduledReceipt.Schedule).IsNotEqualTo(EntityId.None);

        var signReceipt = await client.SignScheduleAsync(new SignScheduleParams
        {
            Schedule = scheduledReceipt.Schedule,
            Signatory = new Signatory(fxPayer.PrivateKey, fxOwner.PrivateKey)
        });
        await Assert.That(signReceipt.Status).IsEqualTo(ResponseCode.Success);
    }
}
