using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.Token;

public class UpdateTokenTests
{
    [Test]
    public async Task Can_Update_Token()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var fxTemplate = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxTemplate.TreasuryAccount.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxTemplate.TreasuryAccount.PrivateKey));

        var newSymbol = Generator.Code(20);
        var newName = Generator.String(20, 50);
        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Treasury = fxTemplate.CreateParams.Treasury,
            Administrator = fxTemplate.CreateParams.Administrator,
            GrantKycEndorsement = fxTemplate.CreateParams.GrantKycEndorsement,
            SuspendEndorsement = fxTemplate.CreateParams.SuspendEndorsement,
            ConfiscateEndorsement = fxTemplate.CreateParams.ConfiscateEndorsement,
            SupplyEndorsement = fxTemplate.CreateParams.SupplyEndorsement,
            MetadataEndorsement = fxTemplate.CreateParams.MetadataEndorsement,
            Symbol = newSymbol,
            Name = newName,
            Expiration = DateTime.UtcNow.AddDays(91),
            RenewPeriod = fxTemplate.CreateParams.RenewPeriod,
            RenewAccount = fxTemplate.RenewAccount,
            Signatory = new Signatory(fxToken.CreateParams.Signatory!, fxTemplate.CreateParams.Signatory!),
            Memo = fxTemplate.CreateParams.Memo
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(newSymbol);
        await Assert.That(info.Name).IsEqualTo(newName);
        await Assert.That(info.Treasury).IsEqualTo(fxTemplate.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxTemplate.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxTemplate.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxTemplate.CreateParams.SuspendEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxTemplate.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxTemplate.CreateParams.SupplyEndorsement);
        await Assert.That(info.MetadataEndorsement).IsEqualTo(fxTemplate.CreateParams.MetadataEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxToken.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Memo).IsEqualTo(fxTemplate.CreateParams.Memo);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Update_Token_And_Get_Record()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var fxTemplate = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        // It looks like changing the treasury requires the receiving account to be
        // associated first, since it still has to sign the update transaction anyway,
        // this seems unecessary.
        await client.AssociateTokenAsync(fxTemplate.TreasuryAccount.CreateReceipt!.Address, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxTemplate.TreasuryAccount.PrivateKey));

        var newSymbol = Generator.Code(20);
        var newName = Generator.String(20, 50);
        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Treasury = fxTemplate.CreateParams.Treasury,
            Administrator = fxTemplate.CreateParams.Administrator,
            GrantKycEndorsement = fxTemplate.CreateParams.GrantKycEndorsement,
            SuspendEndorsement = fxTemplate.CreateParams.SuspendEndorsement,
            ConfiscateEndorsement = fxTemplate.CreateParams.ConfiscateEndorsement,
            SupplyEndorsement = fxTemplate.CreateParams.SupplyEndorsement,
            MetadataEndorsement = fxTemplate.CreateParams.MetadataEndorsement,
            PauseEndorsement = fxTemplate.CreateParams.MetadataEndorsement,
            RoyaltiesEndorsement = fxTemplate.CreateParams.RoyaltiesEndorsement,
            Symbol = newSymbol,
            Name = newName,
            Expiration = DateTime.UtcNow.AddDays(91),
            RenewPeriod = fxTemplate.CreateParams.RenewPeriod,
            RenewAccount = fxTemplate.RenewAccount,
            Signatory = new Signatory(fxToken.CreateParams.Signatory!, fxTemplate.CreateParams.Signatory!),
            Memo = fxTemplate.CreateParams.Memo
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(newSymbol);
        await Assert.That(info.Name).IsEqualTo(newName);
        await Assert.That(info.Treasury).IsEqualTo(fxTemplate.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxTemplate.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxTemplate.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxTemplate.CreateParams.SuspendEndorsement);
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
    public async Task Empty_Update_Parameters_Raises_Error()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Signatory = fxToken.CreateParams.Signatory
        };
        var ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(updateParams);
        }).ThrowsException();
        var ae = ex as ArgumentException;
        await Assert.That(ae).IsNotNull();
        await Assert.That(ae!.ParamName).IsEqualTo("UpdateTokenParams");
        await Assert.That(ae.Message).StartsWith("The Token Updates contain no update properties, it is blank.");
    }

    [Test]
    public async Task Can_Update_Treasury()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Treasury = fxAccount.CreateReceipt!.Address,
            Signatory = new Signatory(fxToken.AdminPrivateKey, fxAccount.PrivateKey)
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Treasury).IsEqualTo(fxAccount.CreateReceipt!.Address);
    }

    [Test]
    public async Task Can_Update_Admin_Endorsement()
    {
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Administrator = newPublicKey,
            Signatory = new Signatory(fxToken.AdminPrivateKey, newPrivateKey)
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Administrator).IsEqualTo(updateParams.Administrator);
    }

    [Test]
    public async Task Can_Update_Grant_Kyc_Endorsement()
    {
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            GrantKycEndorsement = newPublicKey,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(updateParams.GrantKycEndorsement);
    }

    [Test]
    public async Task Can_Add_Grant_Kyc_Endorsement()
    {
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        await using var fxToken = await TestToken.CreateAsync(ctx =>
        {
            ctx.CreateParams.GrantKycEndorsement = null;
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            GrantKycEndorsement = newPublicKey,
            Signatory = fxToken.AdminPrivateKey
        };

        var ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(updateParams);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoKycKey);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.GrantKycEndorsement).IsNull();
    }

    [Test]
    public async Task Can_Update_Suspend_Endorsement()
    {
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            SuspendEndorsement = newPublicKey,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(updateParams.SuspendEndorsement);
    }

    [Test]
    public async Task Can_Update_Confiscate_Endorsement()
    {
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            ConfiscateEndorsement = newPublicKey,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(updateParams.ConfiscateEndorsement);
    }

    [Test]
    public async Task Can_Update_Supply_Endorsement()
    {
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            SupplyEndorsement = newPublicKey,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(updateParams.SupplyEndorsement);
    }

    [Test]
    public async Task Can_Update_Pause_Endorsement()
    {
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            PauseEndorsement = newPublicKey,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.PauseEndorsement).IsEqualTo(updateParams.PauseEndorsement);
    }

    [Test]
    public async Task Can_Update_Metadata_Endorsement()
    {
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            MetadataEndorsement = newPublicKey,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.MetadataEndorsement).IsEqualTo(updateParams.MetadataEndorsement);
    }

    [Test]
    public async Task Can_Update_Symbol()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newSymbol = Generator.Code(20);

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Symbol = newSymbol,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(newSymbol);
    }

    [Test]
    public async Task Can_Update_Name()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newName = Generator.String(30, 50);

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Name = newName,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Name).IsEqualTo(newName);
    }

    [Test]
    public async Task Can_Update_Memo()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newMemo = Generator.Memo(30, 50);

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Memo = newMemo,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Memo).IsEqualTo(newMemo);
    }

    [Test]
    public async Task Can_Update_Memo_To_Empty()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Memo = string.Empty,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Memo).IsEmpty();
    }

    [Test]
    public async Task Can_Update_Expiration()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var newExpiration = Generator.TruncateToSeconds(DateTime.UtcNow.AddDays(91));

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Expiration = newExpiration,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Expiration).IsEqualTo(newExpiration);
    }

    [Test]
    public async Task Can_Update_Renew_Period()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newRenwew = TimeSpan.FromDays(90) + TimeSpan.FromMinutes(10);

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            RenewPeriod = newRenwew,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.RenewPeriod).IsEqualTo(newRenwew);
    }

    [Test]
    public async Task Can_Update_Renew_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newRenwew = TimeSpan.FromDays(89);

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            RenewAccount = fxAccount.CreateReceipt!.Address,
            Signatory = new Signatory(fxToken.AdminPrivateKey, fxAccount.PrivateKey)
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.RenewAccount).IsEqualTo(fxAccount.CreateReceipt!.Address);
    }

    [Test]
    public async Task Any_Account_With_Admin_Key_Can_Update()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newName = Generator.String(30, 50);

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Name = newName,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = fxAccount.PrivateKey;
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Name).IsEqualTo(newName);
    }

    [Test]
    public async Task Updates_Require_Admin_Key()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Name = Generator.String(30, 50)
        };

        var ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(updateParams);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Update Token failed with status: InvalidSignature");

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
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
    }

    [Test]
    public async Task Updating_To_Used_Symbol_Is_Allowed()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var fxOther = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Symbol = fxOther.CreateParams.Symbol,
            Signatory = fxToken.AdminPrivateKey
        };
        var receipt = await client.UpdateTokenAsync(updateParams);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(fxOther.CreateParams.Symbol);
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
    }

    [Test]
    public async Task Updating_To_Used_Name_Is_Allowed()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var fxOther = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Name = fxOther.CreateParams.Name,
            Signatory = fxToken.AdminPrivateKey
        };

        await client.UpdateTokenAsync(updateParams);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxOther.CreateParams.Name);
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
    }

    [Test]
    public async Task Updating_To_Empty_Treasury_Raises_Error()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Treasury = EntityId.None,
            Signatory = fxToken.AdminPrivateKey
        };

        var ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(updateParams);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Update Token failed with status: InvalidAccountId");

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
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
    }

    [Test]
    public async Task Can_Make_Token_Immutable()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var memo = Generator.Memo(50, 100);
        await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Memo = memo,
            Signatory = fxToken.AdminPrivateKey
        });
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Memo).IsEqualTo(memo);

        await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Administrator = Endorsement.None,
            Signatory = fxToken.AdminPrivateKey
        });

        var ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Memo = Generator.Memo(20, 60),
                Signatory = fxToken.AdminPrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsImmutable);

        info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsNull();
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
        await Assert.That(info.Memo).IsEqualTo(memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Remove_Grant_KYC_Endorsement()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            GrantKycEndorsement = Endorsement.None,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.GrantKycEndorsement).IsNull();
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);

        // Check for other regressions
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxToken.CreateParams.Administrator);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxToken.CreateParams.SuspendEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxToken.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxToken.CreateParams.SupplyEndorsement);
        await Assert.That(info.MetadataEndorsement).IsEqualTo(fxToken.CreateParams.MetadataEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxToken.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Remove_Suspend_Endorsement()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            SuspendEndorsement = Endorsement.None,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.SuspendEndorsement).IsNull();

        // Check for any regressions
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxToken.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxToken.CreateParams.GrantKycEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxToken.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxToken.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxToken.CreateParams.SupplyEndorsement);
        await Assert.That(info.MetadataEndorsement).IsEqualTo(fxToken.CreateParams.MetadataEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxToken.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.NotApplicable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Remove_Confiscate_Endorsement()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            ConfiscateEndorsement = Endorsement.None,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.ConfiscateEndorsement).IsNull();

        // Check for other changes
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxToken.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxToken.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxToken.CreateParams.SuspendEndorsement);
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
    }

    [Test]
    public async Task Can_Remove_Supply_Endorsement()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            SupplyEndorsement = Endorsement.None,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.SupplyEndorsement).IsNull();

        // Check for any other regressions
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
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
        await Assert.That(info.MetadataEndorsement).IsEqualTo(fxToken.CreateParams.MetadataEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxToken.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Remove_Pause_Endorsement()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            PauseEndorsement = Endorsement.None,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.PauseEndorsement).IsNull();
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.NotApplicable);

        // Check for any other regressions
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxToken.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxToken.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxToken.CreateParams.SuspendEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxToken.CreateParams.SupplyEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxToken.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.MetadataEndorsement).IsEqualTo(fxToken.CreateParams.MetadataEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxToken.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Remove_Metadata_Endorsement()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            MetadataEndorsement = Endorsement.None,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.MetadataEndorsement).IsNull();

        // Check for any other revisions.
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
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
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxToken.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Cannot_Update_Imutable_Token()
    {
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.Administrator = null);
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            SupplyEndorsement = newPublicKey
        };

        var ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(updateParams);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        // DEFECT 0.50.0 - The following is what SHOULD be returned.
        //await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsImmutable);
        //await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.TokenIsImmutable);
        //await Assert.That(tex.Message).StartsWith("Update Token failed with status: TokenIsImmutable");
        // Instead this is what we get
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Update Token failed with status: InvalidSignature");
    }

    [Test]
    public async Task Updating_The_Treasury_Without_Signing_Raises_Error()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Treasury = fxAccount.CreateReceipt!.Address,
            Signatory = fxToken.AdminPrivateKey
        };

        var ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(updateParams);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Update Token failed with status: InvalidSignature");

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
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

        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)fxToken.CreateParams.Circulation);
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
    }

    [Test]
    public async Task Updating_The_Treasury_Without_Signing_Without_Admin_Key_Raises_Error()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Treasury = fxAccount.CreateReceipt!.Address,
            Signatory = fxAccount.PrivateKey
        };

        var ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(updateParams);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Update Token failed with status: InvalidSignature");

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
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

        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)fxToken.CreateParams.Circulation);
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Update_Treasury_To_Contract()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        // Note: Contract did not need to sign.
        await client.AssociateTokenAsync(fxContract.ContractReceipt!.Contract, fxToken.CreateReceipt!.Token, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxContract.PrivateKey));

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Treasury = fxContract.ContractReceipt!.Contract,
            Signatory = new Signatory(fxToken.AdminPrivateKey, fxContract.PrivateKey)
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Treasury).IsEqualTo(fxContract.ContractReceipt!.Contract);

        await Assert.That(await fxContract.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)fxToken.CreateParams.Circulation);
    }

    [Test]
    public async Task Removing_An_Auto_Renew_Account_Is_Not_Allowed()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = fxToken.RenewAccount.CreateReceipt!.Address,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fxToken.RenewAccount.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
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
        await Assert.That(info.RenewAccount).IsEqualTo(fxToken.CreateParams.RenewAccount);
        await Assert.That(info.RenewPeriod).IsEqualTo(fxToken.CreateParams.RenewPeriod);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxToken.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Change_Treasury_To_Unassociated_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 0);
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.ConfiscateEndorsement = null;
            fx.CreateParams.SuspendEndorsement = null;
            fx.CreateParams.GrantKycEndorsement = null;
        });
        await using var client = await TestNetwork.CreateClientAsync();
        var totalCirculation = fxToken.CreateParams.Circulation;

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsNull();
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)totalCirculation);

        // Returns A Failure
        var ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Treasury = fxAccount.CreateReceipt!.Address,
                Signatory = new Signatory(fxToken.AdminPrivateKey, fxAccount.PrivateKey)
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();

        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.NoRemainingAutomaticAssociations);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.NoRemainingAutomaticAssociations);

        // Confirm it did not change the Treasury Address
        var info = await client.GetTokenInfoAsync(fxToken);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
    }

    [Test]
    public async Task Can_Change_Treasury_To_Airdrop_Enabled_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = -1);
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.ConfiscateEndorsement = null;
            fx.CreateParams.SuspendEndorsement = null;
            fx.CreateParams.GrantKycEndorsement = null;
        });
        await using var client = await TestNetwork.CreateClientAsync();
        var totalCirculation = fxToken.CreateParams.Circulation;

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsNull();
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)totalCirculation);

        await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Treasury = fxAccount.CreateReceipt!.Address,
            Signatory = new Signatory(fxToken.AdminPrivateKey, fxAccount.PrivateKey)
        });

        // Confirm it did change the Treasury Address
        var info = await client.GetTokenInfoAsync(fxToken);
        await Assert.That(info.Treasury).IsEqualTo(fxAccount.CreateReceipt!.Address);
    }

    [Test]
    public async Task Can_Change_Treasury_To_Auto_Associated_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.AutoAssociationLimit = 1);
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.ConfiscateEndorsement = null;
            fx.CreateParams.SuspendEndorsement = null;
            fx.CreateParams.GrantKycEndorsement = null;
        });
        await using var client = await TestNetwork.CreateClientAsync();
        var totalCirculation = fxToken.CreateParams.Circulation;

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsNull();
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)totalCirculation);

        await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Treasury = fxAccount.CreateReceipt!.Address,
            Signatory = new Signatory(fxToken.AdminPrivateKey, fxAccount.PrivateKey)
        });

        // Confirm it did change the Treasury Address
        var info = await client.GetTokenInfoAsync(fxToken);
        await Assert.That(info.Treasury).IsEqualTo(fxAccount.CreateReceipt!.Address);
    }

    [Test]
    public async Task Can_Schedule_Update_Token()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newSymbol = Generator.Code(20);
        var scheduledReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Symbol = newSymbol,
                Signatory = fxToken.AdminPrivateKey,
            },
            Payer = fxPayer
        });
        await Assert.That(scheduledReceipt.Status).IsEqualTo(ResponseCode.Success);

        var infoBefore = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(infoBefore.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);

        var executionReceipt = await client.SignScheduleAsync(new SignScheduleParams
        {
            Schedule = scheduledReceipt.Schedule,
            Signatory = fxPayer.PrivateKey,
        }, ctx =>
        {
            ctx.Payer = fxPayer;
            ctx.Signatory = fxPayer;
        });
        var pendingReceipt = await client.GetReceiptAsync(scheduledReceipt.ScheduledTxId);
        await Assert.That(pendingReceipt.Status).IsEqualTo(ResponseCode.Success);

        var infoAfter = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(infoAfter.Symbol).IsEqualTo(newSymbol);
    }

    [Test]
    public async Task Can_Update_Token_Fixed_Royalty()
    {
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Royalties = new FixedRoyalty[]
            {
                new FixedRoyalty(fx.TreasuryAccount, EntityId.None, 10)
            };
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var royalties = new FixedRoyalty[]
        {
            new FixedRoyalty(fxToken.TreasuryAccount, EntityId.None, 1)
        };

        var receipt = await client.UpdateRoyaltiesAsync(fxToken.CreateReceipt!.Token, royalties, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.RoyaltiesPrivateKey));
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Royalties).HasSingleItem();

        var royalty = info.Royalties[0] as FixedRoyalty;
        await Assert.That(royalty).IsNotNull();
        await Assert.That(royalty!.Receiver).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(royalty.Token).IsEqualTo(EntityId.None);
        await Assert.That(royalty.Amount).IsEqualTo(1);
    }

    [Test]
    public async Task Can_Update_Token_Fixed_Royalty_And_Get_Record()
    {
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Royalties = new FixedRoyalty[]
            {
                new FixedRoyalty(fx.TreasuryAccount, EntityId.None, 10)
            };
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var royalties = new FixedRoyalty[]
        {
            new FixedRoyalty(fxToken.TreasuryAccount, EntityId.None, 1)
        };

        var receipt = await client.UpdateRoyaltiesAsync(fxToken.CreateReceipt!.Token, royalties, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.RoyaltiesPrivateKey));
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Royalties).HasSingleItem();

        var royalty = info.Royalties[0] as FixedRoyalty;
        await Assert.That(royalty).IsNotNull();
        await Assert.That(royalty!.Receiver).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(royalty.Token).IsEqualTo(EntityId.None);
        await Assert.That(royalty.Amount).IsEqualTo(1);
    }

    [Test]
    public async Task Can_Update_Token_Fixed_Royalty_And_Get_Record_With_Signatory_In_Context()
    {
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Royalties = new FixedRoyalty[]
            {
                new FixedRoyalty(fx.TreasuryAccount, EntityId.None, 10)
            };
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var royalties = new FixedRoyalty[]
        {
            new FixedRoyalty(fxToken.TreasuryAccount, EntityId.None, 1)
        };

        var receipt = await client.UpdateRoyaltiesAsync(fxToken.CreateReceipt!.Token, royalties, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.RoyaltiesPrivateKey));
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Royalties).HasSingleItem();

        var royalty = info.Royalties[0] as FixedRoyalty;
        await Assert.That(royalty).IsNotNull();
        await Assert.That(royalty!.Receiver).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(royalty.Token).IsEqualTo(EntityId.None);
        await Assert.That(royalty.Amount).IsEqualTo(1);
    }

    [Test]
    public async Task Can_Update_Token_Fractional_Royalty()
    {
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Royalties = new TokenRoyalty[]
            {
                new TokenRoyalty(fx.TreasuryAccount, 1, 10, 1, 10)
            };
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var royalties = new TokenRoyalty[]
        {
            new TokenRoyalty(fxToken.TreasuryAccount, 1, 2, 1, 100)
        };

        var receipt = await client.UpdateRoyaltiesAsync(fxToken.CreateReceipt!.Token, royalties, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.RoyaltiesPrivateKey));
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Royalties).HasSingleItem();

        var royalty = info.Royalties[0] as TokenRoyalty;
        await Assert.That(royalty).IsNotNull();
        await Assert.That(royalty!.Receiver).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(royalty.Numerator).IsEqualTo(1);
        await Assert.That(royalty.Denominator).IsEqualTo(2);
        await Assert.That(royalty.Minimum).IsEqualTo(1);
        await Assert.That(royalty.Maximum).IsEqualTo(100);
        await Assert.That(royalty.AssessAsSurcharge).IsFalse();
    }

    [Test]
    public async Task Can_Clear_And_Freeze_Royalty_Tables()
    {
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.Royalties = new FixedRoyalty[]
            {
                new FixedRoyalty(fx.TreasuryAccount, EntityId.None, 1)
            };
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.UpdateRoyaltiesAsync(fxToken.CreateReceipt!.Token, Array.Empty<IRoyalty>(), ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.RoyaltiesPrivateKey));
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Royalties).IsEmpty();
    }

    [Test]
    public async Task Can_Remove_Royalties_Endorsement()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            RoyaltiesEndorsement = Endorsement.None,
            Signatory = fxToken.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.RoyaltiesEndorsement).IsNull();

        // Check for any other revisions.
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
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
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Renew_Imutable_Topic()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 10_00_000_000);
        await using var client = await TestNetwork.CreateClientAsync();

        await using var scopedClient = client.Clone(ctx =>
        {
            ctx.Payer = fxAccount;
            ctx.Signatory = fxAccount;
        });

        var memo = Generator.Code(20);
        var receipt = await scopedClient.CreateTopicAsync(new CreateTopicParams
        {
            Memo = memo,
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTopicInfoAsync(receipt.Topic);
        await Assert.That(info.Memo).IsEqualTo(memo);
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo(0UL);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Administrator).IsNull();
        await Assert.That(info.Participant).IsNull();
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsNull();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var newExpiration = new ConsensusTimeStamp(info.Expiration.Seconds + 190 * 60);
        var renew = await scopedClient.UpdateTopicAsync(new UpdateTopicParams()
        {
            Topic = receipt.Topic,
            Expiration = newExpiration
        });
        await Assert.That(renew.Status).IsEqualTo(ResponseCode.Success);

        info = await client.GetTopicInfoAsync(receipt.Topic);
        await Assert.That(info.Memo).IsEqualTo(memo);
        await Assert.That(info.RunningHash.ToArray()).IsNotEmpty();
        await Assert.That(info.SequenceNumber).IsEqualTo(0UL);
        await Assert.That(info.Expiration).IsEqualTo(newExpiration);
        await Assert.That(info.Administrator).IsNull();
        await Assert.That(info.Participant).IsNull();
        await Assert.That(info.AutoRenewPeriod > TimeSpan.MinValue).IsTrue();
        await Assert.That(info.RenewAccount).IsNull();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Cannot_Add_Administrative_Keys()
    {
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.SuspendEndorsement = null;
            fx.CreateParams.PauseEndorsement = null;
            fx.CreateParams.ConfiscateEndorsement = null;
            fx.CreateParams.SupplyEndorsement = null;
            fx.CreateParams.MetadataEndorsement = null;
            fx.CreateParams.RoyaltiesEndorsement = null;
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxToken.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsNull();
        await Assert.That(info.SuspendEndorsement).IsNull();
        await Assert.That(info.ConfiscateEndorsement).IsNull();
        await Assert.That(info.SupplyEndorsement).IsNull();
        await Assert.That(info.MetadataEndorsement).IsNull();
        await Assert.That(info.RoyaltiesEndorsement).IsNull();
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.NotApplicable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.NotApplicable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var (newPublicKey, newPrivateKey) = Generator.KeyPair();

        var ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Administrator = fxToken.CreateParams.Administrator,
                GrantKycEndorsement = newPublicKey,
                Signatory = new Signatory(new Signatory(newPrivateKey), new Signatory(fxToken.AdminPrivateKey))
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoKycKey);

        ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Administrator = fxToken.CreateParams.Administrator,
                SuspendEndorsement = newPublicKey,
                Signatory = new Signatory(new Signatory(newPrivateKey), new Signatory(fxToken.AdminPrivateKey))
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoFreezeKey);

        ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Administrator = fxToken.CreateParams.Administrator,
                ConfiscateEndorsement = newPublicKey,
                Signatory = new Signatory(new Signatory(newPrivateKey), new Signatory(fxToken.AdminPrivateKey))
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoWipeKey);

        ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Administrator = fxToken.CreateParams.Administrator,
                SupplyEndorsement = newPublicKey,
                Signatory = new Signatory(new Signatory(newPrivateKey), new Signatory(fxToken.AdminPrivateKey))
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoSupplyKey);

        ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Administrator = fxToken.CreateParams.Administrator,
                MetadataEndorsement = newPublicKey,
                Signatory = new Signatory(new Signatory(newPrivateKey), new Signatory(fxToken.AdminPrivateKey))
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoMetadataKey);

        ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Administrator = fxToken.CreateParams.Administrator,
                RoyaltiesEndorsement = newPublicKey,
                Signatory = new Signatory(new Signatory(newPrivateKey), new Signatory(fxToken.AdminPrivateKey))
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoFeeScheduleKey);

        info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxToken.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsNull();
        await Assert.That(info.SuspendEndorsement).IsNull();
        await Assert.That(info.ConfiscateEndorsement).IsNull();
        await Assert.That(info.SupplyEndorsement).IsNull();
        await Assert.That(info.MetadataEndorsement).IsNull();
        await Assert.That(info.RoyaltiesEndorsement).IsNull();
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.NotApplicable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.NotApplicable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Cannot_Update_Empty_Administrative_Keys()
    {
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = Endorsement.None;
            fx.CreateParams.SuspendEndorsement = Endorsement.None;
            fx.CreateParams.PauseEndorsement = Endorsement.None;
            fx.CreateParams.ConfiscateEndorsement = Endorsement.None;
            fx.CreateParams.SupplyEndorsement = Endorsement.None;
            fx.CreateParams.MetadataEndorsement = Endorsement.None;
            fx.CreateParams.RoyaltiesEndorsement = Endorsement.None;
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxToken.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsNull();
        await Assert.That(info.SuspendEndorsement).IsNull();
        await Assert.That(info.ConfiscateEndorsement).IsNull();
        await Assert.That(info.SupplyEndorsement).IsNull();
        await Assert.That(info.MetadataEndorsement).IsNull();
        await Assert.That(info.RoyaltiesEndorsement).IsNull();
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.NotApplicable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.NotApplicable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var (newPublicKey, newPrivateKey) = Generator.KeyPair();

        var ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Administrator = fxToken.CreateParams.Administrator,
                GrantKycEndorsement = newPublicKey,
                Signatory = new Signatory(new Signatory(newPrivateKey), new Signatory(fxToken.AdminPrivateKey))
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoKycKey);

        ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Administrator = fxToken.CreateParams.Administrator,
                SuspendEndorsement = newPublicKey,
                Signatory = new Signatory(new Signatory(newPrivateKey), new Signatory(fxToken.AdminPrivateKey))
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoFreezeKey);

        ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Administrator = fxToken.CreateParams.Administrator,
                ConfiscateEndorsement = newPublicKey,
                Signatory = new Signatory(new Signatory(newPrivateKey), new Signatory(fxToken.AdminPrivateKey))
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoWipeKey);

        ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Administrator = fxToken.CreateParams.Administrator,
                SupplyEndorsement = newPublicKey,
                Signatory = new Signatory(new Signatory(newPrivateKey), new Signatory(fxToken.AdminPrivateKey))
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoSupplyKey);

        ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Administrator = fxToken.CreateParams.Administrator,
                MetadataEndorsement = newPublicKey,
                Signatory = new Signatory(new Signatory(newPrivateKey), new Signatory(fxToken.AdminPrivateKey))
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoMetadataKey);

        ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Administrator = fxToken.CreateParams.Administrator,
                RoyaltiesEndorsement = newPublicKey,
                Signatory = new Signatory(new Signatory(newPrivateKey), new Signatory(fxToken.AdminPrivateKey))
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoFeeScheduleKey);

        info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxToken.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsNull();
        await Assert.That(info.SuspendEndorsement).IsNull();
        await Assert.That(info.ConfiscateEndorsement).IsNull();
        await Assert.That(info.SupplyEndorsement).IsNull();
        await Assert.That(info.MetadataEndorsement).IsNull();
        await Assert.That(info.RoyaltiesEndorsement).IsNull();
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.NotApplicable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.NotApplicable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Remove_All_Administrative_Keys()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
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
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var receipt = await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            GrantKycEndorsement = Endorsement.None,
            Signatory = new Signatory(fxToken.AdminPrivateKey)
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        receipt = await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            SuspendEndorsement = Endorsement.None,
            Signatory = new Signatory(fxToken.AdminPrivateKey)
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        receipt = await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            PauseEndorsement = Endorsement.None,
            Signatory = new Signatory(fxToken.AdminPrivateKey)
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        receipt = await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            ConfiscateEndorsement = Endorsement.None,
            Signatory = new Signatory(fxToken.AdminPrivateKey)
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        receipt = await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            SupplyEndorsement = Endorsement.None,
            Signatory = new Signatory(fxToken.AdminPrivateKey)
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        receipt = await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            MetadataEndorsement = Endorsement.None,
            Signatory = new Signatory(fxToken.AdminPrivateKey)
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        receipt = await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            RoyaltiesEndorsement = Endorsement.None,
            Signatory = new Signatory(fxToken.AdminPrivateKey)
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        receipt = await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Administrator = Endorsement.None,
            Signatory = new Signatory(fxToken.AdminPrivateKey)
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsNull();
        await Assert.That(info.GrantKycEndorsement).IsNull();
        await Assert.That(info.SuspendEndorsement).IsNull();
        await Assert.That(info.PauseEndorsement).IsNull();
        await Assert.That(info.ConfiscateEndorsement).IsNull();
        await Assert.That(info.SupplyEndorsement).IsNull();
        await Assert.That(info.MetadataEndorsement).IsNull();
        await Assert.That(info.RoyaltiesEndorsement).IsNull();
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.NotApplicable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.NotApplicable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Remove_All_Keys()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var fxPattern = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Administrator = fxPattern.AdminPublicKey,
            GrantKycEndorsement = fxPattern.GrantPublicKey,
            SuspendEndorsement = fxPattern.SuspendPublicKey,
            PauseEndorsement = fxPattern.PausePublicKey,
            ConfiscateEndorsement = fxPattern.ConfiscatePublicKey,
            SupplyEndorsement = fxPattern.SupplyPublicKey,
            MetadataEndorsement = fxPattern.MetadataPublicKey,
            RoyaltiesEndorsement = fxPattern.RoyaltiesPublicKey,
            Signatory = new Signatory(fxToken.AdminPrivateKey, fxPattern.AdminPrivateKey)
        });

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxPattern.AdminPublicKey);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxPattern.GrantPublicKey);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxPattern.SuspendPublicKey);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxPattern.PausePublicKey);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxPattern.ConfiscatePublicKey);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxPattern.SupplyPublicKey);
        await Assert.That(info.MetadataEndorsement).IsEqualTo(fxPattern.MetadataPublicKey);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxPattern.RoyaltiesPublicKey);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Not_Remove_Administrative_Keys_On_Imutable_Token()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
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
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        var receipt = await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Administrator = Endorsement.None,
            Signatory = new Signatory(fxToken.AdminPrivateKey)
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                GrantKycEndorsement = Endorsement.None,
                Signatory = new Signatory(fxToken.AdminPrivateKey)
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsImmutable);

        ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                SuspendEndorsement = Endorsement.None,
                Signatory = new Signatory(fxToken.AdminPrivateKey)
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsImmutable);

        ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                PauseEndorsement = Endorsement.None,
                Signatory = new Signatory(fxToken.AdminPrivateKey)
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsImmutable);

        ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                ConfiscateEndorsement = Endorsement.None,
                Signatory = new Signatory(fxToken.AdminPrivateKey)
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsImmutable);

        ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                SupplyEndorsement = Endorsement.None,
                Signatory = new Signatory(fxToken.AdminPrivateKey)
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsImmutable);

        ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                MetadataEndorsement = Endorsement.None,
                Signatory = new Signatory(fxToken.AdminPrivateKey)
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsImmutable);

        ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                RoyaltiesEndorsement = Endorsement.None,
                Signatory = new Signatory(fxToken.AdminPrivateKey)
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsImmutable);

        info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxToken.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
        await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsNull();
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
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Update_Token()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var schedulingReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new UpdateTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Memo = Generator.Memo(10, 20),
            },
        });
        await Assert.That(schedulingReceipt.Schedule).IsNotEqualTo(EntityId.None);

        var signReceipt = await client.SignScheduleAsync(new SignScheduleParams
        {
            Schedule = schedulingReceipt.Schedule,
            Signatory = fxToken.AdminPrivateKey,
        });
        await Assert.That(signReceipt.Status).IsEqualTo(ResponseCode.Success);
    }
}
