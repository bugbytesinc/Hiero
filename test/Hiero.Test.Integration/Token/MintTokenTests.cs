using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.Token;

public class MintTokenTests
{
    [Test]
    public async Task Can_Mint_Tokens()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxToken.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.MintTokenAsync(fxToken.CreateReceipt!.Token, fxToken.CreateParams.Circulation, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.SupplyPrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.Circulation).IsEqualTo(fxToken.CreateParams.Circulation * 2);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        // Note: we doubled the circulation
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation * 2);
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

        var expectedTreasury = 2 * fxToken.CreateParams.Circulation;
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
    }

    [Test]
    public async Task Can_Mint_Tokens_Withou_Extra_Signatory()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxToken.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.MintTokenAsync(fxToken.CreateReceipt!.Token, fxToken.CreateParams.Circulation, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.SupplyPrivateKey));
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.Circulation).IsEqualTo(fxToken.CreateParams.Circulation * 2);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        // Note: we doubled the circulation
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation * 2);
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

        var expectedTreasury = 2 * fxToken.CreateParams.Circulation;
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
    }

    [Test]
    public async Task Can_Mint_Tokens_And_Get_Record()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxToken.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.MintTokenAsync(fxToken.CreateReceipt!.Token, fxToken.CreateParams.Circulation, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.SupplyPrivateKey);
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId) as TokenRecord;
        await Assert.That(record).IsNotNull();
        await Assert.That(record!.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.Circulation).IsEqualTo(fxToken.CreateParams.Circulation * 2);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        // Note: we doubled the circulation
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation * 2);
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

        var expectedTreasury = 2 * fxToken.CreateParams.Circulation;
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
    }

    [Test]
    public async Task Can_Mint_Tokens_From_Any_Account_With_Supply_Key()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 100_00_000_000);
        await using var fxToken = await TestToken.CreateAsync();
        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxToken.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.MintTokenAsync(fxToken.CreateReceipt!.Token, fxToken.CreateParams.Circulation, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = new Signatory(fxAccount.PrivateKey, fxToken.SupplyPrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.Circulation).IsEqualTo(fxToken.CreateParams.Circulation * 2);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        // Note: we doubled the circulation
        await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation * 2);
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

        var expectedTreasury = 2 * fxToken.CreateParams.Circulation;
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
    }

    [Test]
    public async Task Mint_Token_Record_Includes_Token_Transfers()
    {
        await using var fxToken = await TestToken.CreateAsync();

        var amountToCreate = fxToken.CreateParams.Circulation / 3;
        var expectedCirculation = fxToken.CreateParams.Circulation + amountToCreate;
        var expectedTreasury = expectedCirculation;
        var treasuryMintTransfer = (long)amountToCreate;

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.MintTokenAsync(fxToken.CreateReceipt!.Token, amountToCreate, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.SupplyPrivateKey);
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId) as TokenRecord;
        await Assert.That(record).IsNotNull();
        await Assert.That(record!.TokenTransfers).HasSingleItem();
        await Assert.That(record.NftTransfers).IsEmpty();
        await Assert.That(record.Circulation).IsEqualTo(expectedCirculation);

        var xfer = record.TokenTransfers[0];
        await Assert.That(xfer.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(xfer.Account).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(xfer.Amount).IsEqualTo(treasuryMintTransfer);

        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(expectedCirculation);
    }

    [Test]
    public async Task Mint_Token_Requires_A_Positive_Amount()
    {
        await using var fxToken = await TestToken.CreateAsync();

        var amountToCreate = 0ul;
        var expectedCirculation = fxToken.CreateParams.Circulation + amountToCreate;
        var expectedTreasury = expectedCirculation;

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.MintTokenAsync(fxToken.CreateReceipt!.Token, amountToCreate, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.SupplyPrivateKey);
            });
        }).ThrowsException();
        var aoe = ex as ArgumentOutOfRangeException;
        await Assert.That(aoe).IsNotNull();
        await Assert.That(aoe!.ParamName).IsEqualTo("Amount");
        await Assert.That(aoe.Message).StartsWith("The token amount must be greater than zero.");

        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(expectedCirculation);
    }

    [Test]
    public async Task Mint_Token_Requires_Signature_By_Supply_Key()
    {
        await using var fxToken = await TestToken.CreateAsync();

        var amountToCreate = fxToken.CreateParams.Circulation / 3;
        var expectedCirculation = fxToken.CreateParams.Circulation;
        var expectedTreasury = expectedCirculation;

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.MintTokenAsync(fxToken.CreateReceipt!.Token, amountToCreate);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Mint Tokens failed with status: InvalidSignature");

        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(expectedCirculation);
    }

    [Test]
    public async Task Can_Not_Mint_More_Than_Ceiling()
    {
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.Ceiling = (long)fx.CreateParams.Circulation);

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.MintTokenAsync(fxToken.CreateReceipt!.Token, fxToken.CreateParams.Circulation, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.SupplyPrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenMaxSupplyReached);
        await Assert.That(tex.Message).StartsWith("Mint Tokens failed with status: TokenMaxSupplyReached");

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That((long)info.Circulation).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
    }

    //[Test]
    //public async Task CanScheduleMintTokenCoins()
    //{
    //    await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
    //    await using var fxToken = await TestToken.CreateAsync();

    //    await using var client = await TestNetwork.CreateClientAsync();
    //    var pendingReceipt = await client.MintTokenAsync(
    //        fxToken.CreateReceipt!.Token,
    //        fxToken.CreateParams.Circulation,
    //        ctx => ctx.Signatory = new Signatory(
    //            fxToken.SupplyPrivateKey,
    //            new PendingParams
    //            {
    //                PendingPayer = fxPayer
    //            }));

    //    await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)fxToken.CreateParams.Circulation);
    //    // This should be considered a network bug.
    //    await Assert.That(pendingReceipt.Circulation).IsEqualTo(0UL);

    //    var schedulingReceipt = await client.SignPendingTransactionAsync(pendingReceipt.Pending.Id, fxPayer.PrivateKey);
    //    await Assert.That(schedulingReceipt.Status).IsEqualTo(ResponseCode.Success);

    //    // Can get receipt for original scheduled tx.
    //    var executedReceipt = await client.GetReceiptAsync(pendingReceipt.Pending.TxId) as TokenReceipt;
    //    var expectedTreasury = 2 * fxToken.CreateParams.Circulation;
    //    await Assert.That(executedReceipt.Status).IsEqualTo(ResponseCode.Success);
    //    await Assert.That(executedReceipt.Circulation).IsEqualTo(expectedTreasury);

    //    // Can get record for original scheduled tx.
    //    var record = await client.GetTransactionRecordAsync(pendingReceipt.Pending.TxId) as TokenRecord;
    //    await Assert.That(record.Circulation).IsEqualTo(expectedTreasury);

    //    var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
    //    await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
    //    await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
    //    await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
    //    await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
    //    // Note: we doubled the circulation
    //    await Assert.That(info.Circulation).IsEqualTo(fxToken.CreateParams.Circulation * 2);
    //    await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
    //    await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
    //    await Assert.That(info.Administrator).IsEqualTo(fxToken.CreateParams.Administrator);
    //    await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxToken.CreateParams.GrantKycEndorsement);
    //    await Assert.That(info.SuspendEndorsement).IsEqualTo(fxToken.CreateParams.SuspendEndorsement);
    //    await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxToken.CreateParams.ConfiscateEndorsement);
    //    await Assert.That(info.SupplyEndorsement).IsEqualTo(fxToken.CreateParams.SupplyEndorsement);
    //    await Assert.That(info.MetadataEndorsement).IsEqualTo(fxToken.CreateParams.MetadataEndorsement);
    //    await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxToken.CreateParams.RoyaltiesEndorsement);
    //    await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
    //    await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
    //    await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
    //    await Assert.That(info.Royalties).IsEmpty();
    //    await Assert.That(info.Deleted).IsFalse();
    //    await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
    //    await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    //    await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
    //}

    [Test]
    public async Task Can_Schedule_And_Sign_Mint_Token()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var schedulingReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new MintTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Amount = 100,
            },
        });
        await Assert.That(schedulingReceipt.Schedule).IsNotEqualTo(EntityId.None);

        var signReceipt = await client.SignScheduleAsync(new SignScheduleParams
        {
            Schedule = schedulingReceipt.Schedule,
            Signatory = fxToken.SupplyPrivateKey,
        });
        await Assert.That(signReceipt.Status).IsEqualTo(ResponseCode.Success);
    }
}
