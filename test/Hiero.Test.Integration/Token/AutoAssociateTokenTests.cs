using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Token;

public class AutoAssociateTokenTests
{
    [Test]
    public async Task Can_Auto_Associate_Token_With_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);

        await AssertHg.TokenNotAssociatedAsync(fxToken, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var xferAmount = fxToken.CreateParams.Circulation / 2;
        var receipt = await client.TransferTokensAsync(fxToken, fxToken.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var association = await AssertHg.TokenIsAssociatedAsync(fxToken, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo((long)xferAmount);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsTrue();

        var info = await client.GetAccountInfoAsync(fxAccount);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(fxAccount.CreateParams.AutoAssociationLimit);

        var tokens = await fxAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).HasSingleItem();

        var balance = tokens[0];
        await Assert.That(balance.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(balance.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(balance.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(balance.AutoAssociated).IsTrue();
    }

    [Test]
    public async Task Can_Auto_Associate_Asset_With_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferNftAsync(new Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var association = await AssertHg.NftIsAssociatedAsync(fxNft, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(1);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsTrue();

        var info = await client.GetAccountInfoAsync(fxAccount);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(fxAccount.CreateParams.AutoAssociationLimit);

        var tokens = await fxAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).HasSingleItem();

        var balance = tokens[0];
        await Assert.That(balance.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(balance.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(balance.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(balance.AutoAssociated).IsTrue();
    }

    [Test]
    public async Task Can_Airdop_Associate_Asset_With_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = -1);
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferNftAsync(new Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var association = await AssertHg.NftIsAssociatedAsync(fxNft, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(1);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsTrue();

        var info = await client.GetAccountInfoAsync(fxAccount);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(fxAccount.CreateParams.AutoAssociationLimit);

        var tokens = await fxAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).HasSingleItem();

        var balance = tokens[0];
        await Assert.That(balance.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(balance.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(balance.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(balance.AutoAssociated).IsTrue();
    }

    [Test]
    public async Task Can_Limit_Token_Auto_Association()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 1);
        await using var fxToken1 = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var fxToken2 = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);

        await AssertHg.TokenNotAssociatedAsync(fxToken1, fxAccount);
        await AssertHg.TokenNotAssociatedAsync(fxToken2, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var xferAmount1 = fxToken1.CreateParams.Circulation / 2;
        var receipt = await client.TransferTokensAsync(fxToken1, fxToken1.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, (long)xferAmount1, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken1.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var ex = await Assert.That(async () =>
        {
            var xferAmount2 = fxToken2.CreateParams.Circulation / 2;
            await client.TransferTokensAsync(fxToken2, fxToken2.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, (long)xferAmount2, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxToken2.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.NoRemainingAutomaticAssociations);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: NoRemainingAutomaticAssociations");

        await AssertHg.TokenIsAssociatedAsync(fxToken1, fxAccount);
        await AssertHg.TokenNotAssociatedAsync(fxToken2, fxAccount);
        await AssertHg.TokenBalanceAsync(fxToken1, fxAccount, xferAmount1);
    }

    [Test]
    public async Task Can_Limit_Asset_Auto_Association()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 1);
        await using var fxNft1 = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var fxNft2 = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);

        await AssertHg.NftNotAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftNotAssociatedAsync(fxNft2, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferNftAsync(new Nft(fxNft1, 1), fxNft1.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft1.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(new Nft(fxNft2, 1), fxNft2.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft2.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.NoRemainingAutomaticAssociations);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: NoRemainingAutomaticAssociations");

        await AssertHg.NftIsAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftNotAssociatedAsync(fxNft2, fxAccount);
        await AssertHg.NftBalanceAsync(fxNft1, fxAccount, 1);
    }

    [Test]
    public async Task Can_Unlimit_Asset_Auto_Association()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = -1);
        await using var fxNft1 = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var fxNft2 = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);

        await AssertHg.NftNotAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftNotAssociatedAsync(fxNft2, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferNftAsync(new Nft(fxNft1, 1), fxNft1.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft1.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        receipt = await client.TransferNftAsync(new Nft(fxNft2, 1), fxNft2.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft2.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.NftIsAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftIsAssociatedAsync(fxNft2, fxAccount);
        await AssertHg.NftBalanceAsync(fxNft1, fxAccount, 1);
        await AssertHg.NftBalanceAsync(fxNft2, fxAccount, 1);
    }

    [Test]
    public async Task No_Token_Auto_Association_Results_In_Error()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 0);
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);

        await AssertHg.TokenNotAssociatedAsync(fxToken, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            var xferAmount2 = fxToken.CreateParams.Circulation / 2;
            await client.TransferTokensAsync(fxToken, fxToken.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, (long)xferAmount2, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenNotAssociatedToAccount);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: TokenNotAssociatedToAccount");

        await AssertHg.TokenNotAssociatedAsync(fxToken, fxAccount);
    }

    [Test]
    public async Task No_Asset_Auto_Association_Results_In_Error()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 0);
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(new Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenNotAssociatedToAccount);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: TokenNotAssociatedToAccount");

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);
    }

    [Test]
    public async Task Can_Lower_Limit_Token_Auto_Association()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 100);
        await using var fxToken1 = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var fxToken2 = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);

        await AssertHg.TokenNotAssociatedAsync(fxToken1, fxAccount);
        await AssertHg.TokenNotAssociatedAsync(fxToken2, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var xferAmount1 = fxToken1.CreateParams.Circulation / 2;
        var receipt = await client.TransferTokensAsync(fxToken1, fxToken1.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, (long)xferAmount1, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken1.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Address = fxAccount,
            AutoAssociationLimit = 1,
            Signatory = fxAccount
        });

        var ex = await Assert.That(async () =>
        {
            var xferAmount2 = fxToken2.CreateParams.Circulation / 2;
            await client.TransferTokensAsync(fxToken2, fxToken2.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, (long)xferAmount2, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxToken2.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.NoRemainingAutomaticAssociations);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: NoRemainingAutomaticAssociations");

        await AssertHg.TokenIsAssociatedAsync(fxToken1, fxAccount);
        await AssertHg.TokenNotAssociatedAsync(fxToken2, fxAccount);
        await AssertHg.TokenBalanceAsync(fxToken1, fxAccount, xferAmount1);
    }

    [Test]
    public async Task Can_Lower_Limit_Asset_Auto_Association()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 100);
        await using var fxNft1 = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var fxNft2 = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);

        await AssertHg.NftNotAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftNotAssociatedAsync(fxNft2, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferNftAsync(new Nft(fxNft1, 1), fxNft1.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft1.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Address = fxAccount,
            AutoAssociationLimit = 1,
            Signatory = fxAccount
        });

        var ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(new Nft(fxNft2, 1), fxNft2.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft2.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.NoRemainingAutomaticAssociations);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: NoRemainingAutomaticAssociations");

        await AssertHg.NftIsAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftNotAssociatedAsync(fxNft2, fxAccount);
        await AssertHg.NftBalanceAsync(fxNft1, fxAccount, 1);
    }

    [Test]
    public async Task Can_Raise_Limit_Token_Auto_Association()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 1);
        await using var fxToken1 = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var fxToken2 = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);

        await AssertHg.TokenNotAssociatedAsync(fxToken1, fxAccount);
        await AssertHg.TokenNotAssociatedAsync(fxToken2, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var xferAmount1 = fxToken1.CreateParams.Circulation / 2;
        var receipt = await client.TransferTokensAsync(fxToken1, fxToken1.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, (long)xferAmount1, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken1.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Address = fxAccount,
            AutoAssociationLimit = 2,
            Signatory = fxAccount
        });

        var xferAmount2 = fxToken2.CreateParams.Circulation / 2;
        var receipt2 = await client.TransferTokensAsync(fxToken2, fxToken2.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, (long)xferAmount2, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken2.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt2.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.TokenIsAssociatedAsync(fxToken1, fxAccount);
        await AssertHg.TokenIsAssociatedAsync(fxToken2, fxAccount);
        await AssertHg.TokenBalanceAsync(fxToken1, fxAccount, xferAmount1);
        await AssertHg.TokenBalanceAsync(fxToken2, fxAccount, xferAmount2);
    }

    [Test]
    public async Task Can_Raise_Limit_Asset_Auto_Association()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 100);
        await using var fxNft1 = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var fxNft2 = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);

        await AssertHg.NftNotAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftNotAssociatedAsync(fxNft2, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferNftAsync(new Nft(fxNft1, 1), fxNft1.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft1.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Address = fxAccount,
            AutoAssociationLimit = 2,
            Signatory = fxAccount
        });

        var receipt2 = await client.TransferNftAsync(new Nft(fxNft2, 1), fxNft2.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft2.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt2.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.NftIsAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftIsAssociatedAsync(fxNft2, fxAccount);
        await AssertHg.NftBalanceAsync(fxNft1, fxAccount, 1);
        await AssertHg.NftBalanceAsync(fxNft2, fxAccount, 1);
    }

    [Test]
    public async Task Can_Convert_To_Airdrop_Enabled_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 100);
        await using var fxNft1 = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var fxNft2 = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);

        await AssertHg.NftNotAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftNotAssociatedAsync(fxNft2, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferNftAsync(new Nft(fxNft1, 1), fxNft1.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft1.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Address = fxAccount,
            AutoAssociationLimit = -1,
            Signatory = fxAccount
        });

        var receipt2 = await client.TransferNftAsync(new Nft(fxNft2, 1), fxNft2.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft2.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt2.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.NftIsAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftIsAssociatedAsync(fxNft2, fxAccount);
        await AssertHg.NftBalanceAsync(fxNft1, fxAccount, 1);
        await AssertHg.NftBalanceAsync(fxNft2, fxAccount, 1);
    }
}
