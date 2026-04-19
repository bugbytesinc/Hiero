using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.Token;

public class DeleteTokenTests
{
    [Test]
    public async Task Can_Delete_Token()
    {
        await using var fx = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var record = await client.DeleteTokenAsync(new DeleteTokenParams
        {
            Token = fx.CreateReceipt!.Token,
            Signatory = fx.AdminPrivateKey
        });
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.Deleted).IsTrue();
    }

    [Test]
    public async Task Anyone_With_Admin_Key_Can_Delete_Token()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var record = await client.DeleteTokenAsync(new DeleteTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Signatory = fxToken.AdminPrivateKey
        }, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = fxAccount.PrivateKey;
        });
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Deleted).IsTrue();
    }

    [Test]
    public async Task Deleting_Does_Not_Remove_Token_Records()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var totalTinytokens = fxToken.CreateParams.Circulation;
        var xferAmount = totalTinytokens / 3;

        await client.TransferTokenAsync(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        var record = await client.DeleteTokenAsync(new DeleteTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Signatory = fxToken.AdminPrivateKey
        });
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxToken.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxToken.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxToken.CreateParams.SuspendEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxToken.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxToken.CreateParams.SupplyEndorsement);
        await Assert.That(info.MetadataEndorsement).IsEqualTo(fxToken.CreateParams.MetadataEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxToken.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsTrue();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var accountInfo = await client.GetAccountInfoAsync(fxAccount.CreateReceipt!.Address);
        var token = (await fxAccount.GetTokenBalancesAsync()).FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(token).IsNotNull();
        await Assert.That(token!.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(token.Balance).IsEqualTo((long)xferAmount);
        await Assert.That(token.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(token.AutoAssociated).IsFalse();
        await Assert.That(token.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);

        var treasuryInfo = await client.GetAccountInfoAsync(fxToken.TreasuryAccount.CreateReceipt!.Address);
        token = (await fxToken.TreasuryAccount.GetTokenBalancesAsync()).FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(token).IsNotNull();
        await Assert.That(token!.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(token.Balance).IsEqualTo((long)(totalTinytokens - xferAmount));
        await Assert.That(token.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(token.AutoAssociated).IsFalse();
        await Assert.That(token.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
    }

    [Test]
    public async Task Deleting_Token_Prevents_Token_Transfers()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var totalTinytokens = fxToken.CreateParams.Circulation;
        var xferAmount = totalTinytokens / 3;

        await client.TransferTokenAsync(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        var record = await client.DeleteTokenAsync(new DeleteTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Signatory = fxToken.AdminPrivateKey
        });
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var ex = await Assert.That(async () =>
        {
            await client.TransferTokenAsync(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, (long)xferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenWasDeleted);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: TokenWasDeleted");
    }

    [Test]
    public async Task Deleting_Token_Prevents_Token_Transfers_Amongst_Third_Parties()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        await using var client = await TestNetwork.CreateClientAsync();
        var totalTinytokens = fxToken.CreateParams.Circulation;
        var xferAmount = totalTinytokens / 3;

        await client.TransferTokenAsync(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, fxAccount1.CreateReceipt!.Address, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });
        await client.TransferTokenAsync(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, fxAccount2.CreateReceipt!.Address, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        var record = await client.DeleteTokenAsync(new DeleteTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Signatory = fxToken.AdminPrivateKey
        });
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var ex = await Assert.That(async () =>
        {
            await client.TransferTokenAsync(fxToken.CreateReceipt!.Token, fxAccount1.CreateReceipt!.Address, fxAccount2.CreateReceipt!.Address, (long)xferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenWasDeleted);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: TokenWasDeleted");
    }

    [Test]
    public async Task Calling_Delete_Without_Admin_Key_Raises_Error()
    {
        await using var fx = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.DeleteTokenAsync(fx.CreateReceipt!.Token);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Delete Token failed with status: InvalidSignature");

        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.Deleted).IsFalse();
    }

    [Test]
    public async Task Calling_Delete_On_An_Imutable_Token_Raises_An_Error()
    {
        await using var fx = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.Administrator = null;
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.DeleteTokenAsync(new DeleteTokenParams
            {
                Token = fx.CreateReceipt!.Token,
                Signatory = fx.AdminPrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsImmutable);
        await Assert.That(tex.Message).StartsWith("Delete Token failed with status: TokenIsImmutable");

        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.Deleted).IsFalse();
    }

    [Test]
    public async Task Can_Delete_Token_With_One_Of_Two_Mult_Sig()
    {
        var (pubAdminKey2, privateAdminKey2) = Generator.KeyPair();
        await using var fx = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.Administrator = new Endorsement(1, ctx.AdminPublicKey, pubAdminKey2);
            ctx.CreateParams.Signatory = new Signatory(ctx.CreateParams.Signatory!, privateAdminKey2);
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var record = await client.DeleteTokenAsync(new DeleteTokenParams
        {
            Token = fx.CreateReceipt!.Token,
            Signatory = fx.AdminPrivateKey
        });
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.Deleted).IsTrue();
    }

    [Test]
    public async Task Deleting_A_Deleted_Token_Raisees_Error()
    {
        await using var fx = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.DeleteTokenAsync(new DeleteTokenParams
        {
            Token = fx.CreateReceipt!.Token,
            Signatory = fx.AdminPrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.Deleted).IsTrue();

        var ex = await Assert.That(async () =>
        {
            await client.DeleteTokenAsync(new DeleteTokenParams
            {
                Token = fx.CreateReceipt!.Token,
                Signatory = fx.AdminPrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenWasDeleted);
        await Assert.That(tex.Message).StartsWith("Delete Token failed with status: TokenWasDeleted");
    }

    [Test]
    public async Task Calling_Delete_With_Invalid_ID_Raises_Error()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.DeleteTokenAsync(new DeleteTokenParams
            {
                Token = fxAccount.CreateReceipt!.Address,
                Signatory = fxAccount.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidTokenId);
        await Assert.That(tex.Message).StartsWith("Delete Token failed with status: InvalidTokenId");

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Deleted).IsFalse();
    }

    [Test]
    public async Task Calling_Delete_With_Missing_ID_Raises_Error()
    {
        await using var fx = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.DeleteTokenAsync(new DeleteTokenParams
            {
                Token = null!,
                Signatory = fx.AdminPrivateKey
            });
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("token");
        await Assert.That(ane.Message).StartsWith("Token is missing. Please check that it is not null");

        ex = await Assert.That(async () =>
        {
            await client.DeleteTokenAsync(new DeleteTokenParams
            {
                Token = EntityId.None,
                Signatory = fx.AdminPrivateKey
            });
        }).ThrowsException();
        ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("token");
        await Assert.That(ane.Message).StartsWith("Token is missing. Please check that it is not null");
    }

    [Test]
    public async Task Cannot_Delete_Treasury_While_Attached_To_Token()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxAccount2 = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxToken.CreateParams.Circulation;

        var ex = await Assert.That(async () =>
        {
            await client.DeleteAccountAsync(new DeleteAccountParams
            {
                Account = fxToken.TreasuryAccount,
                FundsReceiver = fxAccount1,
                Signatory = new Signatory(fxAccount1, fxToken.TreasuryAccount)
            }, ctx =>
            {
                ctx.Payer = fxAccount1;
                ctx.Signatory = new Signatory(fxAccount1, fxToken.TreasuryAccount);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountIsTreasury);
        await Assert.That(tex.Message).StartsWith("Delete Account failed with status: AccountIsTreasury");

        await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount2, (long)fxToken.CreateParams.Circulation, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        ex = await Assert.That(async () =>
        {
            await client.DeleteAccountAsync(new DeleteAccountParams
            {
                Account = fxToken.TreasuryAccount,
                FundsReceiver = fxAccount1,
                Signatory = new Signatory(fxAccount1, fxToken.TreasuryAccount)
            }, ctx =>
            {
                ctx.Payer = fxAccount1;
                ctx.Signatory = new Signatory(fxAccount1, fxToken.TreasuryAccount);
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountIsTreasury);
        await Assert.That(tex.Message).StartsWith("Delete Account failed with status: AccountIsTreasury");

        // Confirm Tokens still exist in account 2
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)fxToken.CreateParams.Circulation);

        // What does the info say,
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxToken.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxToken.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxToken.CreateParams.SuspendEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxToken.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxToken.CreateParams.SupplyEndorsement);
        await Assert.That(info.MetadataEndorsement).IsEqualTo(fxToken.CreateParams.MetadataEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxToken.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        // Move the Treasury, hmm...don't need treasury key?
        var receipt = await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxToken,
            Treasury = fxAccount1,
            Signatory = new Signatory(fxToken.AdminPrivateKey, fxAccount1.PrivateKey)
        });

        // Double check balances
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)fxToken.CreateParams.Circulation);

        // What does the info say now?
        info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxAccount1.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxToken.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxToken.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxToken.CreateParams.SuspendEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxToken.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxToken.CreateParams.SupplyEndorsement);
        await Assert.That(info.MetadataEndorsement).IsEqualTo(fxToken.CreateParams.MetadataEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxToken.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Delete_Treasury_After_Deleting_Token()
    {
        await using var fxBagHolder = await TestAccount.CreateAsync();
        await using var fx = await TestToken.CreateAsync(ctx => ctx.CreateParams.GrantKycEndorsement = null, fxBagHolder);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.TransferTokenAsync(fx.CreateReceipt!.Token, fx.TreasuryAccount, fxBagHolder, (long)fx.CreateParams.Circulation, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fx.TreasuryAccount.PrivateKey);
        });

        var record = await client.DeleteTokenAsync(new DeleteTokenParams
        {
            Token = fx.CreateReceipt!.Token,
            Signatory = fx.AdminPrivateKey
        });
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.Deleted).IsTrue();

        var receipt = await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = fx.TreasuryAccount.CreateReceipt!.Address,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fx.TreasuryAccount.PrivateKey
        });
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Delete_Token_Having_Holders()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(ctx => ctx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount1, 1, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });
        await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount2, 1, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        var record = await client.DeleteTokenAsync(new DeleteTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Signatory = fxToken.AdminPrivateKey
        });
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Deleted).IsTrue();

        await AssertHg.TokenBalanceAsync(fxToken, fxAccount1, 1);
        await AssertHg.TokenBalanceAsync(fxToken, fxAccount2, 1);
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Delete_Token()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new DeleteTokenParams
                {
                    Token = fxToken.CreateReceipt!.Token,
                },
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<TransactionException>();
        await Assert.That(((TransactionException)tex!).Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    }
}
