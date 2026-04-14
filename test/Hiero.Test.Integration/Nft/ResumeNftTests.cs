using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
namespace Hiero.Test.Integration.NftTokens;

public class ResumeNftTests
{
    [Test]
    public async Task Can_Resume_Nft_Coin_Trading()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = true;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Suspended);

        await client.ResumeTokenAsync(new ResumeTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Holder = fxAccount.CreateReceipt!.Address,
            Signatory = fxNft.SuspendPrivateKey,
        });

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);

        await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task Can_Resume_Nft_Coin_Trading_And_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = true;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Suspended);

        var receipt = await client.ResumeTokenAsync(new ResumeTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Holder = fxAccount.CreateReceipt!.Address,
            Signatory = fxNft.SuspendPrivateKey,
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

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);

        await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task Can_Resume_Nft_Coin_Trading_And_Get_Record_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = true;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Suspended);

        var receipt = await client.ResumeTokenAsync(fxNft.CreateReceipt!.Token, fxAccount.CreateReceipt!.Address, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.SuspendPrivateKey));
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

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);

        await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task Can_Resume_Nft_Coin_Trading_From_Any_Account_With_Suspend_Key()
    {
        await using var fxOther = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 100_00_000_000);
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = true;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Suspended);

        await client.ResumeTokenAsync(new ResumeTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Holder = fxAccount.CreateReceipt!.Address,
            Signatory = fxNft.SuspendPrivateKey,
        }, ctx =>
        {
            ctx.Payer = fxOther.CreateReceipt!.Address;
            ctx.Signatory = fxOther.PrivateKey;
        });

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);

        await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task Resuming_An_Unfrozen_Account_Is_Noop()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = false;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);

        await client.ResumeTokenAsync(new ResumeTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Holder = fxAccount.CreateReceipt!.Address,
            Signatory = fxNft.SuspendPrivateKey,
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
    public async Task Can_Resume_A_Suspended_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = false;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);

        await client.SuspendTokenAsync(new SuspendTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Holder = fxAccount.CreateReceipt!.Address,
            Signatory = fxNft.SuspendPrivateKey,
        });

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Suspended);

        await client.ResumeTokenAsync(new ResumeTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Holder = fxAccount.CreateReceipt!.Address,
            Signatory = fxNft.SuspendPrivateKey,
        });

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);

        await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task Resume_Nft_Requires_Suspend_Key_To_Sign_Transaction()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = true;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Suspended);

        var ex = await Assert.That(async () =>
        {
            await client.ResumeTokenAsync(fxNft.CreateReceipt!.Token, fxAccount.CreateReceipt!.Address);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Resume Token failed with status: InvalidSignature");

        var info = (await fxAccount.GetTokenBalancesAsync()).FirstOrDefault(t => t.Token == fxNft.CreateReceipt!.Token);
        await Assert.That(info!.Balance).IsEqualTo(0);
        await Assert.That(info.FreezeStatus).IsEqualTo(TokenTradableStatus.Suspended);
        await Assert.That(info.AutoAssociated).IsFalse();

        ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: AccountFrozenForToken");
    }

    [Test]
    public async Task Can_Not_Resume_Nft_When_Freeze_Not_Enabled()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.SuspendEndorsement = null;
            fx.CreateParams.InitializeSuspended = false;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.NotApplicable);

        var ex = await Assert.That(async () =>
        {
            await client.ResumeTokenAsync(new ResumeTokenParams
            {
                Token = fxNft.CreateReceipt!.Token,
                Holder = fxAccount.CreateReceipt!.Address,
                Signatory = fxNft.SuspendPrivateKey,
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoFreezeKey);
        await Assert.That(tex.Message).StartsWith("Resume Token failed with status: TokenHasNoFreezeKey");

        var info = (await fxAccount.GetTokenBalancesAsync()).FirstOrDefault(t => t.Token == fxNft.CreateReceipt!.Token);
        await Assert.That(info!.Balance).IsEqualTo(0);
        await Assert.That(info.FreezeStatus).IsEqualTo(TokenTradableStatus.NotApplicable);
        await Assert.That(info.AutoAssociated).IsFalse();

        await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.NotApplicable);
    }

    [Test]
    public async Task Can_Not_Schedule_Resume_Nft_Coin_Trading()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = true;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Suspended);

        var ex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ResumeTokenParams
            {
                Holder = fxAccount.CreateReceipt!.Address,
                Token = fxNft.CreateReceipt!.Token,
                Signatory = fxNft.SupplyPrivateKey,
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
        await Assert.That(tex.Message).StartsWith("Scheduling Resume Token failed with status: ScheduledTransactionNotInWhitelist");
    }
}
