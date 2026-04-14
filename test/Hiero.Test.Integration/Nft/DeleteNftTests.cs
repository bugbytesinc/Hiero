using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.NftTokens;

public class DeleteNftTests
{
    [Test]
    public async Task Can_Delete_Nft()
    {
        await using var fx = await TestNft.CreateAsync(fx => fx.Metadata = null!);
        await using var client = await TestNetwork.CreateClientAsync();

        var record = await client.DeleteTokenAsync(fx.CreateReceipt!.Token, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fx.AdminPrivateKey);
        });
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.Deleted).IsTrue();
    }

    [Test]
    public async Task Anyone_With_Admin_Key_Can_Delete_Nft()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var record = await client.DeleteTokenAsync(fxNft.CreateReceipt!.Token, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = new Signatory(fxAccount.PrivateKey, fxNft.AdminPrivateKey);
        });
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Deleted).IsTrue();
    }

    [Test]
    public async Task Deleting_Does_Not_Remove_Nft_Records()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var initialCirculation = (ulong)fxNft.Metadata.Length;
        var xferAmount = 2ul;
        var expectedTreasury = initialCirculation - xferAmount;
        var serialNumbersTransfered = Enumerable.Range(1, 2).Select(i => (long)i);

        var transferParams = new TransferParams
        {
            NftTransfers = serialNumbersTransfered.Select(sn => new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, sn), fxNft.TreasuryAccount, fxAccount)),
            Signatory = fxNft.TreasuryAccount
        };

        await client.TransferAsync(transferParams);

        var record = await client.DeleteTokenAsync(fxNft.CreateReceipt!.Token, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.AdminPrivateKey);
        });
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(initialCirculation);
        await Assert.That(info.Decimals).IsEqualTo(0U);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(info.Deleted).IsTrue();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var asset = (await fxAccount.GetTokenBalancesAsync()).FirstOrDefault(t => t.Token == fxNft.CreateReceipt!.Token);
        await Assert.That(asset).IsNotNull();
        await Assert.That(asset!.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(asset.Balance).IsEqualTo((long)xferAmount);
        await Assert.That(asset.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(asset.AutoAssociated).IsFalse();
        await Assert.That(asset.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);

        asset = (await fxNft.TreasuryAccount.GetTokenBalancesAsync()).FirstOrDefault(t => t.Token == fxNft.CreateReceipt!.Token);
        await Assert.That(asset).IsNotNull();
        await Assert.That(asset!.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(asset.Balance).IsEqualTo((long)expectedTreasury);
        await Assert.That(asset.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(asset.AutoAssociated).IsFalse();
        await Assert.That(asset.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
    }

    [Test]
    public async Task Deleting_Nft_Prevents_Nft_Transfers()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var transferParams = new TransferParams
        {
            NftTransfers = new NftTransfer[] { new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, 1), fxNft.TreasuryAccount, fxAccount) },
            Signatory = fxNft.TreasuryAccount
        };

        await client.TransferAsync(transferParams);

        var record = await client.DeleteTokenAsync(fxNft.CreateReceipt!.Token, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.AdminPrivateKey);
        });
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        transferParams = new TransferParams
        {
            NftTransfers = new NftTransfer[] { new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, 2), fxNft.TreasuryAccount, fxAccount) },
            Signatory = fxNft.TreasuryAccount
        };

        var ex = await Assert.That(async () =>
        {
            await client.TransferAsync(transferParams);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenWasDeleted);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.TokenWasDeleted);
        await Assert.That(tex.Message).StartsWith("Transfer failed with status: TokenWasDeleted");
    }

    [Test]
    public async Task Deleting_Nft_Prevents_Nft_Transfers_Amongst_Third_Parties()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.TransferNftAsync(new Hiero.Nft(fxNft.CreateReceipt!.Token, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount1.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });
        await client.TransferNftAsync(new Hiero.Nft(fxNft.CreateReceipt!.Token, 2), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount2.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        var record = await client.DeleteTokenAsync(fxNft.CreateReceipt!.Token, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.AdminPrivateKey);
        });
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(new Hiero.Nft(fxNft.CreateReceipt!.Token, 1), fxAccount1.CreateReceipt!.Address, fxAccount2.CreateReceipt!.Address, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenWasDeleted);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.TokenWasDeleted);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: TokenWasDeleted");
    }

    [Test]
    public async Task Calling_Delete_Without_Admin_Key_Raises_Error()
    {
        await using var fx = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.DeleteTokenAsync(fx.CreateReceipt!.Token);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Delete Token failed with status: InvalidSignature");

        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.Deleted).IsFalse();
    }

    [Test]
    public async Task Calling_Delete_On_An_Imutable_Nft_Raises_An_Error()
    {
        await using var fx = await TestNft.CreateAsync(ctx =>
        {
            ctx.CreateParams.Administrator = null;
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.DeleteTokenAsync(fx.CreateReceipt!.Token, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fx.AdminPrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsImmutable);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.TokenIsImmutable);
        await Assert.That(tex.Message).StartsWith("Delete Token failed with status: TokenIsImmutable");

        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.Deleted).IsFalse();
    }

    [Test]
    public async Task Can_Delete_Nft_With_One_Of_Two_Mult_Sig()
    {
        var (pubAdminKey2, privateAdminKey2) = Generator.KeyPair();
        await using var fx = await TestNft.CreateAsync(ctx =>
        {
            ctx.CreateParams.Administrator = new Endorsement(1, ctx.AdminPublicKey, pubAdminKey2);
            ctx.CreateParams.Signatory = new Signatory(ctx.CreateParams.Signatory!, privateAdminKey2);
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var record = await client.DeleteTokenAsync(fx.CreateReceipt!.Token, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fx.AdminPrivateKey);
        });
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.Deleted).IsTrue();
    }

    [Test]
    public async Task Deleting_A_Deleted_Nft_Raises_Error()
    {
        await using var fx = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.DeleteTokenAsync(fx.CreateReceipt!.Token, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fx.AdminPrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.Deleted).IsTrue();

        var ex = await Assert.That(async () =>
        {
            await client.DeleteTokenAsync(fx.CreateReceipt!.Token, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fx.AdminPrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenWasDeleted);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.TokenWasDeleted);
        await Assert.That(tex.Message).StartsWith("Delete Token failed with status: TokenWasDeleted");
    }

    [Test]
    public async Task Calling_Delete_With_Invalid_ID_Raises_Error()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.DeleteTokenAsync(fxAccount.CreateReceipt!.Address, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidTokenId);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.InvalidTokenId);
        await Assert.That(tex.Message).StartsWith("Delete Token failed with status: InvalidTokenId");

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Deleted).IsFalse();
    }

    [Test]
    public async Task Calling_Delete_With_Missing_ID_Raises_Error()
    {
        await using var fx = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.DeleteTokenAsync((EntityId)null!);
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("token");
        await Assert.That(ane.Message).StartsWith("Token is missing. Please check that it is not null");

        ex = await Assert.That(async () =>
        {
            await client.DeleteTokenAsync(EntityId.None);
        }).ThrowsException();
        ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("token");
        await Assert.That(ane.Message).StartsWith("Token is missing. Please check that it is not null");
    }

    [Test]
    public async Task Cannot_Delete_Treasury_While_Attached_To_Nft()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxAccount2 = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = (ulong)fxNft.Metadata.Length;

        var ex = await Assert.That(async () =>
        {
            await client.DeleteAccountAsync(new DeleteAccountParams
            {
                Account = fxNft.TreasuryAccount,
                FundsReceiver = fxAccount1,
                Signatory = new Signatory(fxAccount1.PrivateKey, fxNft.TreasuryAccount.PrivateKey)
            }, ctx =>
            {
                ctx.Payer = fxAccount1;
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountIsTreasury);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.AccountIsTreasury);
        await Assert.That(tex.Message).StartsWith("Delete Account failed with status: AccountIsTreasury");

        var serialNumbersTransfered = Enumerable.Range(1, (int)circulation).Select(i => (long)i);

        var transferParams = new TransferParams
        {
            NftTransfers = serialNumbersTransfered.Select(sn => new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, sn), fxNft.TreasuryAccount, fxAccount2)),
            Signatory = fxNft.TreasuryAccount
        };

        await client.TransferAsync(transferParams);

        ex = await Assert.That(async () =>
        {
            await client.DeleteAccountAsync(new DeleteAccountParams
            {
                Account = fxNft.TreasuryAccount,
                FundsReceiver = fxAccount1,
                Signatory = new Signatory(fxAccount1.PrivateKey, fxNft.TreasuryAccount.PrivateKey)
            }, ctx =>
            {
                ctx.Payer = fxAccount1;
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountIsTreasury);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.AccountIsTreasury);
        await Assert.That(tex.Message).StartsWith("Delete Account failed with status: AccountIsTreasury");

        // Confirm Nfts still exist in account 2
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)circulation);

        // What does the info say,
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(circulation);
        await Assert.That(info.Decimals).IsEqualTo(0U);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        // Move the Treasury, hmm...don't need treasury key?
        await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxNft,
            Treasury = fxAccount1,
            Signatory = new Signatory(fxNft.AdminPrivateKey, fxAccount1.PrivateKey)
        });

        // Double check balances
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)circulation);

        // What does the info say now?
        info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxAccount1.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(circulation);
        await Assert.That(info.Decimals).IsEqualTo(0U);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Delete_Treasury_After_Deleting_Nft()
    {
        await using var fxBagHolder = await TestAccount.CreateAsync();
        await using var fx = await TestNft.CreateAsync(ctx => ctx.CreateParams.GrantKycEndorsement = null, fxBagHolder);
        await using var client = await TestNetwork.CreateClientAsync();

        var xfers = fx.MintReceipt!.SerialNumbers.Select(s => new NftTransfer(new Hiero.Nft(fx.CreateReceipt!.Token, s), fx.TreasuryAccount, fxBagHolder)).ToArray();

        await client.TransferAsync(new TransferParams
        {
            NftTransfers = xfers,
            Signatory = fx.TreasuryAccount
        });

        var record = await client.DeleteTokenAsync(fx.CreateReceipt!.Token, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fx.AdminPrivateKey);
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
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Not_Schedule_A_Delete_Nft()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new DeleteTokenParams
            {
                Token = fxNft,
                Signatory = fxNft.AdminPrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
        await Assert.That(tex.Message).StartsWith("Scheduling Delete Token failed with status: ScheduledTransactionNotInWhitelist");
    }
}
