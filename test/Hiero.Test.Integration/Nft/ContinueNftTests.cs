using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
namespace Hiero.Test.Integration.NftTokens;

public class ContinueNftTests
{
    [Test]
    public async Task Can_Resume_Nft_Coin_Trading()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.PauseTokenAsync(new PauseTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Signatory = fxNft.PausePrivateKey,
        });

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Suspended);

        await client.ContinueTokenAsync(new ContinueTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Signatory = fxNft.PausePrivateKey,
        });

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Tradable);

        await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task Can_Resume_Nft_Coin_Trading_Without_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.PauseTokenAsync(fxNft.CreateReceipt!.Token, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.PausePrivateKey));

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Suspended);

        await client.ContinueTokenAsync(fxNft.CreateReceipt!.Token, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.PausePrivateKey));

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Tradable);

        await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task Can_Continue_Nft_Coin_Trading_And_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.PauseTokenAsync(new PauseTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Signatory = fxNft.PausePrivateKey,
        });

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Suspended);

        var receipt = await client.ContinueTokenAsync(new ContinueTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Signatory = fxNft.PausePrivateKey,
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Tradable);

        await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task Can_Continue_Nft_Coin_Trading_And_Get_Record_Without_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.PauseTokenAsync(new PauseTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Signatory = fxNft.PausePrivateKey,
        });

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Suspended);

        var receipt = await client.ContinueTokenAsync(fxNft.CreateReceipt!.Token, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.PausePrivateKey));
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Tradable);

        await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task Can_Continue_Nft_Coin_Trading_And_Get_Record_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Tradable);

        await client.PauseTokenAsync(new PauseTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Signatory = fxNft.PausePrivateKey,
        });

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Suspended);

        var receipt = await client.ContinueTokenAsync(fxNft.CreateReceipt!.Token, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.PausePrivateKey));
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Tradable);

        await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task Can_Resume_Nft_Coin_Trading_From_Any_Account_With_Pause_Key()
    {
        await using var fxOther = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 100_00_000_000);
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.PauseTokenAsync(new PauseTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Signatory = fxNft.PausePrivateKey,
        });

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Suspended);

        await client.ContinueTokenAsync(new ContinueTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Signatory = fxNft.PausePrivateKey,
        }, ctx =>
        {
            ctx.Payer = fxOther.CreateReceipt!.Address;
            ctx.Signatory = fxOther.PrivateKey;
        });

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Tradable);

        await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task Continuing_An_Unpaused_Token_Is_Noop()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Tradable);

        await client.ContinueTokenAsync(new ContinueTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Signatory = fxNft.PausePrivateKey,
        });

        var info = (await fxAccount.GetTokenBalancesAsync()).FirstOrDefault(t => t.Token == fxNft.CreateReceipt!.Token);
        await Assert.That(info!.Balance).IsEqualTo(0);
        await Assert.That(info.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.AutoAssociated).IsFalse();

        await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        info = (await fxAccount.GetTokenBalancesAsync()).FirstOrDefault(t => t.Token == fxNft.CreateReceipt!.Token);
        await Assert.That(info!.Balance).IsEqualTo(1);
        await Assert.That(info.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Continue_Nft_Requires_Pause_Key_To_Sign_Transaction()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.PauseTokenAsync(new PauseTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Signatory = fxNft.PausePrivateKey,
        });

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Suspended);

        var ex = await Assert.That(async () =>
        {
            await client.ContinueTokenAsync(new ContinueTokenParams
            {
                Token = fxNft.CreateReceipt!.Token,
                Signatory = fxAccount.PrivateKey,
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Continue Token failed with status: InvalidSignature");

        ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsPaused);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: TokenIsPaused");
    }

    [Test]
    public async Task Can_Not_Continue_Nft_When_Pause_Not_Enabled()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.PauseEndorsement = null;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.NotApplicable);

        var ex = await Assert.That(async () =>
        {
            await client.ContinueTokenAsync(new ContinueTokenParams
            {
                Token = fxNft.CreateReceipt!.Token,
                Signatory = fxNft.PausePrivateKey,
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoPauseKey);
        await Assert.That(tex.Message).StartsWith("Continue Token failed with status: TokenHasNoPauseKey");

        await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.NotApplicable);
    }

    [Test]
    public async Task Can_Not_Schedule_Continue_Nft_Coin_Trading()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.PauseTokenAsync(new PauseTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Signatory = fxNft.PausePrivateKey,
        });

        await AssertHg.NftPausedAsync(fxNft, TokenTradableStatus.Suspended);

        var ex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ContinueTokenParams
            {
                Token = fxNft.CreateReceipt!.Token,
                Signatory = fxNft.PausePrivateKey,
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
        await Assert.That(tex.Message).StartsWith("Scheduling Continue Token failed with status: ScheduledTransactionNotInWhitelist");
    }
}
