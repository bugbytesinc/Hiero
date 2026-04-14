using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Royalties;

public class CreateRoyaltiesTests
{
    [Test]
    public async Task Can_Create_Token_With_Fixed_Royalty()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var comToken = await TestToken.CreateAsync(null, fxAccount);
        var fixedRoyalty = new FixedRoyalty(fxAccount, comToken, 100);
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Royalties = new IRoyalty[] { fixedRoyalty };
        }, fxAccount);
        await Assert.That(fxToken.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Royalties.Count).IsEqualTo(1);
        await Assert.That(info.Royalties[0]).IsEqualTo(fixedRoyalty);
    }

    [Test]
    public async Task Can_Create_Token_With_Fractional_Royalty()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        var tokenRoyalty = new TokenRoyalty(fxAccount, 1, 2, 1, 100);
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Royalties = new IRoyalty[] { tokenRoyalty };
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxAccount.PrivateKey);
        });
        await Assert.That(fxToken.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Royalties.Count).IsEqualTo(1);
        await Assert.That(info.Royalties[0]).IsEqualTo(tokenRoyalty);
    }

    [Test]
    public async Task Can_Create_Token_With_Fixed_And_Fractional_Royalties()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var comToken = await TestToken.CreateAsync(null, fxAccount);
        var fixedRoyalty = new FixedRoyalty(fxAccount, comToken, 100);
        var tokenRoyalty = new TokenRoyalty(fxAccount, 1, 2, 1, 100);
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Royalties = new IRoyalty[] { fixedRoyalty, tokenRoyalty };
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxAccount.PrivateKey);
        });
        await Assert.That(fxToken.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Royalties.Count).IsEqualTo(2);
        await Assert.That(info.Royalties.First(f => f.RoyaltyType == RoyaltyType.Fixed)).IsEqualTo(fixedRoyalty);
        await Assert.That(info.Royalties.First(f => f.RoyaltyType == RoyaltyType.Token)).IsEqualTo(tokenRoyalty);
    }

    [Test]
    public async Task Can_Add_Fixed_Royalty_To_Token_Definition()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var comToken = await TestToken.CreateAsync(null, fxAccount);
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);
        await Assert.That(fxToken.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var fixedRoyalty = new FixedRoyalty(fxAccount, comToken, 100);
        await using var client = await TestNetwork.CreateClientAsync();
        await client.UpdateRoyaltiesAsync(fxToken.CreateReceipt!.Token, new IRoyalty[] { fixedRoyalty }, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.RoyaltiesPrivateKey));

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Royalties.Count).IsEqualTo(1);
        await Assert.That(info.Royalties[0]).IsEqualTo(fixedRoyalty);
    }

    [Test]
    public async Task Can_Add_Fractional_Royalty_To_Token_Definition()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);
        await Assert.That(fxToken.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var tokenRoyalty = new TokenRoyalty(fxAccount, 1, 2, 1, 100);
        await using var client = await TestNetwork.CreateClientAsync();
        await client.UpdateRoyaltiesAsync(fxToken.CreateReceipt!.Token, new IRoyalty[] { tokenRoyalty }, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.RoyaltiesPrivateKey));

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Royalties.Count).IsEqualTo(1);
        await Assert.That(info.Royalties[0]).IsEqualTo(tokenRoyalty);
    }

    [Test]
    public async Task Can_Add_Fixed_And_Fractional_Royalties_To_Token_Definition()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var comToken = await TestToken.CreateAsync(null, fxAccount);
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);
        await Assert.That(fxToken.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var fixedRoyalty = new FixedRoyalty(fxAccount, comToken, 100);
        var tokenRoyalty = new TokenRoyalty(fxAccount, 1, 2, 1, 100);
        await using var client = await TestNetwork.CreateClientAsync();
        await client.UpdateRoyaltiesAsync(fxToken.CreateReceipt!.Token, new IRoyalty[] { fixedRoyalty, tokenRoyalty }, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.RoyaltiesPrivateKey));

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Royalties.Count).IsEqualTo(2);
        await Assert.That(info.Royalties.First(f => f.RoyaltyType == RoyaltyType.Fixed)).IsEqualTo(fixedRoyalty);
        await Assert.That(info.Royalties.First(f => f.RoyaltyType == RoyaltyType.Token)).IsEqualTo(tokenRoyalty);
    }

    [Test]
    public async Task Can_Add_Fixed_And_Fractional_Royalties_To_Token_Definition_With_Signatory_In_Context()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var comToken = await TestToken.CreateAsync(null, fxAccount);
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);
        await Assert.That(fxToken.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var fixedRoyalty = new FixedRoyalty(fxAccount, comToken, 100);
        var tokenRoyalty = new TokenRoyalty(fxAccount, 1, 2, 1, 100);
        await using var client = await TestNetwork.CreateClientAsync();
        await client.UpdateRoyaltiesAsync(fxToken.CreateReceipt!.Token, new IRoyalty[] { fixedRoyalty, tokenRoyalty }, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.RoyaltiesPrivateKey));

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Royalties.Count).IsEqualTo(2);
        await Assert.That(info.Royalties.First(f => f.RoyaltyType == RoyaltyType.Fixed)).IsEqualTo(fixedRoyalty);
        await Assert.That(info.Royalties.First(f => f.RoyaltyType == RoyaltyType.Token)).IsEqualTo(tokenRoyalty);
    }

    [Test]
    public async Task Can_Create_Asset_With_Fixed_Royalty()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var comToken = await TestToken.CreateAsync(null, fxAccount);
        var fixedRoyalty = new FixedRoyalty(fxAccount, comToken, 100);
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Royalties = new IRoyalty[] { fixedRoyalty };
        }, fxAccount);
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Royalties.Count).IsEqualTo(1);
        await Assert.That(info.Royalties[0]).IsEqualTo(fixedRoyalty);
    }

    [Test]
    public async Task Can_Create_Asset_With_Value_Royalty()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var comToken = await TestToken.CreateAsync(null, fxAccount);
        var nftRoyalty = new NftRoyalty(fxAccount, 1, 2, 1, comToken.CreateReceipt!.Token);
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Royalties = new IRoyalty[] { nftRoyalty };
        }, fxAccount);
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Royalties.Count).IsEqualTo(1);
        await Assert.That(info.Royalties[0]).IsEqualTo(nftRoyalty);
    }

    [Test]
    public async Task Can_Add_Fixed_Royalty_To_Asset_Definition()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var comToken = await TestToken.CreateAsync(null, fxAccount);
        await using var fxNft = await TestNft.CreateAsync(null, fxAccount);
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var fixedRoyalty = new FixedRoyalty(fxAccount, comToken, 100);
        await using var client = await TestNetwork.CreateClientAsync();
        await client.UpdateRoyaltiesAsync(fxNft.CreateReceipt!.Token, new IRoyalty[] { fixedRoyalty }, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.RoyaltiesPrivateKey));

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Royalties.Count).IsEqualTo(1);
        await Assert.That(info.Royalties[0]).IsEqualTo(fixedRoyalty);
    }

    [Test]
    public async Task Can_Not_Add_Fractional_Royalty_To_Asset_Definition()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(null, fxAccount);
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var tokenRoyalty = new TokenRoyalty(fxAccount, 1, 2, 1, 100);
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.UpdateRoyaltiesAsync(fxNft.CreateReceipt!.Token, new IRoyalty[] { tokenRoyalty }, ctx =>
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.RoyaltiesPrivateKey));
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.CustomFractionalFeeOnlyAllowedForFungibleCommon);
        await Assert.That(tex.Message).StartsWith("Royalties Update failed with status: CustomFractionalFeeOnlyAllowedForFungibleCommon");
    }

    [Test]
    public async Task Can_Not_Create_Token_With_Royalty_Royalty()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        var nftRoyalty = new NftRoyalty(fxAccount, 1, 2, 0, EntityId.None);
        var ex = await Assert.That(async () =>
        {
            await using var fxToken = await TestToken.CreateAsync(fx =>
            {
                fx.CreateParams.Royalties = new IRoyalty[] { nftRoyalty };
                fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.CustomRoyaltyFeeOnlyAllowedForNonFungibleUnique);
        await Assert.That(tex.Message).StartsWith("Create Token failed with status: CustomRoyaltyFeeOnlyAllowedForNonFungibleUnique");
    }

    [Test]
    public async Task Can_Not_Add_Royalty_Royalty_To_Token_Definition()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);
        await Assert.That(fxToken.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var nftRoyalty = new NftRoyalty(fxAccount, 1, 2, 0, EntityId.None);
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.UpdateRoyaltiesAsync(fxToken.CreateReceipt!.Token, new IRoyalty[] { nftRoyalty }, ctx =>
                ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.RoyaltiesPrivateKey));
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.CustomRoyaltyFeeOnlyAllowedForNonFungibleUnique);
        await Assert.That(tex.Message).StartsWith("Royalties Update failed with status: CustomRoyaltyFeeOnlyAllowedForNonFungibleUnique");
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Update_Royalties()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var fixedRoyalty = new FixedRoyalty(fxAccount, EntityId.None, 10);

        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new UpdateRoyaltiesParams
                {
                    Token = fxToken.CreateReceipt!.Token,
                    Royalties = new IRoyalty[] { fixedRoyalty },
                },
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<TransactionException>();
        await Assert.That(((TransactionException)tex!).Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    }
}
