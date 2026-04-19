using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.NftTokens;

public class CreateNftTests
{
    [Test]
    public async Task Can_Create_Nft_Definition()
    {
        await using var fxNft = await TestNft.CreateAsync(fx => fx.Metadata = null!);
        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxNft.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(0UL);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxNft.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var treasury = await client.GetAccountInfoAsync(fxNft.TreasuryAccount.CreateReceipt.Address);
        await Assert.That(treasury.Balance).IsEqualTo(fxNft.TreasuryAccount.CreateParams.InitialBalance);
        await Assert.That(treasury.NftCount).IsEqualTo(0);

        var tokens = await fxNft.TreasuryAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).HasSingleItem();
        var tokInfo = tokens[0];
        await Assert.That(tokInfo.Token).IsEqualTo(fxNft.CreateReceipt.Token);
        await Assert.That(tokInfo.Balance).IsEqualTo(0);
        await Assert.That(tokInfo.KycStatus).IsEqualTo(TokenKycStatus.Granted);
        await Assert.That(tokInfo.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(tokInfo.AutoAssociated).IsFalse();
        await Assert.That(info.Royalties).IsEmpty();

        var record = (CreateTokenRecord)await client.GetTransactionRecordAsync(fxNft.CreateReceipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsNotEmpty();
        await Assert.That(record.Fee >= 0UL).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.TokenTransfers).IsEmpty();
        await Assert.That(record.NftTransfers).IsEmpty();
        await Assert.That(record.Royalties).IsEmpty();
        AssertHg.SingleAssociation(fxNft, fxNft.TreasuryAccount, record.Associations);
    }

    [Test]
    public async Task Can_Create_Nft_Definition_With_Alias_Treasury_Defect()
    {
        // Defect 0.21.0: Associating an NFT with an account using its alias address has not yet been
        // implemented by the network, although it will accept the transaction.
        await using var fxTreasury = await TestAliasAccount.CreateAsync();
        var ex = await Assert.That(async () =>
        {
            await using var fxNft = await TestNft.CreateAsync(fx =>
            {
                fx.Metadata = null!;
                fx.CreateParams.Treasury = fxTreasury.Alias;
                fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.RenewAccount.PrivateKey, fxTreasury.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Create NFT failed with status: InvalidAccountId");
    }

    [Test]
    public async Task Can_Create_Nft_Definition_With_Fixed_Royalty()
    {
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.Metadata = null!;
            fx.CreateParams.Royalties =
            [
                new FixedRoyalty(fx.TreasuryAccount, EntityId.None, 1)
            ];
        });
        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxNft.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(0UL);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxNft.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).HasSingleItem();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var royalty = info.Royalties[0] as FixedRoyalty;
        await Assert.That(royalty).IsNotNull();
        await Assert.That(royalty!.Receiver).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt.Address);
        await Assert.That(royalty.Token).IsEqualTo(EntityId.None);
        await Assert.That(royalty.Amount).IsEqualTo(1);
    }

