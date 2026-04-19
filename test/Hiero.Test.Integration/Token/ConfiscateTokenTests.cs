using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Token;

public class ConfiscateTokenTests
{
    [Test]
    public async Task Can_Confiscate_Token_Coins()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var xferAmount = 2 * fxToken.CreateParams.Circulation / (ulong)Generator.Integer(3, 5);
        var expectedTreasury = fxToken.CreateParams.Circulation - xferAmount;

        var xferReceipt = await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation);

        var receipt = await client.ConfiscateTokenAsync(new ConfiscateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount,
            Amount = xferAmount,
            Signatory = fxToken.ConfiscatePrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.Circulation).IsEqualTo(expectedTreasury);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(expectedTreasury);
    }

    [Test]
    public async Task Can_Confiscate_Token_Coins_From_Alias_Account()
    {
        await using var fxAccount = await TestAliasAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var client = await TestNetwork.CreateClientAsync();
        await client.AssociateTokenAsync(fxToken.CreateReceipt!.Token, fxAccount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount.PrivateKey);
        });

        var xferAmount = 2 * fxToken.CreateParams.Circulation / (ulong)Generator.Integer(3, 5);
        var expectedTreasury = fxToken.CreateParams.Circulation - xferAmount;

        var xferReceipt = await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation);

        var receipt = await client.ConfiscateTokenAsync(new ConfiscateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount.Alias,
            Amount = xferAmount,
            Signatory = fxToken.ConfiscatePrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.Circulation).IsEqualTo(expectedTreasury);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(expectedTreasury);
    }

    [Test]
    public async Task Can_Confiscate_A_Small_Amount_Token_Coins()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var xferAmount = 2ul;
        var expectedTreasury = fxToken.CreateParams.Circulation - xferAmount;

        var xferReceipt = await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation);

        var receipt = await client.ConfiscateTokenAsync(new ConfiscateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount,
            Amount = xferAmount,
            Signatory = fxToken.ConfiscatePrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.Circulation).IsEqualTo(expectedTreasury);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(expectedTreasury);
    }

    [Test]
    public async Task Can_Confiscate_A_Small_Amount_Token_Coins_And_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var xferAmount = 2ul;
        var expectedTreasury = fxToken.CreateParams.Circulation - xferAmount;

        var receipt = await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation);

        var confiscateReceipt = await client.ConfiscateTokenAsync(new ConfiscateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount,
            Amount = xferAmount,
            Signatory = fxToken.ConfiscatePrivateKey
        });
        var record = (TokenRecord)await client.GetTransactionRecordAsync(confiscateReceipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.Circulation).IsEqualTo(expectedTreasury);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(expectedTreasury);
    }

    [Test]
    public async Task Can_Confiscate_A_Small_Amount_Token_Coins_And_Get_Record_Without_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var xferAmount = 2ul;
        var expectedTreasury = fxToken.CreateParams.Circulation - xferAmount;

        var receipt = await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation);

        var confiscateReceipt = await client.ConfiscateTokenAsync(fxToken, fxAccount, xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(TestNetwork.PrivateKey, fxToken.ConfiscatePrivateKey);
        });
        var record = (TokenRecord)await client.GetTransactionRecordAsync(confiscateReceipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.Circulation).IsEqualTo(expectedTreasury);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(expectedTreasury);
    }

    [Test]
    public async Task Can_Confiscate_A_Small_Amount_Token_Coins_From_Any_Account_With_Confiscate_Key()
    {
        await using var fxOther = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var xferAmount = 2ul;
        var expectedTreasury = fxToken.CreateParams.Circulation - xferAmount;

        var xferReceipt = await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation);

        var receipt = await client.ConfiscateTokenAsync(new ConfiscateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount,
            Amount = xferAmount,
            Signatory = fxToken.ConfiscatePrivateKey
        }, ctx =>
        {
            ctx.Payer = fxOther.CreateReceipt!.Address;
            ctx.Signatory = fxOther.PrivateKey;
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.Circulation).IsEqualTo(expectedTreasury);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(expectedTreasury);
    }

    [Test]
    public async Task Can_Not_Confiscate_More_Tokens_Than_Account_Has()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var xferAmount = 2ul;
        var expectedTreasury = fxToken.CreateParams.Circulation - xferAmount;

        var receipt = await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation);

        var ex = await Assert.That(async () =>
        {
            await client.ConfiscateTokenAsync(new ConfiscateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Holder = fxAccount,
                Amount = xferAmount * 2,
                Signatory = fxToken.ConfiscatePrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidWipingAmount);
        await Assert.That(tex.Message).StartsWith("Confiscate Tokens failed with status: InvalidWipingAmount");

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
    }

    [Test]
    public async Task Confiscate_Record_Includes_Token_Transfers()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var xferAmount = 2ul;
        var expectedTreasury = fxToken.CreateParams.Circulation - xferAmount;

        var receipt = await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation);

        var confiscateReceipt = await client.ConfiscateTokenAsync(new ConfiscateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount,
            Amount = xferAmount,
            Signatory = fxToken.ConfiscatePrivateKey
        });
        var record = (TokenRecord)await client.GetTransactionRecordAsync(confiscateReceipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Circulation).IsEqualTo(expectedTreasury);
        await Assert.That(record.TokenTransfers).HasSingleItem();
        await Assert.That(record.NftTransfers).IsEmpty();
        await Assert.That(record.Royalties).IsEmpty();
        await Assert.That(record.Associations).IsEmpty();

        var xfer = record.TokenTransfers[0];
        await Assert.That(xfer.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(xfer.Account).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(xfer.Amount).IsEqualTo(-(long)xferAmount);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation - xferAmount);
    }

    [Test]
    public async Task Confiscation_Requires_Confiscate_Key_Signature()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var xferAmount = 2ul;
        var expectedTreasury = fxToken.CreateParams.Circulation - xferAmount;

        var receipt = await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation);

        var ex = await Assert.That(async () =>
        {
            await client.ConfiscateTokenAsync(fxToken, fxAccount, xferAmount);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Confiscate Tokens failed with status: InvalidSignature");

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
    }

    [Test]
    public async Task Cannot_Confiscate_When_No_Confiscation_Endorsement()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.ConfiscateEndorsement = null;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var xferAmount = 2ul;
        var expectedTreasury = fxToken.CreateParams.Circulation - xferAmount;

        var receipt = await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation);

        var ex = await Assert.That(async () =>
        {
            await client.ConfiscateTokenAsync(new ConfiscateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Holder = fxAccount,
                Amount = xferAmount,
                Signatory = fxToken.ConfiscatePrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoWipeKey);
        await Assert.That(tex.Message).StartsWith("Confiscate Tokens failed with status: TokenHasNoWipeKey");

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Confiscate_Token()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var xferAmount = 2ul;

        await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new ConfiscateTokenParams
                {
                    Token = fxToken.CreateReceipt!.Token,
                    Holder = fxAccount,
                    Amount = 1,
                },
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<TransactionException>();
        await Assert.That(((TransactionException)tex!).Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    }
}
