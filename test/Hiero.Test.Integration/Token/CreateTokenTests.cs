using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.Token;

public class CreateTokenTests
{
    [Test]
    public async Task Can_Create_A_Token()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxToken.CreateReceipt.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxToken.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxToken.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxToken.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxToken.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxToken.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxToken.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxToken.CreateParams.SupplyEndorsement);
        await Assert.That(info.MetadataEndorsement).IsEqualTo(fxToken.CreateParams.MetadataEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxToken.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var treasury = await client.GetAccountInfoAsync(fxToken.TreasuryAccount.CreateReceipt.Address);
        await Assert.That(treasury.Balance).IsEqualTo(fxToken.TreasuryAccount.CreateParams.InitialBalance);

        var tokens = await fxToken.TreasuryAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).HasSingleItem();

        var token = tokens[0];
        await Assert.That(token.Token).IsEqualTo(fxToken.CreateReceipt.Token);
        await Assert.That(token.Balance).IsEqualTo((long)fxToken.CreateParams.Circulation);
        await Assert.That(token.KycStatus).IsEqualTo(TokenKycStatus.Granted);
        await Assert.That(token.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(token.AutoAssociated).IsFalse();

        var record = (CreateTokenRecord)await client.GetTransactionRecordAsync(fxToken.CreateReceipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsNotEmpty();
        await Assert.That(record.Fee >= 0UL).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.TokenTransfers).HasSingleItem();
        await Assert.That(record.NftTransfers).IsEmpty();
        AssertHg.SingleAssociation(fxToken, fxToken.TreasuryAccount, record.Associations);
    }

    [Test]
    public async Task Can_Create_A_Token_With_Receipt()
    {
        await using var fxTreasury = await TestAccount.CreateAsync();
        await using var fxRenew = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var createParams = new CreateTokenParams
        {
            Name = Generator.Code(50),
            Symbol = Generator.Code(20),
            Circulation = (ulong)(Generator.Integer(10, 20) * 100000),
            Decimals = (uint)Generator.Integer(2, 5),
            Treasury = fxTreasury.CreateReceipt!.Address,
            Administrator = fxTreasury.PublicKey,
            GrantKycEndorsement = fxTreasury.PublicKey,
            SuspendEndorsement = fxTreasury.PublicKey,
            PauseEndorsement = fxTreasury.PublicKey,
            ConfiscateEndorsement = fxTreasury.PublicKey,
            SupplyEndorsement = fxTreasury.PublicKey,
            MetadataEndorsement = fxTreasury.PublicKey,
            RoyaltiesEndorsement = fxTreasury.PublicKey,
            InitializeSuspended = false,
            Expiration = Generator.TruncatedFutureDate(2000, 3000),
            RenewAccount = fxRenew.CreateReceipt!.Address,
            RenewPeriod = TimeSpan.FromDays(90),
            Signatory = new Signatory(fxTreasury.PrivateKey, fxRenew.PrivateKey),
            Memo = Generator.Memo(20)
        };
        var receipt = await client.CreateTokenAsync(createParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(receipt.Token);
        await Assert.That(info.Token).IsEqualTo(receipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(createParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(createParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxTreasury.CreateReceipt.Address);
        await Assert.That(info.Circulation).IsEqualTo(createParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(createParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(createParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(createParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(createParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(createParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(createParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(createParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(createParams.SupplyEndorsement);
        await Assert.That(info.MetadataEndorsement).IsEqualTo(createParams.SupplyEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(createParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(createParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Create_With_Alias_Treasury_Is_Unsupported()
    {
        await using var fxTreasury = await TestAliasAccount.CreateAsync();
        await using var fxRenew = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var createParams = new CreateTokenParams
        {
            Name = Generator.Code(50),
            Symbol = Generator.Code(20),
            Circulation = (ulong)(Generator.Integer(10, 20) * 100000),
            Decimals = (uint)Generator.Integer(2, 5),
            Treasury = fxTreasury.Alias,
            Administrator = fxTreasury.PublicKey,
            GrantKycEndorsement = fxTreasury.PublicKey,
            SuspendEndorsement = fxTreasury.PublicKey,
            PauseEndorsement = fxTreasury.PublicKey,
            ConfiscateEndorsement = fxTreasury.PublicKey,
            SupplyEndorsement = fxTreasury.PublicKey,
            MetadataEndorsement = fxTreasury.PublicKey,
            RoyaltiesEndorsement = fxTreasury.PublicKey,
            InitializeSuspended = false,
            Expiration = Generator.TruncatedFutureDate(2000, 3000),
            RenewAccount = fxRenew.CreateReceipt!.Address,
            RenewPeriod = TimeSpan.FromDays(90),
            Signatory = new Signatory(fxTreasury.PrivateKey, fxRenew.PrivateKey),
            Memo = Generator.Memo(20)
        };
        var ex = await Assert.That(async () =>
        {
            await client.CreateTokenAsync(createParams);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Create Token failed with status: InvalidAccountId");
    }

    [Test]
    public async Task Zero_Circulation_Raises_Error()
    {
        var ex = await Assert.That(async () =>
        {
            await TestToken.CreateAsync(ctx =>
            {
                ctx.CreateParams.Circulation = 0;
            });
        }).ThrowsException();
        var aoe = ex as ArgumentOutOfRangeException;
        await Assert.That(aoe).IsNotNull();
        await Assert.That(aoe!.ParamName).IsEqualTo("Circulation");
        await Assert.That(aoe.Message).StartsWith("The initial circulation of tokens must be greater than zero.");
    }

    [Test]
    public async Task Zero_Divisibility_Is_Allowed()
    {
        await using var fxToken = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.Decimals = 0;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Decimals).IsEqualTo(0u);

        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)fxToken.CreateParams.Circulation);
    }

    [Test]
    public async Task Missing_Treasury_Address_Raises_Error()
    {
        var ex = await Assert.That(async () =>
        {
            await TestToken.CreateAsync(ctx =>
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
            await using var fx = await TestToken.CreateAsync(ctx =>
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
            await TestToken.CreateAsync(fx =>
            {
                fx.CreateParams.Treasury = fxFile.CreateReceipt!.File;
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Create Token failed with status: InvalidAccountId");
    }

    [Test]
    public async Task Can_Set_Treasury_To_Node_Contract_Account()
    {
        var fxContract = await GreetingContract.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Treasury = fxContract.ContractReceipt!.Contract;
            fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.RenewAccount.PrivateKey, fxContract.PrivateKey);
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Treasury).IsEqualTo(fxContract.ContractReceipt!.Contract);

        await Assert.That(await fxContract.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)fxToken.CreateParams.Circulation);
    }

    [Test]
    public async Task Null_Administrator_Key_Is_Allowed()
    {
        await using var fxToken = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.Administrator = null;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Administrator).IsNull();
    }

    [Test]
    public async Task Empty_Key_Administrator_Key_Is_Allowed()
    {
        await using var fxToken = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.Administrator = Endorsement.None;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Administrator).IsNull();
    }

    [Test]
    public async Task Null_Grant_Kyc_Key_Is_Allowed()
    {
        await using var fxToken = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.GrantKycEndorsement = null;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.GrantKycEndorsement).IsNull();
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
    }

    [Test]
    public async Task Empty_Key_Grant_Kyc_Key_Is_Allowed()
    {
        await using var fxToken = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.GrantKycEndorsement = Endorsement.None;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.GrantKycEndorsement).IsNull();
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
    }

    [Test]
    public async Task Null_Suspend_Key_Is_Allowed()
    {
        await using var fx = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.SuspendEndorsement = null;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.SuspendEndorsement).IsNull();
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.NotApplicable);
    }

    [Test]
    public async Task Empty_Key_Suspend_Key_Is_Allowed()
    {
        await using var fx = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.SuspendEndorsement = Endorsement.None;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.SuspendEndorsement).IsNull();
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.NotApplicable);
    }

    [Test]
    public async Task Null_Pause_Key_Is_Allowed()
    {
        await using var fx = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.PauseEndorsement = null;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.PauseEndorsement).IsNull();
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.NotApplicable);
    }

    [Test]
    public async Task Empty_Key_Pause_Key_Is_Allowed()
    {
        await using var fx = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.PauseEndorsement = Endorsement.None;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.PauseEndorsement).IsNull();
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.NotApplicable);
    }

    [Test]
    public async Task Null_Confiscate_Key_Is_Allowed()
    {
        await using var fx = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.ConfiscateEndorsement = null;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.ConfiscateEndorsement).IsNull();
    }

    [Test]
    public async Task Empty_Confiscate_Key_Is_Allowed()
    {
        await using var fx = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.ConfiscateEndorsement = Endorsement.None;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.ConfiscateEndorsement).IsNull();
    }

    [Test]
    public async Task Null_Supply_Key_Is_Allowed()
    {
        await using var fxToken = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.SupplyEndorsement = null;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.SupplyEndorsement).IsNull();
    }

    [Test]
    public async Task Empty_Supply_Key_Is_Allowed()
    {
        await using var fx = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.SupplyEndorsement = Endorsement.None;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.SupplyEndorsement).IsNull();
    }

    [Test]
    public async Task Null_Metadata_Key_Is_Allowed()
    {
        await using var fxToken = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.MetadataEndorsement = null;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.MetadataEndorsement).IsNull();
    }

    [Test]
    public async Task Empty_Metadata_Key_Is_Allowed()
    {
        await using var fx = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.MetadataEndorsement = Endorsement.None;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.MetadataEndorsement).IsNull();
    }

    [Test]
    public async Task Null_Royalties_Key_Is_Allowed()
    {
        await using var fxToken = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.RoyaltiesEndorsement = null;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.RoyaltiesEndorsement).IsNull();
    }

    [Test]
    public async Task Empty_Royalties_Key_Is_Allowed()
    {
        await using var fx = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.RoyaltiesEndorsement = Endorsement.None;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.RoyaltiesEndorsement).IsNull();
    }

    [Test]
    public async Task Null_Symbol_Is_Not_Allowed()
    {
        var ex = await Assert.That(async () =>
        {
            await using var fx = await TestToken.CreateAsync(ctx =>
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
            await using var fx = await TestToken.CreateAsync(ctx =>
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
        await using var fxToken = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.Symbol = "123" + Generator.Code(20) + "456";
        });
        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxToken.CreateReceipt.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxToken.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
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
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var treasury = await client.GetAccountInfoAsync(fxToken.TreasuryAccount.CreateReceipt.Address);
        await Assert.That(treasury.Balance).IsEqualTo(fxToken.TreasuryAccount.CreateParams.InitialBalance);

        var tokens = await fxToken.TreasuryAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).HasSingleItem();

        var token = tokens[0];
        await Assert.That(token.Token).IsEqualTo(fxToken.CreateReceipt.Token);
        await Assert.That(token.Balance).IsEqualTo((long)fxToken.CreateParams.Circulation);
        await Assert.That(token.KycStatus).IsEqualTo(TokenKycStatus.Granted);
        await Assert.That(token.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(token.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Task_Duplicate_Symbols_Are_Allowed()
    {
        await using var fxTaken = await TestToken.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Symbol = fxTaken.CreateParams.Symbol;
        });

        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxToken.CreateReceipt.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxToken.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
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
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var treasury = await client.GetAccountInfoAsync(fxToken.TreasuryAccount.CreateReceipt.Address);
        await Assert.That(treasury.Balance).IsEqualTo(fxToken.TreasuryAccount.CreateParams.InitialBalance);


        var tokens = await fxToken.TreasuryAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).HasSingleItem();

        var token = tokens[0];
        await Assert.That(token.Token).IsEqualTo(fxToken.CreateReceipt.Token);
        await Assert.That(token.Balance).IsEqualTo((long)fxToken.CreateParams.Circulation);
        await Assert.That(token.KycStatus).IsEqualTo(TokenKycStatus.Granted);
        await Assert.That(token.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(token.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Null_Name_Is_Not_Allowed()
    {
        var ex = await Assert.That(async () =>
        {
            await using var fx = await TestToken.CreateAsync(ctx =>
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
            await using var fx = await TestToken.CreateAsync(ctx =>
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
        await using var fx = await TestToken.CreateAsync(ctx =>
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
        await using var fxTaken = await TestToken.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Name = fxTaken.CreateParams.Name;
        });

        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxToken.CreateReceipt.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxToken.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
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
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var treasury = await client.GetAccountInfoAsync(fxToken.TreasuryAccount.CreateReceipt.Address);
        await Assert.That(treasury.Balance).IsEqualTo(fxToken.TreasuryAccount.CreateParams.InitialBalance);

        var tokens = await fxToken.TreasuryAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).HasSingleItem();

        var token = tokens[0];
        await Assert.That(token.Token).IsEqualTo(fxToken.CreateReceipt.Token);
        await Assert.That(token.Balance).IsEqualTo((long)fxToken.CreateParams.Circulation);
        await Assert.That(token.KycStatus).IsEqualTo(TokenKycStatus.Granted);
        await Assert.That(token.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(token.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Initialize_Supended_Can_Be_False()
    {
        await using var fx = await TestToken.CreateAsync(ctx =>
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
        await using var fx = await TestToken.CreateAsync();

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
    }

    [Test]
    public async Task Auto_Renew_Account_Is_Not_Required()
    {
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Expiration = DateTime.UtcNow.AddDays(33);
            fx.CreateParams.RenewPeriod = null;
            fx.CreateParams.RenewAccount = null;
            fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.TreasuryAccount.PrivateKey);
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.RenewAccount).IsNull();
    }

    [Test]
    public async Task Missing_Admin_Signature_Raises_Error()
    {
        var ex = await Assert.That(async () =>
        {
            await TestToken.CreateAsync(fx =>
            {
                fx.CreateParams.Signatory = new Signatory(fx.GrantPrivateKey, fx.SuspendPrivateKey, fx.ConfiscatePrivateKey, fx.SupplyPrivateKey, fx.RenewAccount.PrivateKey, fx.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Create Token failed with status: InvalidSignature");
    }

    [Test]
    public async Task Missing_Grant_Admin_Signature_Is_Allowed()
    {
        await using var fx = await TestToken.CreateAsync(fx =>
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
        await using var fx = await TestToken.CreateAsync(fx =>
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
        await using var fx = await TestToken.CreateAsync(fx =>
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
        await using var fx = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.GrantPrivateKey, fx.SuspendPrivateKey, fx.ConfiscatePrivateKey, fx.RenewAccount.PrivateKey, fx.TreasuryAccount.PrivateKey);
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.SupplyEndorsement).IsNotNull();
    }

    [Test]
    public async Task Create_Token_Missing_Renew_Account_Signature_Raises_Error()
    {
        var ex = await Assert.That(async () =>
        {
            await TestToken.CreateAsync(fx =>
            {
                fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.GrantPrivateKey, fx.SuspendPrivateKey, fx.ConfiscatePrivateKey, fx.SupplyPrivateKey, fx.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Create Token failed with status: InvalidSignature");
    }

    [Test]
    public async Task Null_Memo_Is_Allowed()
    {
        await using var fx = await TestToken.CreateAsync(fx =>
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
        await using var fx = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Memo = string.Empty;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fx.CreateReceipt!.Token);
        await Assert.That(info.Memo).IsEmpty();
    }

    [Test]
    public async Task Expiration_Time_In_Past_Raises_Error()
    {
        var ex = await Assert.That(async () =>
        {
            await TestToken.CreateAsync(fx =>
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
        await using var fx = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.RenewAccount.PrivateKey, fx.TreasuryAccount.PrivateKey);
        });
        await Assert.That(fx.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Create_With_Re_Used_Symbol_From_Deleted_Token()
    {
        await using var fxTaken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await client.DeleteTokenAsync(new DeleteTokenParams { Token = fxTaken.CreateReceipt!.Token, Signatory = fxTaken.AdminPrivateKey });
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Symbol = fxTaken.CreateParams.Symbol;
        });

        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxToken.CreateReceipt.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxToken.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
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
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var treasury = await client.GetAccountInfoAsync(fxToken.TreasuryAccount.CreateReceipt.Address);
        await Assert.That(treasury.Balance).IsEqualTo(fxToken.TreasuryAccount.CreateParams.InitialBalance);

        var tokens = await fxToken.TreasuryAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).HasSingleItem();

        var token = tokens[0];
        await Assert.That(token.Token).IsEqualTo(fxToken.CreateReceipt.Token);
        await Assert.That(token.Balance).IsEqualTo((long)fxToken.CreateParams.Circulation);
        await Assert.That(token.KycStatus).IsEqualTo(TokenKycStatus.Granted);
        await Assert.That(token.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(token.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Create_With_Re_Used_Name_From_Deleted_Token()
    {
        await using var fxTaken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await client.DeleteTokenAsync(new DeleteTokenParams { Token = fxTaken.CreateReceipt!.Token, Signatory = fxTaken.AdminPrivateKey });
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Name = fxTaken.CreateParams.Name;
        });

        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxToken.CreateReceipt.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxToken.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
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
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var treasury = await client.GetAccountInfoAsync(fxToken.TreasuryAccount.CreateReceipt.Address);
        await Assert.That(treasury.Balance).IsEqualTo(fxToken.TreasuryAccount.CreateParams.InitialBalance);

        var tokens = await fxToken.TreasuryAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).HasSingleItem();

        var token = tokens[0];
        await Assert.That(token.Token).IsEqualTo(fxToken.CreateReceipt.Token);
        await Assert.That(token.Balance).IsEqualTo((long)fxToken.CreateParams.Circulation);
        await Assert.That(token.KycStatus).IsEqualTo(TokenKycStatus.Granted);
        await Assert.That(token.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(token.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Create_With_Contract_As_Treasury()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Treasury = fxContract.ContractReceipt!.Contract;
            fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fxContract.PrivateKey, fx.RenewAccount.PrivateKey);
        });
        await Assert.That(fxToken.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxContract.ContractReceipt!.Contract);
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
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        await Assert.That(await fxContract.GetCryptoBalanceAsync()).IsEqualTo((ulong)fxContract.ContractParams.InitialBalance);

        var tokens = await fxContract.GetTokenBalancesAsync();
        await Assert.That(tokens).HasSingleItem();

        var token = tokens[0];
        await Assert.That(token.Balance).IsEqualTo((long)fxToken.CreateParams.Circulation);
    }

    [Test]
    public async Task Can_Create_Without_Renewal_Information()
    {
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.RenewAccount = null;
            fx.CreateParams.RenewPeriod = default;
        });
        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxToken.CreateReceipt.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxToken.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
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
        await Assert.That(info.RenewPeriod).IsNull();
        await Assert.That(info.RenewAccount).IsNull();
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();

        var treasury = await client.GetAccountInfoAsync(fxToken.TreasuryAccount.CreateReceipt.Address);
        await Assert.That(treasury.Balance).IsEqualTo(fxToken.TreasuryAccount.CreateParams.InitialBalance);

        var tokens = await fxToken.TreasuryAccount.GetTokenBalancesAsync();
        await Assert.That(tokens).HasSingleItem();

        var token = tokens[0];

        await Assert.That(token.Token).IsEqualTo(fxToken.CreateReceipt.Token);
        await Assert.That(token.Balance).IsEqualTo((long)fxToken.CreateParams.Circulation);
        await Assert.That(token.KycStatus).IsEqualTo(TokenKycStatus.Granted);
        await Assert.That(token.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(token.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Schedule_A_Create_Token()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        var fxTokenTemplate = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var schedulingReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = fxTokenTemplate.CreateParams,
            Payer = fxPayer,
        });
        await Assert.That(schedulingReceipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(schedulingReceipt).IsNotNull();
        var transactionReceipt = await client.SignScheduleAsync(schedulingReceipt.Schedule, ctx =>
        {
            ctx.Payer = fxPayer;
            ctx.Signatory = fxPayer;
        });
        var pendingReceipt = await client.GetReceiptAsync(schedulingReceipt.ScheduledTxId);
        await Assert.That(pendingReceipt.Status).IsEqualTo(ResponseCode.Success);

        var createReceipt = pendingReceipt as CreateTokenReceipt;
        await Assert.That(createReceipt).IsNotNull();

        var info = await client.GetTokenInfoAsync(createReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(createReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxTokenTemplate.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxTokenTemplate.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxTokenTemplate.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(fxTokenTemplate.CreateParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(fxTokenTemplate.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxTokenTemplate.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxTokenTemplate.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxTokenTemplate.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxTokenTemplate.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxTokenTemplate.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxTokenTemplate.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxTokenTemplate.CreateParams.SupplyEndorsement);
        await Assert.That(info.MetadataEndorsement).IsEqualTo(fxTokenTemplate.CreateParams.MetadataEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxTokenTemplate.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxTokenTemplate.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Create_Token_With_Fixed_Royalty()
    {
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Royalties = new FixedRoyalty[]
            {
                    new FixedRoyalty(fx.TreasuryAccount, EntityId.None, 1)
            };
        });
        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxToken.CreateReceipt.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxToken.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
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
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).HasSingleItem();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var royalty = info.Royalties[0] as FixedRoyalty;
        await Assert.That(royalty).IsNotNull();
        await Assert.That(royalty!.Receiver).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt.Address);
        await Assert.That(royalty.Token).IsEqualTo(EntityId.None);
        await Assert.That(royalty.Amount).IsEqualTo(1);

    }

    [Test]
    public async Task Can_Create_Token_With_Fractional_Royalty()
    {
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Royalties = new TokenRoyalty[]
            {
                    new TokenRoyalty(fx.TreasuryAccount, 1, 2, 1, 10)
            };
        });
        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxToken.CreateReceipt.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxToken.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
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
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).HasSingleItem();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var royalty = info.Royalties[0] as TokenRoyalty;
        await Assert.That(royalty).IsNotNull();
        await Assert.That(royalty!.Receiver).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt.Address);
        await Assert.That(royalty.Numerator).IsEqualTo(1);
        await Assert.That(royalty.Denominator).IsEqualTo(2);
        await Assert.That(royalty.Minimum).IsEqualTo(1);
        await Assert.That(royalty.Maximum).IsEqualTo(10);
        await Assert.That(royalty.AssessAsSurcharge).IsFalse();
    }

    [Test]
    public async Task Can_Create_Token_With_Fractional_Royalty_As_Surcharge()
    {
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Royalties = new TokenRoyalty[]
            {
                    new TokenRoyalty(fx.TreasuryAccount, 1, 2, 1, 10, true)
            };
        });
        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxToken.CreateReceipt.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxToken.CreateReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
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
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).HasSingleItem();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var royalty = info.Royalties[0] as TokenRoyalty;
        await Assert.That(royalty).IsNotNull();
        await Assert.That(royalty!.Receiver).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(royalty.Numerator).IsEqualTo(1);
        await Assert.That(royalty.Denominator).IsEqualTo(2);
        await Assert.That(royalty.Minimum).IsEqualTo(1);
        await Assert.That(royalty.Maximum).IsEqualTo(10);
        await Assert.That(royalty.AssessAsSurcharge).IsTrue();
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Create_Token()
    {
        await using var fxTreasury = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        var (adminPublicKey, adminPrivateKey) = Generator.KeyPair();
        var (supplyPublicKey, supplyPrivateKey) = Generator.KeyPair();
        await using var client = await TestNetwork.CreateClientAsync();

        var schedulingReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new CreateTokenParams
            {
                Name = Generator.Code(100),
                Symbol = Generator.Code(100),
                Circulation = 100_000,
                Decimals = 2,
                Treasury = fxTreasury.CreateReceipt!.Address,
                Administrator = adminPublicKey,
                SupplyEndorsement = supplyPublicKey,
                Expiration = DateTime.UtcNow.AddDays(90),
                Memo = Generator.Code(20),
                Signatory = new Signatory(adminPrivateKey, supplyPrivateKey),
            },
        });
        await Assert.That(schedulingReceipt.Schedule).IsNotEqualTo(EntityId.None);

        var signReceipt = await client.SignScheduleAsync(new SignScheduleParams
        {
            Schedule = schedulingReceipt.Schedule,
            Signatory = fxTreasury.PrivateKey,
        });
        await Assert.That(signReceipt.Status).IsEqualTo(ResponseCode.Success);
    }
}
