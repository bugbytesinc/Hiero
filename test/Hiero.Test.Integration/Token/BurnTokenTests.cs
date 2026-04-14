using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.Token;

public class BurnTokenTests
{
    [Test]
    public async Task Can_Burn_Tokens_Async()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxToken.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var amountToDestory = fxToken.CreateParams.Circulation / 3;
        var expectedCirculation = fxToken.CreateParams.Circulation - amountToDestory;

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.BurnTokensAsync(fxToken, amountToDestory, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.SupplyPrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.Circulation).IsEqualTo(expectedCirculation);

        var info = await client.GetTokenInfoAsync(fxToken);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(expectedCirculation);
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

        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedCirculation);
    }

    [Test]
    public async Task Can_Burn_Tokens_Async_And_Get_Record()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxToken.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var amountToDestory = fxToken.CreateParams.Circulation / 3;
        var expectedCirculation = fxToken.CreateParams.Circulation - amountToDestory;

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.BurnTokensAsync(fxToken, amountToDestory, ctx =>
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
        await Assert.That(record.Circulation).IsEqualTo(expectedCirculation);

        var info = await client.GetTokenInfoAsync(fxToken);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(expectedCirculation);
        await Assert.That(info.Decimals).IsEqualTo(fxToken.CreateParams.Decimals);
        await Assert.That(info.Ceiling).IsEqualTo(fxToken.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxToken.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxToken.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxToken.CreateParams.SuspendEndorsement);
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

        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedCirculation);
    }

    [Test]
    public async Task Can_Burn_Tokens_Async_And_Get_Record_Without_Extra_Signatory()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxToken.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var amountToDestory = fxToken.CreateParams.Circulation / 3;
        var expectedCirculation = fxToken.CreateParams.Circulation - amountToDestory;

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.BurnTokensAsync(fxToken, amountToDestory, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.SupplyPrivateKey));
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
        await Assert.That(record.Circulation).IsEqualTo(expectedCirculation);

        var info = await client.GetTokenInfoAsync(fxToken);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(expectedCirculation);
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

        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedCirculation);
    }

    [Test]
    public async Task Can_Burn_Token_Coins_From_Any_Account_With_Supply_Key()
    {
        await using var fxAccount = await TestAccount.CreateAsync(ctx => ctx.CreateParams.InitialBalance = 60_000_000_000);
        await using var fxToken = await TestToken.CreateAsync();
        await Assert.That(fxToken.CreateReceipt).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxToken.CreateReceipt!.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxToken.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var amountToDestory = fxToken.CreateParams.Circulation / 3;
        var expectedCirculation = fxToken.CreateParams.Circulation - amountToDestory;

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.BurnTokensAsync(fxToken, amountToDestory, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = new Signatory(fxAccount.PrivateKey, fxToken.SupplyPrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.Circulation).IsEqualTo(expectedCirculation);

        var info = await client.GetTokenInfoAsync(fxToken);
        await Assert.That(info.Circulation).IsEqualTo(expectedCirculation);
    }

    [Test]
    public async Task Burn_Token_Record_Includes_Token_Transfers()
    {
        await using var fxToken = await TestToken.CreateAsync();

        var amountToDestory = fxToken.CreateParams.Circulation / 3;
        var expectedCirculation = fxToken.CreateParams.Circulation - amountToDestory;

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.BurnTokensAsync(fxToken, amountToDestory, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.SupplyPrivateKey);
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId) as TokenRecord;
        await Assert.That(record).IsNotNull();
        await Assert.That(record!.TokenTransfers).HasSingleItem();
        await Assert.That(record.NftTransfers).IsEmpty();
        await Assert.That(record.Royalties).IsEmpty();
        await Assert.That(record.Associations).IsEmpty();
        await Assert.That(record.Circulation).IsEqualTo(expectedCirculation);

        var xfer = record.TokenTransfers[0];
        await Assert.That(xfer.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(xfer.Account).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(xfer.Amount).IsEqualTo(-(long)amountToDestory);

        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedCirculation);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(expectedCirculation);
    }

    [Test]
    public async Task Can_Not_Burn_More_Tokens_Than_Are_In_Circulation()
    {
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);

        var amountToDestory = fxToken.CreateParams.Circulation + 1;

        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)fxToken.CreateParams.Circulation);
        await using var client = await TestNetwork.CreateClientAsync();
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation);

        var ex = await Assert.That(async () =>
        {
            await client.BurnTokensAsync(fxToken, amountToDestory, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.SupplyPrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidTokenBurnAmount);
        await Assert.That(tex.Message).StartsWith("Burn Tokens failed with status: InvalidTokenBurnAmount");

        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)fxToken.CreateParams.Circulation);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
    }

    [Test]
    public async Task Burning_Coins_Requires_Supply_Key_To_Sign_Transaction()
    {
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);

        var amountToDestory = fxToken.CreateParams.Circulation / 3;

        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)fxToken.CreateParams.Circulation);
        await using var client = await TestNetwork.CreateClientAsync();
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation);

        var ex = await Assert.That(async () =>
        {
            await client.BurnTokensAsync(fxToken, amountToDestory);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Burn Tokens failed with status: InvalidSignature");

        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)fxToken.CreateParams.Circulation);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
    }

    [Test]
    public async Task Can_Not_Burn_More_Tokens_Than_Treasury_Has()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);

        var amountToDestory = 2 * fxToken.CreateParams.Circulation / 3;
        var amountToTransfer = amountToDestory;
        var expectedTreasury = fxToken.CreateParams.Circulation - amountToTransfer;

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferTokensAsync(fxToken, fxToken.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, (long)amountToTransfer, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)amountToTransfer);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation);

        var ex = await Assert.That(async () =>
        {
            await client.BurnTokensAsync(fxToken, amountToDestory, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.SupplyPrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InsufficientTokenBalance);
        await Assert.That(tex.Message).StartsWith("Burn Tokens failed with status: InsufficientTokenBalance");

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)amountToTransfer);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxToken)).Circulation).IsEqualTo(fxToken.CreateParams.Circulation);
    }

    //[Test]
    //public async Task CanScheduleBurnTokenCoins()
    //{
    //    await using var fxPayer = await TestAccount.CreateAsync(ctx => ctx.CreateParams.InitialBalance = 40_00_000_000);
    //    await using var fxToken = await TestToken.CreateAsync();
    //    var amountToDestory = fxToken.CreateParams.Circulation / 3 + 1;
    //    var expectedCirculation = fxToken.CreateParams.Circulation - amountToDestory;
    //    await using var client = await TestNetwork.CreateClientAsync();
    //    var pendingReceipt = await client.BurnTokensAsync(
    //            fxToken,
    //            amountToDestory,
    //            ctx => ctx.Signatory = new Signatory(
    //                fxToken.SupplyPrivateKey,
    //                new PendingParams
    //                {
    //                    PendingPayer = fxPayer
    //                }));

    //    await Assert.That(pendingReceipt.Status).IsEqualTo(ResponseCode.Success);
    //    // This should be considered a network bug.
    //    await Assert.That(pendingReceipt.Circulation).IsEqualTo(0UL);

    //    await AssertHg.TokenBalanceAsync(fxToken, fxToken.TreasuryAccount, fxToken.CreateParams.Circulation);

    //    var signingReceipt = await client.SignPendingTransactionAsync(pendingReceipt.Pending.Id, fxPayer.PrivateKey);
    //    await Assert.That(signingReceipt.Status).IsEqualTo(ResponseCode.Success);

    //    var executedReceipt = await client.GetReceiptAsync(pendingReceipt.Pending.TxId) as TokenReceipt;
    //    await Assert.That(executedReceipt.Status).IsEqualTo(ResponseCode.Success);
    //    await Assert.That(executedReceipt.Circulation).IsEqualTo(expectedCirculation);

    //    var executedRecord = await client.GetTransactionRecordAsync(pendingReceipt.Pending.TxId) as TokenRecord;
    //    await Assert.That(executedRecord.Status).IsEqualTo(ResponseCode.Success);
    //    await Assert.That(executedRecord.Circulation).IsEqualTo(expectedCirculation);

    //    await AssertHg.TokenBalanceAsync(fxToken, fxToken.TreasuryAccount, expectedCirculation);

    //    var info = await client.GetTokenInfoAsync(fxToken);
    //    await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
    //    await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
    //    await Assert.That(info.Treasury).IsEqualTo(fxToken.TreasuryAccount.CreateReceipt!.Address);
    //    await Assert.That(info.Circulation).IsEqualTo(expectedCirculation);
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
    //}

    [Test]
    public async Task Can_Schedule_And_Sign_Burn_Token()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var schedulingReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new BurnTokenParams
            {
                Token = fxToken.CreateReceipt!.Token,
                Amount = 1,
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
