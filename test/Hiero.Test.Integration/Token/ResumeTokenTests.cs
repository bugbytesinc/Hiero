using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
namespace Hiero.Test.Integration.Token;

public class ResumeTokenTests
{
    [Test]
    public async Task Can_Resume_Token_Coin_Trading()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = true;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Suspended);

        await client.ResumeTokenAsync(new ResumeTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount.CreateReceipt!.Address,
            Signatory = fxToken.SuspendPrivateKey,
        });

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Tradable);

        await client.TransferTokensAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task Reume_Token_Coin_Trading_With_Alias_Account_Is_Unsupported()
    {
        await using var fxAccount = await TestAliasAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = true;
        });
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxAccount.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount.PrivateKey);
        });

        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Suspended);

        var ex = await Assert.That(async () =>
        {
            await client.ResumeTokenAsync(new ResumeTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Holder = fxAccount.Alias,
                Signatory = fxToken.SuspendPrivateKey,
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Resume Token failed with status: InvalidAccountId");

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Suspended);
    }

    [Test]
    public async Task Can_Reume_Token_Coin_Trading_And_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = true;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Suspended);

        var receipt = await client.ResumeTokenAsync(new ResumeTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount.CreateReceipt!.Address,
            Signatory = fxToken.SuspendPrivateKey,
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

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Tradable);

        await client.TransferTokensAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task Can_Reume_Token_Coin_Trading_And_Get_Record_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = true;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Suspended);

        var receipt = await client.ResumeTokenAsync(fxToken.CreateReceipt!.Token, fxAccount.CreateReceipt!.Address, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.SuspendPrivateKey));
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

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Tradable);

        await client.TransferTokensAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task Can_Reume_Token_Coin_Trading_From_Any_Account_With_Suspend_Key()
    {
        await using var fxOther = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 100_00_000_000);
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = true;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Suspended);

        await client.ResumeTokenAsync(new ResumeTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount.CreateReceipt!.Address,
            Signatory = fxToken.SuspendPrivateKey,
        }, ctx =>
        {
            ctx.Payer = fxOther.CreateReceipt!.Address;
            ctx.Signatory = fxOther.PrivateKey;
        });

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Tradable);

        await client.TransferTokensAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task Resuming_An_Unfrozen_Account_Is_Noop()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = false;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Tradable);

        await client.ResumeTokenAsync(new ResumeTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount.CreateReceipt!.Address,
            Signatory = fxToken.SuspendPrivateKey,
        });

        var info = (await fxAccount.GetTokenBalancesAsync()).FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(info!.Balance).IsEqualTo(0);
        await Assert.That(info.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.AutoAssociated).IsFalse();

        await client.TransferTokensAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        info = (await fxAccount.GetTokenBalancesAsync()).FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(info!.Balance).IsEqualTo((long)xferAmount);
        await Assert.That(info.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Resume_A_Suspended_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = false;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Tradable);

        await client.SuspendTokenAsync(new SuspendTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount.CreateReceipt!.Address,
            Signatory = fxToken.SuspendPrivateKey,
        });

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Suspended);

        await client.ResumeTokenAsync(new ResumeTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount.CreateReceipt!.Address,
            Signatory = fxToken.SuspendPrivateKey,
        });

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Tradable);

        await client.TransferTokensAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task Resume_Token_Requires_Suspend_Key_To_Sign_Transaction()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = true;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Suspended);

        var ex = await Assert.That(async () =>
        {
            await client.ResumeTokenAsync(fxToken.CreateReceipt!.Token, fxAccount.CreateReceipt!.Address);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Resume Token failed with status: InvalidSignature");

        var info = (await fxAccount.GetTokenBalancesAsync()).FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(info!.Balance).IsEqualTo(0);
        await Assert.That(info.FreezeStatus).IsEqualTo(TokenTradableStatus.Suspended);
        await Assert.That(info.AutoAssociated).IsFalse();

        ex = await Assert.That(async () =>
        {
            await client.TransferTokensAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: AccountFrozenForToken");
    }

    [Test]
    public async Task Can_Not_Resume_Token_When_Freeze_Not_Enabled()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.SuspendEndorsement = null;
            fx.CreateParams.InitializeSuspended = false;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.NotApplicable);

        var ex = await Assert.That(async () =>
        {
            await client.ResumeTokenAsync(new ResumeTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Holder = fxAccount.CreateReceipt!.Address,
                Signatory = fxToken.SuspendPrivateKey,
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoFreezeKey);
        await Assert.That(tex.Message).StartsWith("Resume Token failed with status: TokenHasNoFreezeKey");

        var info = (await fxAccount.GetTokenBalancesAsync()).FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(info!.Balance).IsEqualTo(0);
        await Assert.That(info.FreezeStatus).IsEqualTo(TokenTradableStatus.NotApplicable);
        await Assert.That(info.AutoAssociated).IsFalse();

        await client.TransferTokensAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.NotApplicable);
    }

    //[Test]
    //public async Task CanNotScheduleReumeTokenCoinTrading()
    //{
    //    await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
    //    await using var fxAccount = await TestAccount.CreateAsync();
    //    await using var fxToken = await TestToken.CreateAsync(fx =>
    //    {
    //        fx.CreateParams.GrantKycEndorsement = null;
    //        fx.CreateParams.InitializeSuspended = true;
    //    }, fxAccount);
    //    await using var client = await TestNetwork.CreateClientAsync();
    //
    //    await AssertHg.TokenTradableStatusAsync(fxToken, fxAccount, TokenTradableStatus.Suspended);
    //    var ex = await Assert.That(async () =>
    //    {
    //        await client.ScheduleAsync(new ResumeTokenParams
    //        {
    //            Token = fxToken.CreateReceipt!.Token,
    //            Holder = fxAccount.CreateReceipt!.Address,
    //            Signatory = fxToken.SuspendPrivateKey,
    //        });
    //    }).ThrowsException();
    //    var tex = ex as TransactionException;
    //    await Assert.That(tex).IsNotNull();
    //    await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    //    await Assert.That(tex.Message).StartsWith("Scheduling Resume Token failed with status: ScheduledTransactionNotInWhitelist");
    //}

    [Test]
    public async Task Can_Schedule_And_Sign_Resume_Token()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = false;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.SuspendTokenAsync(new SuspendTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount.CreateReceipt!.Address,
            Signatory = fxToken.SuspendPrivateKey,
        });

        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new ResumeTokenParams
                {
                    Token = fxToken.CreateReceipt!.Token,
                    Holder = fxAccount.CreateReceipt!.Address,
                },
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<TransactionException>();
        await Assert.That(((TransactionException)tex!).Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    }
}
