using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Token;

public class GrantTokenTests
{
    [Test]
    public async Task Can_Grant_Tokens()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenKycStatusAsync(fxToken, fxAccount, TokenKycStatus.Revoked);

        var receipt = await client.GrantTokenKycAsync(new GrantTokenKycParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount,
            Signatory = fxToken.GrantPrivateKey
        });

        await AssertHg.TokenKycStatusAsync(fxToken, fxAccount, TokenKycStatus.Granted);

        receipt = await client.TransferTokensAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await AssertHg.TokenKycStatusAsync(fxToken, fxAccount, TokenKycStatus.Granted);
    }

    [Test]
    public async Task Grant_Token_Coins_To_Alias_Account_Is_Unsupported()
    {
        await using var fxAccount = await TestAliasAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await client.AssociateTokenAsync(fxAccount, fxToken.CreateReceipt!.Token, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount.PrivateKey);
        });

        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenKycStatusAsync(fxToken, fxAccount, TokenKycStatus.Revoked);

        var ex = await Assert.That(async () =>
        {
            await client.GrantTokenKycAsync(new GrantTokenKycParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Holder = fxAccount.Alias,
                Signatory = fxToken.GrantPrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Grant Token KYC failed with status: InvalidAccountId");

        await AssertHg.TokenKycStatusAsync(fxToken, fxAccount, TokenKycStatus.Revoked);
    }

    [Test]
    public async Task Can_Grant_Tokens_And_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenKycStatusAsync(fxToken, fxAccount, TokenKycStatus.Revoked);

        var grantReceipt = await client.GrantTokenKycAsync(new GrantTokenKycParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount,
            Signatory = fxToken.GrantPrivateKey
        });
        var record = await client.GetTransactionRecordAsync(grantReceipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        await AssertHg.TokenKycStatusAsync(fxToken, fxAccount, TokenKycStatus.Granted);

        var receipt = await client.TransferTokensAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await AssertHg.TokenKycStatusAsync(fxToken, fxAccount, TokenKycStatus.Granted);
    }

    [Test]
    public async Task Can_Grant_Tokens_And_Get_Record_Without_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenKycStatusAsync(fxToken, fxAccount, TokenKycStatus.Revoked);

        var grantReceipt = await client.GrantTokenKycAsync(fxToken.CreateReceipt!.Token, fxAccount, ctx =>
        {
            ctx.Signatory = new Signatory(TestNetwork.PrivateKey, fxToken.GrantPrivateKey);
        });
        var record = await client.GetTransactionRecordAsync(grantReceipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        await AssertHg.TokenKycStatusAsync(fxToken, fxAccount, TokenKycStatus.Granted);

        var receipt = await client.TransferTokensAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await AssertHg.TokenKycStatusAsync(fxToken, fxAccount, TokenKycStatus.Granted);
    }

    [Test]
    public async Task Can_Grant_Token_Coins_From_Wny_Account_With_Grant_Key()
    {
        await using var fxOther = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;

        await AssertHg.TokenKycStatusAsync(fxToken, fxAccount, TokenKycStatus.Revoked);

        var receipt = await client.GrantTokenKycAsync(new GrantTokenKycParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount,
            Signatory = fxToken.GrantPrivateKey
        }, ctx =>
        {
            ctx.Payer = fxOther.CreateReceipt!.Address;
            ctx.Signatory = fxOther.PrivateKey;
        });

        await AssertHg.TokenKycStatusAsync(fxToken, fxAccount, TokenKycStatus.Granted);

        receipt = await client.TransferTokensAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await AssertHg.TokenKycStatusAsync(fxToken, fxAccount, TokenKycStatus.Granted);
    }

    [Test]
    public async Task Grant_Token_Coins_Requires_Grant_Key_Signature()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.TokenNotAssociatedAsync(fxToken, fxAccount);

        var ex = await Assert.That(async () =>
        {
            await client.GrantTokenKycAsync(fxToken.CreateReceipt!.Token, fxAccount);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Grant Token KYC failed with status: InvalidSignature");
    }

    [Test]
    public async Task Cannot_Grant_Token_Coins_When_Grant_KYC_Turned_Off()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.TokenKycStatusAsync(fxToken, fxAccount, TokenKycStatus.NotApplicable);

        var ex = await Assert.That(async () =>
        {
            await client.GrantTokenKycAsync(new GrantTokenKycParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Holder = fxAccount,
                Signatory = fxToken.GrantPrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoKycKey);
        await Assert.That(tex.Message).StartsWith("Grant Token KYC failed with status: TokenHasNoKycKey");
    }

    [Test]
    public async Task Cannot_Grant_Token_Coins_Before_Auto_Association()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount2);
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.GrantTokenKycAsync(new GrantTokenKycParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Holder = fxAccount1,
                Signatory = fxToken.GrantPrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenNotAssociatedToAccount);
        await Assert.That(tex.Message).StartsWith("Grant Token KYC failed with status: TokenNotAssociatedToAccount");

        var receipt = await client.GrantTokenKycAsync(new GrantTokenKycParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount2,
            Signatory = fxToken.GrantPrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Grant_Token_Kyc()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new GrantTokenKycParams
                {
                    Token = fxToken.CreateReceipt!.Token,
                    Holder = fxAccount,
                },
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<TransactionException>();
        await Assert.That(((TransactionException)tex!).Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    }
}
