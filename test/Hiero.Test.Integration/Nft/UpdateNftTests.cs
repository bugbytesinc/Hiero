using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.NftTokens;

public class UpdateNftTests
{
    [Test]
    public async Task Can_Update_Nft()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var fxTemplate = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokenAsync(fxTemplate.TreasuryAccount.CreateReceipt!.Address, fxNft.CreateReceipt!.Token, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxTemplate.TreasuryAccount.PrivateKey);
        });

        var newSymbol = Generator.Code(20);
        var newName = Generator.String(20, 50);
        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            // Can't do this with NFTs, they don't transfer automatically
            //Treasury = fxTemplate.CreateParams.Treasury,
            Administrator = fxTemplate.CreateParams.Administrator,
            GrantKycEndorsement = fxTemplate.CreateParams.GrantKycEndorsement,
            SuspendEndorsement = fxTemplate.CreateParams.SuspendEndorsement,
            PauseEndorsement = fxTemplate.CreateParams.PauseEndorsement,
            ConfiscateEndorsement = fxTemplate.CreateParams.ConfiscateEndorsement,
            SupplyEndorsement = fxTemplate.CreateParams.SupplyEndorsement,
            RoyaltiesEndorsement = fxTemplate.CreateParams.RoyaltiesEndorsement,
            Symbol = newSymbol,
            Name = newName,
            Expiration = DateTime.UtcNow.AddDays(91),
            RenewPeriod = fxTemplate.CreateParams.RenewPeriod,
            RenewAccount = fxTemplate.RenewAccount,
            Signatory = new Signatory(fxNft.CreateParams.Signatory!, fxTemplate.CreateParams.Signatory!),
            Memo = fxTemplate.CreateParams.Memo
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(newSymbol);
        await Assert.That(info.Name).IsEqualTo(newName);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0u);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxTemplate.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxTemplate.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxTemplate.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxTemplate.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxTemplate.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxTemplate.CreateParams.SupplyEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxTemplate.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Memo).IsEqualTo(fxTemplate.CreateParams.Memo);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Update_Nft_Generates_Invalid_HAPI_Protobuf_Defect()
    {
        // Defect: 0.49.0
        await using var fxNft = await TestNft.CreateAsync(fx => fx.Metadata = null!);
        await using var fxTemplate = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        // It looks like changing the treasury requires the receiving account to be
        // associated first, since it still has to sign the update transaction anyway,
        // this seems unecessary.
        await client.AssociateTokenAsync(fxTemplate.TreasuryAccount.CreateReceipt!.Address, fxNft.CreateReceipt!.Token, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxTemplate.TreasuryAccount.PrivateKey);
        });

        var newSymbol = Generator.Code(100);
        var newName = Generator.String(20, 50);
        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Treasury = fxTemplate.CreateParams.Treasury,
            Administrator = fxTemplate.CreateParams.Administrator,
            GrantKycEndorsement = fxTemplate.CreateParams.GrantKycEndorsement,
            SuspendEndorsement = fxTemplate.CreateParams.SuspendEndorsement,
            PauseEndorsement = fxTemplate.CreateParams.PauseEndorsement,
            ConfiscateEndorsement = fxTemplate.CreateParams.ConfiscateEndorsement,
            SupplyEndorsement = fxTemplate.CreateParams.SupplyEndorsement,
            Symbol = newSymbol,
            Name = newName,
            Expiration = DateTime.UtcNow.AddDays(91),
            RenewPeriod = fxTemplate.CreateParams.RenewPeriod,
            RenewAccount = fxTemplate.RenewAccount,
            Signatory = new Signatory(fxNft.CreateParams.Signatory!, fxTemplate.CreateParams.Signatory!),
            Memo = fxTemplate.CreateParams.Memo
        };

        // NOTE: Prior Hedera defect (0.49.0) that produced a sentinel value in the
        // record with an invalid serial number has been fixed. The update now succeeds
        // and the record can be retrieved without error.
        var receipt2 = await client.UpdateTokenAsync(updateParams);
        var record = await client.GetTransactionRecordAsync(receipt2.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee >= 0UL).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        // Verify the changes were made
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(newSymbol);
        await Assert.That(info.Name).IsEqualTo(newName);
        await Assert.That(info.Treasury).IsEqualTo(fxTemplate.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(0UL);
        await Assert.That(info.Decimals).IsEqualTo(0u);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxTemplate.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxTemplate.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxTemplate.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxTemplate.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxTemplate.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxTemplate.CreateParams.SupplyEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxTemplate.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Empty_Update_Parameters_Raises_Error()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Signatory = fxNft.CreateParams.Signatory
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
        await using var fxNft = await TestNft.CreateAsync(ctx => ctx.Metadata = null!, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Treasury = fxAccount.CreateReceipt!.Address,
            Signatory = new Signatory(fxNft.AdminPrivateKey, fxAccount.PrivateKey)
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Treasury).IsEqualTo(fxAccount.CreateReceipt!.Address);
    }

    [Test]
    public async Task Can_Update_Admin_Endorsement()
    {
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Administrator = newPublicKey,
            Signatory = new Signatory(fxNft.AdminPrivateKey, newPrivateKey)
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Administrator).IsEqualTo(updateParams.Administrator);
    }

    [Test]
    public async Task Can_Update_Grant_Kyc_Endorsement()
    {
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            GrantKycEndorsement = newPublicKey,
            Signatory = fxNft.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(updateParams.GrantKycEndorsement);
    }

    [Test]
    public async Task Can_Update_Suspend_Endorsement()
    {
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            SuspendEndorsement = newPublicKey,
            Signatory = fxNft.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(updateParams.SuspendEndorsement);
    }

    [Test]
    public async Task Can_Update_Confiscate_Endorsement()
    {
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            ConfiscateEndorsement = newPublicKey,
            Signatory = fxNft.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(updateParams.ConfiscateEndorsement);
    }

    [Test]
    public async Task Can_Update_Supply_Endorsement()
    {
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            SupplyEndorsement = newPublicKey,
            Signatory = fxNft.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(updateParams.SupplyEndorsement);
    }

    [Test]
    public async Task Can_Update_Royalties_Endorsement()
    {
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            RoyaltiesEndorsement = newPublicKey,
            Signatory = fxNft.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(updateParams.RoyaltiesEndorsement);
    }

    [Test]
    public async Task Can_Update_Symbol()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newSymbol = Generator.Code(20);

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Symbol = newSymbol,
            Signatory = fxNft.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(newSymbol);
    }

    [Test]
    public async Task Can_Update_Name()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newName = Generator.String(30, 50);

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Name = newName,
            Signatory = fxNft.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Name).IsEqualTo(newName);
    }

    [Test]
    public async Task Can_Update_Memo()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newMemo = Generator.Memo(30, 50);

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Memo = newMemo,
            Signatory = fxNft.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Memo).IsEqualTo(newMemo);
    }

    [Test]
    public async Task Can_Update_Memo_To_Empty()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Memo = string.Empty,
            Signatory = fxNft.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Memo).IsEmpty();
    }

    [Test]
    public async Task Can_Update_Expiration()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var newExpiration = Generator.TruncateToSeconds(DateTime.UtcNow.AddDays(91));

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Expiration = newExpiration,
            Signatory = fxNft.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Expiration == newExpiration).IsTrue();
    }

    [Test]
    public async Task Can_Update_Renew_Period()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newRenwew = TimeSpan.FromDays(90) + TimeSpan.FromMinutes(10);

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            RenewPeriod = newRenwew,
            Signatory = fxNft.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.RenewPeriod).IsEqualTo(newRenwew);
    }

    [Test]
    public async Task Can_Update_Renew_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            RenewAccount = fxAccount.CreateReceipt!.Address,
            Signatory = new Signatory(fxNft.AdminPrivateKey, fxAccount.PrivateKey)
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.RenewAccount).IsEqualTo(fxAccount.CreateReceipt!.Address);
    }

    [Test]
    public async Task Any_Account_With_Admin_Key_Can_Update()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newName = Generator.String(30, 50);

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Name = newName,
            Signatory = fxNft.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = fxAccount.PrivateKey;
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Name).IsEqualTo(newName);
    }

    [Test]
    public async Task Updates_Require_Admin_Key()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Name = Generator.String(30, 50)
        };

        var ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(updateParams);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Update Token failed with status: InvalidSignature");

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0u);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Updating_To_Used_Symbol_Is_Allowed()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var fxOther = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Symbol = fxOther.CreateParams.Symbol,
            Signatory = fxNft.AdminPrivateKey
        };
        var receipt = await client.UpdateTokenAsync(updateParams);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxOther.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0u);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Updating_To_Used_Name_Is_Allowed()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var fxOther = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Name = fxOther.CreateParams.Name,
            Signatory = fxNft.AdminPrivateKey
        };

        await client.UpdateTokenAsync(updateParams);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxOther.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0u);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Updating_To_Empty_Treasury_Raises_Error()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Treasury = EntityId.None,
            Signatory = fxNft.AdminPrivateKey
        };

        var ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(updateParams);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Update Token failed with status: InvalidAccountId");

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0u);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Make_Nft_Immutable()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var memo = Generator.Code(20);
        await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Memo = memo,
            Signatory = fxNft.AdminPrivateKey
        });
        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Memo).IsEqualTo(memo);

        await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Administrator = Endorsement.None,
            Signatory = fxNft.AdminPrivateKey
        });

        var ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxNft.CreateReceipt!.Token,
                Memo = Generator.Code(30),
                Signatory = fxNft.AdminPrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenIsImmutable);
        await Assert.That(tex.Message).StartsWith("Update Token failed with status: TokenIsImmutable");

        info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0u);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsNull();
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
        await Assert.That(info.Memo).IsEqualTo(memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Remove_Grant_KYC_Endorsement()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            GrantKycEndorsement = Endorsement.None,
            Signatory = fxNft.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.GrantKycEndorsement).IsNull();
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);

        // Check for other regressions
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0u);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Remove_Suspend_Endorsement()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            SuspendEndorsement = Endorsement.None,
            Signatory = fxNft.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.SuspendEndorsement).IsNull();
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.NotApplicable);

        // Check for other regressions
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0u);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Remove_Confiscate_Endorsement()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            ConfiscateEndorsement = Endorsement.None,
            Signatory = fxNft.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.ConfiscateEndorsement).IsNull();

        // Check for any regressions
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0u);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Remove_Supply_Endorsement()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            SupplyEndorsement = Endorsement.None,
            Signatory = fxNft.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.SupplyEndorsement).IsNull();

        // Check for other regressions
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0u);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Remove_Pause_Endorsement()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            PauseEndorsement = Endorsement.None,
            Signatory = fxNft.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.PauseEndorsement).IsNull();
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.NotApplicable);

        // Check for other regressions
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0u);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.MetadataEndorsement).IsEqualTo(fxNft.CreateParams.MetadataEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxNft.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Remove_Metadata_Endorsement()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            MetadataEndorsement = Endorsement.None,
            Signatory = fxNft.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.MetadataEndorsement).IsNull();

        // Check for other regressions
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0u);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxNft.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Remove_Royalties_Endorsement()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            RoyaltiesEndorsement = Endorsement.None,
            Signatory = fxNft.AdminPrivateKey
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.RoyaltiesEndorsement).IsNull();

        // Check for other regressions
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0u);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.MetadataEndorsement).IsEqualTo(fxNft.CreateParams.MetadataEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Cannot_Update_Imutable_Nft()
    {
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.Administrator = null);
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            SupplyEndorsement = newPublicKey,
            Signatory = new Signatory(newPrivateKey, fxNft.SupplyPrivateKey),
        };

        await client.UpdateTokenAsync(updateParams);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(new Endorsement(newPublicKey));
    }

    [Test]
    public async Task Updating_The_Treasury_Without_Signing_Raises_Error()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Treasury = fxAccount.CreateReceipt!.Address,
            Signatory = fxNft.AdminPrivateKey
        };

        var ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(updateParams);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Update Token failed with status: InvalidSignature");

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0u);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)fxNft.Metadata.Length);
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
    }

    [Test]
    public async Task Updating_The_Treasury_Without_Signing_Without_Admin_Key_Raises_Error()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
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
        await Assert.That(tex.Message).StartsWith("Update Token failed with status: InvalidSignature");

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Name).IsEqualTo(fxNft.CreateParams.Name);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0u);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)fxNft.Metadata.Length);
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Update_Treasury_To_Contract()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(ctx => ctx.Metadata = null!);
        await using var client = await TestNetwork.CreateClientAsync();

        // Note: Contract did not need to sign.
        await client.AssociateTokenAsync(fxContract.ContractReceipt!.Contract, fxNft.CreateReceipt!.Token, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxContract.PrivateKey);
        });

        var updateParams = new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Treasury = fxContract.ContractReceipt!.Contract,
            Signatory = new Signatory(fxNft.AdminPrivateKey, fxContract.PrivateKey)
        };

        var receipt = await client.UpdateTokenAsync(updateParams);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Treasury).IsEqualTo(fxContract.ContractReceipt!.Contract);
    }

    [Test]
    public async Task Removing_An_Auto_Renew_Account_Is_Not_Allowed()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = fxNft.RenewAccount.CreateReceipt!.Address,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fxNft.RenewAccount.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0u);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.RenewAccount).IsEqualTo(fxNft.CreateParams.RenewAccount);
        await Assert.That(info.RenewPeriod).IsEqualTo(fxNft.CreateParams.RenewPeriod);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxNft.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Not_Change_Treasury_To_Unassociated_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx =>
        {
            fx.CreateParams.AutoAssociationLimit = 0;
        });
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.ConfiscateEndorsement = null;
            fx.CreateParams.SuspendEndorsement = null;
            fx.CreateParams.GrantKycEndorsement = null;
        });
        await using var client = await TestNetwork.CreateClientAsync();
        var totalCirculation = (ulong)fxNft.Metadata.Length;

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsNull();
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)totalCirculation);

        // Returns A Failure
        var ex = await Assert.That(async () =>
        {
            await client.UpdateTokenAsync(new UpdateTokenParams
            {
                Token = fxNft.CreateReceipt!.Token,
                Treasury = fxAccount.CreateReceipt!.Address,
                Signatory = new Signatory(fxNft.AdminPrivateKey, fxAccount.PrivateKey)
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.NoRemainingAutomaticAssociations);

        // Confirm it did not change the Treasury Address
        var info = await client.GetTokenInfoAsync(fxNft);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
    }

    [Test]
    public async Task Can_Change_Treasury_To_Auto_Associated_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.ConfiscateEndorsement = null;
            fx.CreateParams.SuspendEndorsement = null;
            fx.CreateParams.GrantKycEndorsement = null;
        });
        await using var client = await TestNetwork.CreateClientAsync();
        var totalCirculation = (ulong)fxNft.Metadata.Length;

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsNull();
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)totalCirculation);

        await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Treasury = fxAccount.CreateReceipt!.Address,
            Signatory = new Signatory(fxNft.AdminPrivateKey, fxAccount.PrivateKey)
        });

        // Confirm it did change the Treasury Address
        var info = await client.GetTokenInfoAsync(fxNft);
        await Assert.That(info.Treasury).IsEqualTo(fxAccount.CreateReceipt!.Address);
    }

    [Test]
    public async Task Can_Schedule_Update_Nft()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newSymbol = Generator.Code(20);
        var scheduledReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new UpdateTokenParams
            {
                Token = fxNft.CreateReceipt!.Token,
                Symbol = newSymbol,
                Signatory = fxNft.AdminPrivateKey,
            },
            Payer = fxPayer
        });
        await Assert.That(scheduledReceipt.Status).IsEqualTo(ResponseCode.Success);

        var infoBefore = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(infoBefore.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);

        var executionReceipt = await client.SignScheduleAsync(scheduledReceipt.Schedule, ctx =>
        {
            ctx.Payer = fxPayer;
            ctx.Signatory = fxPayer;
        });
        var pendingReceipt = await client.GetReceiptAsync(scheduledReceipt.ScheduledTxId);
        await Assert.That(pendingReceipt.Status).IsEqualTo(ResponseCode.Success);

        var infoAfter = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(infoAfter.Symbol).IsEqualTo(newSymbol);
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Update_Nft_Metadata()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxNft = await TestNft.CreateAsync();
        var serialNumber = fxNft.MintReceipt!.SerialNumbers[0];
        var newMetadata = Generator.SHA384Hash();
        await using var client = await TestNetwork.CreateClientAsync();
        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new UpdateNftsParams
                {
                    Token = fxNft.CreateReceipt!.Token,
                    SerialNumbers = new[] { serialNumber },
                    Metadata = newMetadata,
                    Signatory = fxNft.MetadataPrivateKey,
                },
                Payer = fxPayer
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<PrecheckException>();
        await Assert.That(((PrecheckException)tex!).Status).IsEqualTo(ResponseCode.Busy);
    }
}
