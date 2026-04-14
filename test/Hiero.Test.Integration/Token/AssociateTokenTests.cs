using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Token;

public class AssociateTokenTests
{
    [Test]
    public async Task Can_Associate_Token_With_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync();

        await AssertHg.TokenNotAssociatedAsync(fxToken, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = [fxToken.CreateReceipt!.Token],
            Signatory = fxAccount.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var association = await AssertHg.TokenIsAssociatedAsync(fxToken, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Associate_Token_With_Alias_Account_Is_Unsupported()
    {
        await using var fxAccount = await TestAliasAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync();

        await AssertHg.TokenNotAssociatedAsync(fxToken, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.AssociateTokensAsync(new AssociateTokenParams
            {
                Account = fxAccount.Alias,
                Tokens = [fxToken.CreateReceipt!.Token],
                Signatory = fxAccount.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Associate Token with Account failed with status: InvalidAccountId");

        await AssertHg.TokenNotAssociatedAsync(fxToken, fxAccount);
    }

    [Test]
    public async Task Can_Associate_Token_With_Account_And_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync();

        await AssertHg.TokenNotAssociatedAsync(fxToken, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = [fxToken.CreateReceipt!.Token],
            Signatory = fxAccount.PrivateKey
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        var association = await AssertHg.TokenIsAssociatedAsync(fxToken, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Associate_Token_With_Account_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxToken = await TestToken.CreateAsync();

        await AssertHg.TokenNotAssociatedAsync(fxToken, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokenAsync(fxAccount.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = fxAccount.PrivateKey;
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var association = await AssertHg.TokenIsAssociatedAsync(fxToken, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Associate_Token_With_Account_And_Get_Record_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxToken = await TestToken.CreateAsync();

        await AssertHg.TokenNotAssociatedAsync(fxToken, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = [fxToken.CreateReceipt!.Token],
        }, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = fxAccount.PrivateKey;
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(fxAccount.CreateReceipt!.Address);

        var association = await AssertHg.TokenIsAssociatedAsync(fxToken, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Associate_Multiple_Tokens_With_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken1 = await TestToken.CreateAsync();
        await using var fxToken2 = await TestToken.CreateAsync();

        var tokens = new EntityId[] { fxToken1.CreateReceipt!.Token, fxToken2.CreateReceipt!.Token };

        await AssertHg.TokenNotAssociatedAsync(fxToken1, fxAccount);
        await AssertHg.TokenNotAssociatedAsync(fxToken2, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = tokens,
            Signatory = fxAccount.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var association = await AssertHg.TokenIsAssociatedAsync(fxToken1, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken1.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        association = await AssertHg.TokenIsAssociatedAsync(fxToken2, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken2.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Associate_Multiple_Tokens_With_Account_And_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken1 = await TestToken.CreateAsync();
        await using var fxToken2 = await TestToken.CreateAsync();

        var tokens = new EntityId[] { fxToken1.CreateReceipt!.Token, fxToken2.CreateReceipt!.Token };

        await AssertHg.TokenNotAssociatedAsync(fxToken1, fxAccount);
        await AssertHg.TokenNotAssociatedAsync(fxToken2, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = tokens,
            Signatory = fxAccount.PrivateKey
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        var association = await AssertHg.TokenIsAssociatedAsync(fxToken1, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken1.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        association = await AssertHg.TokenIsAssociatedAsync(fxToken2, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken2.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Associate_Multiple_Tokens_With_Account_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxToken1 = await TestToken.CreateAsync();
        await using var fxToken2 = await TestToken.CreateAsync();

        var tokens = new EntityId[] { fxToken1.CreateReceipt!.Token, fxToken2.CreateReceipt!.Token };

        await AssertHg.TokenNotAssociatedAsync(fxToken1, fxAccount);
        await AssertHg.TokenNotAssociatedAsync(fxToken2, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = tokens,
        }, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = fxAccount.PrivateKey;
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var association = await AssertHg.TokenIsAssociatedAsync(fxToken1, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken1.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        association = await AssertHg.TokenIsAssociatedAsync(fxToken2, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken2.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Associate_Multiple_Tokens_With_Account_And_Get_Record_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxToken1 = await TestToken.CreateAsync();
        await using var fxToken2 = await TestToken.CreateAsync();

        var tokens = new EntityId[] { fxToken1.CreateReceipt!.Token, fxToken2.CreateReceipt!.Token };

        await AssertHg.TokenNotAssociatedAsync(fxToken1, fxAccount);
        await AssertHg.TokenNotAssociatedAsync(fxToken2, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = tokens,
        }, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = fxAccount.PrivateKey;
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(fxAccount.CreateReceipt!.Address);

        var association = await AssertHg.TokenIsAssociatedAsync(fxToken1, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken1.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        association = await AssertHg.TokenIsAssociatedAsync(fxToken2, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken2.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task No_Token_Balance_Record_Exists_When_Not_Associated()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync();

        await AssertHg.TokenNotAssociatedAsync(fxToken, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = [fxToken.CreateReceipt!.Token],
            Signatory = fxAccount.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var association = await AssertHg.TokenIsAssociatedAsync(fxToken, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Association_Requires_Signing_By_Target_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync();

        await AssertHg.TokenNotAssociatedAsync(fxToken, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.AssociateTokenAsync(fxAccount.CreateReceipt!.Address, fxToken.CreateReceipt!.Token);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Associate Token with Account failed with status: InvalidSignature");

        await AssertHg.TokenNotAssociatedAsync(fxToken, fxAccount);
    }

    [Test]
    public async Task Association_Requires_Token_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.AssociateTokenAsync(fxAccount.CreateReceipt!.Address, EntityId.None);
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("token");
        await Assert.That(ane.Message).StartsWith("Token is missing. Please check that it is not null or empty.");

        ex = await Assert.That(async () =>
        {
            await client.AssociateTokenAsync(fxAccount.CreateReceipt!.Address, null!);
        }).ThrowsException();
        ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("token");
        await Assert.That(ane.Message).StartsWith("Token is missing. Please check that it is not null or empty.");

        ex = await Assert.That(async () =>
        {
            await client.AssociateTokensAsync(new AssociateTokenParams
            {
                Account = fxAccount.CreateReceipt!.Address,
                Tokens = null!,
            });
        }).ThrowsException();
        ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("Tokens");
        await Assert.That(ane.Message).StartsWith("The list of tokens cannot be null.");

        ex = await Assert.That(async () =>
        {
            await client.AssociateTokensAsync(new AssociateTokenParams
            {
                Account = fxAccount.CreateReceipt!.Address,
                Tokens = [null!],
            });
        }).ThrowsException();
        var aoe = ex as ArgumentOutOfRangeException;
        await Assert.That(aoe).IsNotNull();
        await Assert.That(aoe!.ParamName).IsEqualTo("Tokens");
        await Assert.That(aoe.Message).StartsWith("The list of tokens cannot contain an empty or null address.");
    }

    [Test]
    public async Task Association_Requires_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync();

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.AssociateTokenAsync(null!, fxToken.CreateReceipt!.Token);
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("account");
        await Assert.That(ane.Message).StartsWith("Account Address/Alias is missing. Please check that it is not null.");

        ex = await Assert.That(async () =>
        {
            await client.AssociateTokenAsync(EntityId.None, fxToken.CreateReceipt!.Token);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: InvalidAccountId");
    }

    [Test]
    public async Task Associating_With_Deleted_Account_Raises_Error()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync();

        await using var client = await TestNetwork.CreateClientAsync();
        await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fxAccount.PrivateKey
        });

        var ex = await Assert.That(async () =>
        {
            await client.AssociateTokensAsync(new AssociateTokenParams
            {
                Account = fxAccount.CreateReceipt!.Address,
                Tokens = [fxToken.CreateReceipt!.Token],
                Signatory = fxAccount.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountDeleted);
        await Assert.That(tex.Message).StartsWith("Associate Token with Account failed with status: AccountDeleted");
    }

    [Test]
    public async Task Associating_With_Duplicate_Account_Raises_Error()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync();

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            var tokens = new EntityId[] { fxToken.CreateReceipt!.Token, fxToken.CreateReceipt!.Token };
            await client.AssociateTokensAsync(new AssociateTokenParams
            {
                Account = fxAccount.CreateReceipt!.Address,
                Tokens = tokens,
                Signatory = fxAccount.PrivateKey
            });
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.TokenIdRepeatedInTokenList);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: TokenIdRepeatedInTokenList");
    }

    [Test]
    public async Task Associate_With_Associated_Token_Raises_Error()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken1 = await TestToken.CreateAsync(null, fxAccount);
        await using var fxToken2 = await TestToken.CreateAsync();

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            var tokens = new EntityId[] { fxToken1.CreateReceipt!.Token, fxToken2.CreateReceipt!.Token };
            await client.AssociateTokensAsync(new AssociateTokenParams
            {
                Account = fxAccount.CreateReceipt!.Address,
                Tokens = tokens,
                Signatory = fxAccount.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenAlreadyAssociatedToAccount);
        await Assert.That(tex.Message).StartsWith("Associate Token with Account failed with status: TokenAlreadyAssociatedToAccount");
    }

    [Test]
    public async Task Can_Associate_Token_With_Contract()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync();

        // Assert Not Associated
        var info = await fxContract.GetTokenBalancesAsync();
        await Assert.That(info.FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token)).IsNull();

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxContract.ContractReceipt!.Contract,
            Tokens = [fxToken.CreateReceipt!.Token],
            Signatory = fxContract.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        info = await fxContract.GetTokenBalancesAsync();
        var association = info.FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(association).IsNotNull();
        await Assert.That(association!.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    //[Test]
    //public async Task CanNotScheduleAssociateTokenWithAccount()
    //{
    //    await using var fxPayer = await TestAccount.CreateAsync();
    //    await using var fxAccount = await TestAccount.CreateAsync();
    //    await using var fxToken = await TestToken.CreateAsync();

    //    await AssertHg.TokenNotAssociatedAsync(fxToken, fxAccount);

    //    await using var client = await TestNetwork.CreateClientAsync();
    //    var ex = await Assert.That(async () =>
    //    {
    //        await client.AssociateTokensAsync(new AssociateTokenParams
    //        {
    //            Account = fxAccount.CreateReceipt!.Address,
    //            Tokens = [fxToken.CreateReceipt!.Token],
    //            Signatory = new Signatory(
    //                fxAccount.PrivateKey,
    //                new PendingParams
    //                {
    //                    PendingPayer = fxPayer
    //                })
    //        });
    //    }).ThrowsException();
    //    var tex = ex as TransactionException;
    //    await Assert.That(tex).IsNotNull();
    //    await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    //    await Assert.That(tex.Message).StartsWith("Scheduling Associate Token with Account failed with status: ScheduledTransactionNotInWhitelist");
    //}

    [Test]
    public async Task Can_Schedule_And_Sign_Associate_Token()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new AssociateTokenParams
                {
                    Account = fxAccount.CreateReceipt!.Address,
                    Tokens = new[] { fxToken.CreateReceipt!.Token },
                },
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<TransactionException>();
        await Assert.That(((TransactionException)tex!).Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    }
}