    [Test]
    public async Task Can_Create_A_Nft_With_Receipt()
    {
        await using var fxTreasury = await TestAccount.CreateAsync();
        await using var fxRenew = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var createParams = new CreateNftParams
        {
            Name = Generator.Code(50),
            Symbol = Generator.Code(100),
            Ceiling = (long)(Generator.Integer(10, 20) * 100000),
            Treasury = fxTreasury.CreateReceipt!.Address,
            Administrator = fxTreasury.PublicKey,
            GrantKycEndorsement = fxTreasury.PublicKey,
            SuspendEndorsement = fxTreasury.PublicKey,
            PauseEndorsement = fxTreasury.PublicKey,
            ConfiscateEndorsement = fxTreasury.PublicKey,
            SupplyEndorsement = fxTreasury.PublicKey,
            InitializeSuspended = false,
            Expiration = Generator.TruncatedFutureDate(2000, 3000),
            RenewAccount = fxRenew.CreateReceipt!.Address,
            RenewPeriod = TimeSpan.FromDays(90),
            Signatory = new Signatory(fxTreasury.PrivateKey, fxRenew.PrivateKey),
            Memo = Generator.Memo(20)
        };
        var receipt = await client.CreateNftAsync(createParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(receipt.Token);
        await Assert.That(info.Token).IsEqualTo(receipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(createParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(createParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxTreasury.CreateReceipt.Address);
        await Assert.That(info.Circulation).IsEqualTo(0UL);
        await Assert.That(info.Decimals).IsEqualTo(0U);
        await Assert.That(info.Administrator).IsEqualTo(createParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(createParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(createParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(createParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(createParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(createParams.SupplyEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(createParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(createParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Associate_Nft_With_Alias_Account_Defect()
    {
        // Defect 0.21.0: Creating an NFT with a treasury identified by its alias address has not yet been
        // implemented by the network, although it will accept the transaction.
        await using var fxTreasury = await TestAliasAccount.CreateAsync();
        await using var fxRenew = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var createParams = new CreateNftParams
        {
            Name = Generator.Code(50),
            Symbol = Generator.Code(100),
            Ceiling = (long)(Generator.Integer(10, 20) * 100000),
            Treasury = fxTreasury.Alias,
            Administrator = fxTreasury.PublicKey,
            GrantKycEndorsement = fxTreasury.PublicKey,
            SuspendEndorsement = fxTreasury.PublicKey,
            PauseEndorsement = fxTreasury.PublicKey,
            ConfiscateEndorsement = fxTreasury.PublicKey,
            SupplyEndorsement = fxTreasury.PublicKey,
            InitializeSuspended = false,
            Expiration = Generator.TruncatedFutureDate(2000, 3000),
            RenewAccount = fxRenew.CreateReceipt!.Address,
            RenewPeriod = TimeSpan.FromDays(90),
            Signatory = new Signatory(fxTreasury.PrivateKey, fxRenew.PrivateKey),
            Memo = Generator.Memo(20)
        };
        var ex = await Assert.That(async () =>
        {
            await client.CreateNftAsync(createParams);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Create NFT failed with status: InvalidAccountId");
    }

    [Test]
    public async Task Missing_Treasury_Address_Raises_Error()
    {
        var ex = await Assert.That(async () =>
        {
            await TestNft.CreateAsync(ctx =>
            {
                ctx.CreateParams.Treasury = null!;
            });
        }).ThrowsException();
        var aoe = ex as ArgumentOutOfRangeException;
        await Assert.That(aoe).IsNotNull();
        await Assert.That(aoe!.ParamName).IsEqualTo("Treasury");
        await Assert.That(aoe.Message).StartsWith("The treasury must be specified.");

        ex = await Assert.That(async () =>
        {
            await using var fx = await TestNft.CreateAsync(ctx =>
            {
                ctx.CreateParams.Treasury = EntityId.None;
            });
        }).ThrowsException();
        aoe = ex as ArgumentOutOfRangeException;
        await Assert.That(aoe).IsNotNull();
        await Assert.That(aoe!.ParamName).IsEqualTo("Treasury");
        await Assert.That(aoe.Message).StartsWith("The treasury must be specified.");
    }

    [Test]
    public async Task File_Address_As_Treasury_Raises_Error()
    {
        var fxFile = await TestFile.CreateAsync();
        var ex = await Assert.That(async () =>
        {
            await TestNft.CreateAsync(fx =>
            {
                fx.CreateParams.Treasury = fxFile.CreateReceipt!.File;
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Create NFT failed with status: InvalidAccountId");
    }

    [Test]
    public async Task Can_Set_Treasury_To_Node_Contract_Account()
    {
        var fxContract = await GreetingContract.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Treasury = fxContract.ContractReceipt!.Contract;
            fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.RenewAccount.PrivateKey, fxContract.PrivateKey);
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Treasury).IsEqualTo(fxContract.ContractReceipt!.Contract);

        await Assert.That(await fxContract.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length);
    }

    [Test]
    public async Task Null_Administrator_Key_Is_Allowed()
    {
        await using var fxNft = await TestNft.CreateAsync(ctx =>
        {
            ctx.CreateParams.Administrator = null;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Administrator).IsNull();
    }

    [Test]
    public async Task Empty_Key_Administrator_Key_Is_Allowed()
    {
        await using var fxNft = await TestNft.CreateAsync(ctx =>
        {
            ctx.CreateParams.Administrator = Endorsement.None;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Administrator).IsNull();
    }

    [Test]
    public async Task Null_Grant_Kyc_Key_Is_Allowed()
    {
        await using var fxNft = await TestNft.CreateAsync(ctx =>
        {
            ctx.CreateParams.GrantKycEndorsement = null;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.GrantKycEndorsement).IsNull();
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
    }

    [Test]
    public async Task Empty_Key_Grant_Kyc_Key_Is_Allowed()
    {
        await using var fxNft = await TestNft.CreateAsync(ctx =>
        {
            ctx.CreateParams.GrantKycEndorsement = Endorsement.None;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.GrantKycEndorsement).IsNull();
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
    }

    [Test]
    public async Task Null_Suspend_Key_Is_Allowed()
    {
        await using var fxNft = await TestNft.CreateAsync(ctx =>
        {
            ctx.CreateParams.SuspendEndorsement = null;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.SuspendEndorsement).IsNull();
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.NotApplicable);
    }

    [Test]
    public async Task Empty_Key_Suspend_Key_Is_Allowed()
    {
        await using var fxNft = await TestNft.CreateAsync(ctx =>
        {
            ctx.CreateParams.SuspendEndorsement = Endorsement.None;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.SuspendEndorsement).IsNull();
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.NotApplicable);
    }

    [Test]
    public async Task Null_Confiscate_Key_Is_Allowed()
    {
        await using var fxNft = await TestNft.CreateAsync(ctx =>
        {
            ctx.CreateParams.ConfiscateEndorsement = null;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.ConfiscateEndorsement).IsNull();
    }

    [Test]
    public async Task Empty_Confiscate_Key_Is_Allowed()
    {
        await using var fxNft = await TestNft.CreateAsync(ctx =>
        {
            ctx.CreateParams.ConfiscateEndorsement = Endorsement.None;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.ConfiscateEndorsement).IsNull();
    }

    [Test]
    public async Task Null_Supply_Key_Is_Not_Allowed()
    {
        var ex = await Assert.That(async () =>
        {
            await TestNft.CreateAsync(ctx =>
            {
                ctx.CreateParams.SupplyEndorsement = null;
                ctx.Metadata = null!;
            });
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.TokenHasNoSupplyKey);
    }

    [Test]
    public async Task Empty_Supply_Key_Is_Not_Allowed()
    {
        var ex = await Assert.That(async () =>
        {
            await TestNft.CreateAsync(ctx =>
            {
                ctx.CreateParams.SupplyEndorsement = Endorsement.None;
                ctx.Metadata = null!;
            });
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.TokenHasNoSupplyKey);
    }

    [Test]
    public async Task Null_Royalty_Key_Is_Allowed()
    {
        await using var fxNft = await TestNft.CreateAsync(ctx =>
        {
            ctx.CreateParams.RoyaltiesEndorsement = null;
            ctx.Metadata = null!;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.RoyaltiesEndorsement).IsNull();
    }

    [Test]
    public async Task Empty_Royalty_Key_Is_Allowed()
    {
        await using var fxNft = await TestNft.CreateAsync(ctx =>
        {
            ctx.CreateParams.RoyaltiesEndorsement = Endorsement.None;
            ctx.Metadata = null!;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.RoyaltiesEndorsement).IsNull();
    }

    [Test]
    public async Task Null_Symbol_Is_Not_Allowed()
    {
        var ex = await Assert.That(async () =>
        {
            await using var fx = await TestNft.CreateAsync(ctx =>
            {
                ctx.CreateParams.Symbol = null!;
            });
        }).ThrowsException();
        var aoe = ex as ArgumentOutOfRangeException;
        await Assert.That(aoe).IsNotNull();
        await Assert.That(aoe!.ParamName).IsEqualTo("Symbol");
        await Assert.That(aoe.Message).StartsWith("The token symbol must be specified. (Parameter 'Symbol')");
    }

    [Test]
    public async Task Empty_Symbol_Is_Not_Allowed()
    {
        var ex = await Assert.That(async () =>
        {
            await using var fx = await TestNft.CreateAsync(ctx =>
            {
                ctx.CreateParams.Symbol = string.Empty;
            });
        }).ThrowsException();
        var aoe = ex as ArgumentOutOfRangeException;
        await Assert.That(aoe).IsNotNull();
        await Assert.That(aoe!.ParamName).IsEqualTo("Symbol");
        await Assert.That(aoe.Message).StartsWith("The token symbol must be specified. (Parameter 'Symbol')");
    }

    [Test]
    public async Task Symbol_Does_Allow_Numbers()
    {
        await using var fxNft = await TestNft.CreateAsync(ctx =>
        {
            ctx.CreateParams.Symbol = "123" + Generator.Code(20) + "456";
        });
        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxNft.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0U);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxNft.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var treasury = await client.GetAccountInfoAsync(fxNft.TreasuryAccount.CreateReceipt.Address);
        await Assert.That(treasury.Balance).IsEqualTo(fxNft.TreasuryAccount.CreateParams.InitialBalance);

        var tokens = await fxNft.TreasuryAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).HasSingleItem();
        var nftInfo = tokens[0];
        await Assert.That(nftInfo.Token).IsEqualTo(fxNft.CreateReceipt.Token);
        await Assert.That(nftInfo.Balance).IsEqualTo(fxNft.Metadata.Length);
        await Assert.That(nftInfo.KycStatus).IsEqualTo(TokenKycStatus.Granted);
        await Assert.That(nftInfo.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(nftInfo.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Task_Duplicate_Symbols_Are_Allowed()
    {
        await using var fxTaken = await TestNft.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Symbol = fxTaken.CreateParams.Symbol;
        });

        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxNft.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0U);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxNft.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var treasury = await client.GetAccountInfoAsync(fxNft.TreasuryAccount.CreateReceipt.Address);
        await Assert.That(treasury.Balance).IsEqualTo(fxNft.TreasuryAccount.CreateParams.InitialBalance);

        var tokens = await fxNft.TreasuryAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).HasSingleItem();
        var nftInfo = tokens[0];
        await Assert.That(nftInfo.Token).IsEqualTo(fxNft.CreateReceipt.Token);
        await Assert.That(nftInfo.Balance).IsEqualTo(fxNft.Metadata.Length);
        await Assert.That(nftInfo.KycStatus).IsEqualTo(TokenKycStatus.Granted);
        await Assert.That(nftInfo.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(nftInfo.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Null_Name_Is_Not_Allowed()
    {
        var ex = await Assert.That(async () =>
        {
            await using var fx = await TestNft.CreateAsync(ctx =>
            {
                ctx.CreateParams.Name = null!;
            });
        }).ThrowsException();
        var aoe = ex as ArgumentOutOfRangeException;
        await Assert.That(aoe).IsNotNull();
        await Assert.That(aoe!.ParamName).IsEqualTo("Name");
        await Assert.That(aoe.Message).StartsWith("The name cannot be null or empty. (Parameter 'Name')");
    }

    [Test]
    public async Task Empty_Name_Is_Not_Allowed()
    {
        var ex = await Assert.That(async () =>
        {
            await using var fx = await TestNft.CreateAsync(ctx =>
            {
                ctx.CreateParams.Name = string.Empty;
            });
        }).ThrowsException();
        var aoe = ex as ArgumentOutOfRangeException;
        await Assert.That(aoe).IsNotNull();
        await Assert.That(aoe!.ParamName).IsEqualTo("Name");
        await Assert.That(aoe.Message).StartsWith("The name cannot be null or empty. (Parameter 'Name')");
    }

    [Test]
    public async Task Name_Does_Allow_Numbers_And_Spaces()
    {
        await using var fx = await TestNft.CreateAsync(ctx =>
        {
            ctx.CreateParams.Name = Generator.Code(20) + " 123\r\n\t?";
        });
        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.Name).IsEqualTo(fx.CreateParams.Name);
    }

    [Test]
    public async Task Task_Duplicate_Names_Are_Allowed()
    {
        await using var fxTaken = await TestNft.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Name = fxTaken.CreateParams.Name;
        });

        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxNft.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0U);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxNft.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var treasury = await client.GetAccountInfoAsync(fxNft.TreasuryAccount.CreateReceipt.Address);
        await Assert.That(treasury.Balance).IsEqualTo(fxNft.TreasuryAccount.CreateParams.InitialBalance);

        var tokens = await fxNft.TreasuryAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).HasSingleItem();
        var nftInfo = tokens[0];
        await Assert.That(nftInfo.Token).IsEqualTo(fxNft.CreateReceipt.Token);
        await Assert.That(nftInfo.Balance).IsEqualTo(fxNft.Metadata.Length);
        await Assert.That(nftInfo.KycStatus).IsEqualTo(TokenKycStatus.Granted);
        await Assert.That(nftInfo.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(nftInfo.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Initialize_Supended_Can_Be_False()
    {
        await using var fx = await TestNft.CreateAsync(ctx =>
        {
            ctx.CreateParams.InitializeSuspended = true;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Suspended);
    }

    [Test]
    public async Task Initialize_Is_False_By_Default()
    {
        await using var fx = await TestNft.CreateAsync();

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
    }

    [Test]
    public async Task Auto_Renew_Account_Is_Not_Required()
    {
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.RenewAccount = null;
            fx.CreateParams.RenewPeriod = null;
            fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.TreasuryAccount.PrivateKey);
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.RenewAccount).IsNull();
    }

    [Test]
    public async Task Missing_Admin_Signature_Raises_Error()
    {
        var ex = await Assert.That(async () =>
        {
            await TestNft.CreateAsync(fx =>
            {
                fx.CreateParams.Signatory = new Signatory(fx.GrantPrivateKey, fx.SuspendPrivateKey, fx.ConfiscatePrivateKey, fx.SupplyPrivateKey, fx.RenewAccount.PrivateKey, fx.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Create NFT failed with status: InvalidSignature");
    }

    [Test]
    public async Task Missing_Grant_Admin_Signature_Is_Allowed()
    {
        await using var fx = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.SuspendPrivateKey, fx.ConfiscatePrivateKey, fx.SupplyPrivateKey, fx.RenewAccount.PrivateKey, fx.TreasuryAccount.PrivateKey);
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.GrantKycEndorsement).IsNotNull();
    }

    [Test]
    public async Task Missing_Suspend_Admin_Signature_Is_Allowed()
    {
        await using var fx = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.GrantPrivateKey, fx.ConfiscatePrivateKey, fx.SupplyPrivateKey, fx.RenewAccount.PrivateKey, fx.TreasuryAccount.PrivateKey);
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.SuspendEndorsement).IsNotNull();
    }

    [Test]
    public async Task Missing_Confiscate_Admin_Signature_Is_Allowed()
    {
        await using var fx = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.GrantPrivateKey, fx.SuspendPrivateKey, fx.SupplyPrivateKey, fx.RenewAccount.PrivateKey, fx.TreasuryAccount.PrivateKey);
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.ConfiscateEndorsement).IsNotNull();
    }

    [Test]
    public async Task Missing_Supply_Admin_Signature_Is_Allowed()
    {
        await using var fx = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.GrantPrivateKey, fx.SuspendPrivateKey, fx.ConfiscatePrivateKey, fx.RenewAccount.PrivateKey, fx.TreasuryAccount.PrivateKey);
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.SupplyEndorsement).IsNotNull();
    }

    [Test]
    public async Task Null_Memo_Is_Allowed()
    {
        await using var fx = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Memo = null!;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.Memo).IsEmpty();
    }

    [Test]
    public async Task Empty_Memo_Is_Allowed()
    {
        await using var fx = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Memo = string.Empty;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.Memo).IsEmpty();
    }

    [Test]
    public async Task Create_Nft_Missing_Renew_Account_Signature_Raises_Error()
    {
        var ex = await Assert.That(async () =>
        {
            await TestNft.CreateAsync(fx =>
            {
                fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.GrantPrivateKey, fx.SuspendPrivateKey, fx.ConfiscatePrivateKey, fx.SupplyPrivateKey, fx.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Create NFT failed with status: InvalidSignature");
    }

    [Test]
    public async Task Expiration_Time_In_Past_Raises_Error()
    {
        var ex = await Assert.That(async () =>
        {
            await TestNft.CreateAsync(fx =>
            {
                fx.CreateParams.Expiration = DateTime.UtcNow.AddDays(-5);
            });
        }).ThrowsException();
        var aoe = ex as ArgumentOutOfRangeException;
        await Assert.That(aoe).IsNotNull();
        await Assert.That(aoe!.ParamName).IsEqualTo("Expiration");
        await Assert.That(aoe.Message).StartsWith("The expiration time must be in the future.");
    }

    [Test]
    public async Task Only_Admin_Treasury_And_Renew_Account_Keys_Are_Requied()
    {
        await using var fx = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.RenewAccount.PrivateKey, fx.TreasuryAccount.PrivateKey);
        });
        await Assert.That(fx.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Create_With_Re_Used_Symbol_From_Deleted_Nft()
    {
        await using var fxTaken = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await client.DeleteTokenAsync(new DeleteTokenParams { Token = fxTaken.CreateReceipt!.Token, Signatory = fxTaken.AdminPrivateKey });
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Symbol = fxTaken.CreateParams.Symbol;
        });

        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxNft.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0U);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxNft.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var treasury = await client.GetAccountInfoAsync(fxNft.TreasuryAccount.CreateReceipt.Address);
        await Assert.That(treasury.Balance).IsEqualTo(fxNft.TreasuryAccount.CreateParams.InitialBalance);

        var tokens = await fxNft.TreasuryAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).HasSingleItem();
        var nftInfo = tokens[0];
        await Assert.That(nftInfo.Token).IsEqualTo(fxNft.CreateReceipt.Token);
        await Assert.That(nftInfo.Balance).IsEqualTo(fxNft.Metadata.Length);
        await Assert.That(nftInfo.KycStatus).IsEqualTo(TokenKycStatus.Granted);
        await Assert.That(nftInfo.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(nftInfo.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Create_With_Re_Used_Name_From_Deleted_Nft()
    {
        await using var fxTaken = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await client.DeleteTokenAsync(new DeleteTokenParams { Token = fxTaken.CreateReceipt!.Token, Signatory = fxTaken.AdminPrivateKey });
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Name = fxTaken.CreateParams.Name;
        });

        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxNft.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0U);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxNft.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var treasury = await client.GetAccountInfoAsync(fxNft.TreasuryAccount.CreateReceipt.Address);
        await Assert.That(treasury.Balance).IsEqualTo(fxNft.TreasuryAccount.CreateParams.InitialBalance);

        var tokens = await fxNft.TreasuryAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).HasSingleItem();
        var nftInfo = tokens[0];
        await Assert.That(nftInfo.Token).IsEqualTo(fxNft.CreateReceipt.Token);
        await Assert.That(nftInfo.Balance).IsEqualTo(fxNft.Metadata.Length);
        await Assert.That(nftInfo.KycStatus).IsEqualTo(TokenKycStatus.Granted);
        await Assert.That(nftInfo.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(nftInfo.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Create_With_Contract_As_Treasury()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Treasury = fxContract.ContractReceipt!.Contract;
            fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fxContract.PrivateKey, fx.RenewAccount.PrivateKey);
        });
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxContract.ContractReceipt!.Contract);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0U);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxNft.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        await Assert.That(await fxContract.GetCryptoBalanceAsync()).IsEqualTo((ulong)fxContract.ContractParams.InitialBalance);
        await Assert.That(await fxContract.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxContract.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length);
    }

    [Test]
    public async Task Can_Create_Without_Renewal_Information()
    {
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.RenewAccount = null;
            fx.CreateParams.RenewPeriod = default;
        });
        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxNft.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0U);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxNft.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.RenewPeriod).IsNull();
        await Assert.That(info.RenewAccount).IsNull();
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();

        await Assert.That(await fxNft.TreasuryAccount.GetCryptoBalanceAsync()).IsEqualTo(fxNft.TreasuryAccount.CreateParams.InitialBalance);
        var tokens = await fxNft.TreasuryAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).HasSingleItem();
        var nftInfo = tokens[0];
        await Assert.That(nftInfo.Token).IsEqualTo(fxNft.CreateReceipt.Token);
        await Assert.That(nftInfo.Balance).IsEqualTo(fxNft.Metadata.Length);
        await Assert.That(nftInfo.KycStatus).IsEqualTo(TokenKycStatus.Granted);
        await Assert.That(nftInfo.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(nftInfo.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Schedule_A_Create_Nft()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        var fxTemplate = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var scheduledReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = fxTemplate.CreateParams,
            Payer = fxPayer
        });
        await Assert.That(scheduledReceipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(scheduledReceipt).IsNotNull();
        var transactionReceipt = await client.SignScheduleAsync(scheduledReceipt.Schedule, ctx =>
        {
            ctx.Payer = fxPayer;
            ctx.Signatory = fxPayer;
        });
        var pendingReceipt = await client.GetReceiptAsync(scheduledReceipt.ScheduledTransactionId);
        await Assert.That(pendingReceipt.Status).IsEqualTo(ResponseCode.Success);

        var createReceipt = pendingReceipt as CreateTokenReceipt;
        await Assert.That(createReceipt).IsNotNull();

        var info = await client.GetTokenInfoAsync(createReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(createReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxTemplate.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxTemplate.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxTemplate.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Ceiling).IsEqualTo(fxTemplate.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxTemplate.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxTemplate.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxTemplate.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxTemplate.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxTemplate.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxTemplate.CreateParams.SupplyEndorsement);
        await Assert.That(info.MetadataEndorsement).IsEqualTo(fxTemplate.CreateParams.MetadataEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxTemplate.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxTemplate.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Create_Nft()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxTreasury = await TestAccount.CreateAsync();
        var (adminPublicKey, adminPrivateKey) = Generator.KeyPair();
        var (supplyPublicKey, _) = Generator.KeyPair();
        await using var client = await TestNetwork.CreateClientAsync();
        var createParams = new CreateNftParams
        {
            Name = Generator.Code(50),
            Symbol = Generator.Code(100),
            Treasury = fxTreasury.CreateReceipt!.Address,
            Administrator = adminPublicKey,
            SupplyEndorsement = supplyPublicKey,
            Expiration = Generator.TruncatedFutureDate(800, 1400),
            Signatory = new Signatory(adminPrivateKey, fxTreasury.PrivateKey),
            Memo = Generator.Code(20)
        };
        var scheduledReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = createParams,
            Payer = fxPayer
        });
        await Assert.That(scheduledReceipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(scheduledReceipt.Schedule).IsNotEqualTo(EntityId.None);

        var signReceipt = await client.SignScheduleAsync(scheduledReceipt.Schedule, ctx =>
        {
            ctx.Payer = fxPayer;
            ctx.Signatory = fxPayer.PrivateKey;
        });
        await Assert.That(signReceipt.Status).IsEqualTo(ResponseCode.Success);

        var pendingReceipt = await client.GetReceiptAsync(scheduledReceipt.ScheduledTransactionId) as CreateTokenReceipt;
        await Assert.That(pendingReceipt).IsNotNull();
        await Assert.That(pendingReceipt!.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(pendingReceipt.Token.AccountNum > 0).IsTrue();
    }
}
