using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
namespace Hiero.Test.Integration.Token;

public class PauseTokenTests
{
    [Test]
    public async Task Can_Pause_Token_Coin_Trading()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Tradable);

        await client.PauseTokenAsync(new PauseTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Signatory = fxToken.PausePrivateKey,
        });

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Suspended);

        var ex = await Assert.That(async () =>
        {
            await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsPaused);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: TokenIsPaused");

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Suspended);
    }

    [Test]
    public async Task Can_Pause_Token_Coin_Trading_And_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Tradable);

        var receipt = await client.PauseTokenAsync(new PauseTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Signatory = fxToken.PausePrivateKey,
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

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Suspended);

        var ex = await Assert.That(async () =>
        {
            await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsPaused);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: TokenIsPaused");

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Suspended);
    }

    [Test]
    public async Task Can_Pause_Token_Coin_Trading_And_Get_Record_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Tradable);

        var receipt = await client.PauseTokenAsync(fxToken.CreateReceipt!.Token, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.PausePrivateKey));
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

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Suspended);

        var ex = await Assert.That(async () =>
        {
            await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsPaused);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: TokenIsPaused");

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Suspended);
    }

    [Test]
    public async Task Can_Pause_Token_Coin_Trading_From_Any_Account_With_Pause_Key()
    {
        await using var fxOther = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Tradable);

        await client.PauseTokenAsync(new PauseTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Signatory = fxToken.PausePrivateKey,
        }, ctx =>
        {
            ctx.Payer = fxOther.CreateReceipt!.Address;
            ctx.Signatory = fxOther.PrivateKey;
        });

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Suspended);

        var ex = await Assert.That(async () =>
        {
            await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsPaused);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: TokenIsPaused");

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Suspended);
    }

    [Test]
    public async Task Pausing_A_Paused_Token_Is_A_Noop()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Tradable);

        await client.PauseTokenAsync(new PauseTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Signatory = fxToken.PausePrivateKey,
        });

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Suspended);

        await client.PauseTokenAsync(new PauseTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Signatory = fxToken.PausePrivateKey,
        });

        var ex = await Assert.That(async () =>
        {
            await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsPaused);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: TokenIsPaused");
    }

    [Test]
    public async Task Pause_Token_Requires_Pause_Key_To_Sign_Transaciton()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        });
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Tradable);

        var ex = await Assert.That(async () =>
        {
            await client.PauseTokenAsync(fxToken.CreateReceipt!.Token);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Pause Token failed with status: InvalidSignature");

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task Cannot_Pause_Token_When_Freeze_Not_Enabled()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.PauseEndorsement = null;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.NotApplicable);

        var ex = await Assert.That(async () =>
        {
            await client.PauseTokenAsync(new PauseTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Signatory = fxToken.PausePrivateKey,
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoPauseKey);
        await Assert.That(tex.Message).StartsWith("Pause Token failed with status: TokenHasNoPauseKey");

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.NotApplicable);

        await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.NotApplicable);
    }

    //[Test]
    //public async Task CanNotSchedulePauseTokenCoinTrading()
    //{
    //    await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
    //    await using var fxAccount = await TestAccount.CreateAsync();
    //    await using var fxToken = await TestToken.CreateAsync(fx =>
    //    {
    //        fx.CreateParams.GrantKycEndorsement = null;
    //    }, fxAccount);
    //    var circulation = fxToken.CreateParams.Circulation;
    //    var xferAmount = circulation / 3;
    //    await using var client = await TestNetwork.CreateClientAsync();
    //    await AssertHg.TokenPausedAsync(fxToken, TokenTradableStatus.Tradable);
    //    var ex = await Assert.That(async () =>
    //    {
    //        await client.PauseTokenAsync(new PauseTokenParams
    //        {
    //            Token = fxToken.CreateReceipt!.Token,
    //            Signatory = fxToken.PausePrivateKey,
    //        });
    //    }).ThrowsException();
    //    var tex = ex as TransactionException;
    //    await Assert.That(tex).IsNotNull();
    //    await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    //    await Assert.That(tex.Message).StartsWith("Scheduling Pause Token failed with status: ScheduledTransactionNotInWhitelist");
    //}

    [Test]
    public async Task Can_Schedule_And_Sign_Pause_Token()
    {
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new PauseTokenParams
                {
                    Token = fxToken.CreateReceipt!.Token,
                },
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<TransactionException>();
        await Assert.That(((TransactionException)tex!).Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    }
}
