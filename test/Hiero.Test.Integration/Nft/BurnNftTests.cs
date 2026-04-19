using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.NftTokens;

public class BurnNftTests
{
    [Test]
    public async Task Can_Burn_Nfts_Async()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var amountToDestory = fxNft.Metadata.Length / 3 + 1;
        var expectedCirculation = (ulong)(fxNft.Metadata.Length - amountToDestory);
        var serialNumbers = Enumerable.Range(1, amountToDestory).Select(i => (long)i);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.BurnNftsAsync(new BurnNftParams
        {
            Token = fxNft.CreateReceipt!.Token,
            SerialNumbers = [.. serialNumbers],
            Signatory = fxNft.SupplyPrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.Circulation).IsEqualTo(expectedCirculation);

        var info = await client.GetTokenInfoAsync(fxNft);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(expectedCirculation);
        await Assert.That(info.Decimals).IsEqualTo(0U);
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

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedCirculation);
    }

    [Test]
    public async Task Can_Burn_Single_Nft_Async()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var expectedCirculation = (ulong)(fxNft.Metadata.Length - 1);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.BurnNftAsync(new Hiero.Nft(fxNft.CreateReceipt!.Token, 1), ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.SupplyPrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.Circulation).IsEqualTo(expectedCirculation);

        var info = await client.GetTokenInfoAsync(fxNft);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(expectedCirculation);
        await Assert.That(info.Decimals).IsEqualTo(0U);
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

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedCirculation);
    }

    [Test]
    public async Task Can_Burn_Nfts_Async_With_Supply_Key_In_Context()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var amountToDestory = fxNft.Metadata.Length / 3 + 1;
        var expectedCirculation = (ulong)(fxNft.Metadata.Length - amountToDestory);
        var serialNumbers = Enumerable.Range(1, amountToDestory).Select(i => (long)i);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.BurnNftsAsync(new BurnNftParams
        {
            Token = fxNft.CreateReceipt!.Token,
            SerialNumbers = [.. serialNumbers],
        }, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.SupplyPrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.Circulation).IsEqualTo(expectedCirculation);

        var info = await client.GetTokenInfoAsync(fxNft);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(expectedCirculation);
        await Assert.That(info.Decimals).IsEqualTo(0U);
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

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedCirculation);
    }

    [Test]
    public async Task Can_Burn_Nfts_Async_And_Get_Record()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var amountToDestory = fxNft.Metadata.Length / 3 + 1;
        var expectedCirculation = (ulong)(fxNft.Metadata.Length - amountToDestory);
        var serialNumbers = Enumerable.Range(1, amountToDestory).Select(i => (long)i);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.BurnNftsAsync(new BurnNftParams
        {
            Token = fxNft.CreateReceipt!.Token,
            SerialNumbers = [.. serialNumbers],
            Signatory = fxNft.SupplyPrivateKey
        });
        var record = (TokenRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsGreaterThanOrEqualTo(0UL);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.Circulation).IsEqualTo(expectedCirculation);

        var info = await client.GetTokenInfoAsync(fxNft);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(expectedCirculation);
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
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedCirculation);
    }

    [Test]
    public async Task Can_Burn_Single_Nft()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var expectedCirculation = (ulong)(fxNft.Metadata.Length - 1);
        var nft = new Hiero.Nft(fxNft, 1);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.BurnNftAsync(nft, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.SupplyPrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.Circulation).IsEqualTo(expectedCirculation);

        var info = await client.GetTokenInfoAsync(fxNft);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(expectedCirculation);
        await Assert.That(info.Decimals).IsEqualTo(0U);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
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

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedCirculation);
    }

    [Test]
    public async Task Can_Burn_Single_Nft_Async_And_Get_Record()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var expectedCirculation = (ulong)(fxNft.Metadata.Length - 1);
        var nft = new Hiero.Nft(fxNft, 1);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.BurnNftsAsync(new BurnNftParams
        {
            Token = nft.Token,
            SerialNumbers = [nft.SerialNumber],
            Signatory = fxNft.SupplyPrivateKey
        });
        var record = (TokenRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsGreaterThanOrEqualTo(0UL);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.Circulation).IsEqualTo(expectedCirculation);
        await Assert.That(record.ParentTransactionConsensus).IsNull();

        var info = await client.GetTokenInfoAsync(fxNft);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(expectedCirculation);
        await Assert.That(info.Decimals).IsEqualTo(0U);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedCirculation);
    }

    [Test]
    public async Task Can_Burn_Nfts_Async_And_Get_Record_Without_Extra_Signatory()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var amountToDestory = fxNft.Metadata.Length / 3 + 1;
        var expectedCirculation = (ulong)(fxNft.Metadata.Length - amountToDestory);
        var serialNumbers = Enumerable.Range(1, amountToDestory).Select(i => (long)i).ToArray();

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.BurnNftsAsync(new BurnNftParams
        {
            Token = fxNft.CreateReceipt!.Token,
            SerialNumbers = serialNumbers,
            Signatory = new Signatory(fxNft.SupplyPrivateKey)
        });
        var record = (TokenRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsGreaterThanOrEqualTo(0UL);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.Circulation).IsEqualTo(expectedCirculation);

        var info = await client.GetTokenInfoAsync(fxNft);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(expectedCirculation);
        await Assert.That(info.Decimals).IsEqualTo(0U);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedCirculation);
    }

    [Test]
    public async Task Can_Burn_Single_Nft_And_Get_Record_Without_Extra_Signatory()
    {
        await using var fxNft = await TestNft.CreateAsync();
        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var expectedCirculation = (ulong)(fxNft.Metadata.Length - 1);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.BurnNftAsync(new Hiero.Nft(fxNft.CreateReceipt!.Token, 1), ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.SupplyPrivateKey);
        });
        var record = (TokenRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsGreaterThanOrEqualTo(0UL);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.Circulation).IsEqualTo(expectedCirculation);

        var info = await client.GetTokenInfoAsync(fxNft);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(expectedCirculation);
        await Assert.That(info.Decimals).IsEqualTo(0U);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedCirculation);
    }

    [Test]
    public async Task Can_Burn_Nft_Coins_From_Any_Account_With_Supply_Key()
    {
        await using var fxAccount = await TestAccount.CreateAsync(ctx => ctx.CreateParams.InitialBalance = 60_000_000_000);
        await using var fxNft = await TestNft.CreateAsync();
        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var amountToDestory = fxNft.Metadata.Length / 3 + 1;
        var expectedCirculation = (ulong)(fxNft.Metadata.Length - amountToDestory);
        var serialNumbers = Enumerable.Range(1, amountToDestory).Select(i => (long)i);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.BurnNftsAsync(new BurnNftParams
        {
            Token = fxNft.CreateReceipt!.Token,
            SerialNumbers = [.. serialNumbers],
            Signatory = fxNft.SupplyPrivateKey
        }, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = fxAccount.PrivateKey;
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.Circulation).IsEqualTo(expectedCirculation);

        var info = await client.GetTokenInfoAsync(fxNft);
        await Assert.That(info.Circulation).IsEqualTo(expectedCirculation);
    }

    [Test]
    public async Task Burn_Nft_Record_Includes_Nft_Transfers()
    {
        await using var fxNft = await TestNft.CreateAsync();

        var amountToDestroy = fxNft.Metadata.Length / 3 + 1;
        var expectedCirculation = (ulong)(fxNft.Metadata.Length - amountToDestroy);
        var serialNumbers = Enumerable.Range(1, amountToDestroy).Select(i => (long)i);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.BurnNftsAsync(new BurnNftParams
        {
            Token = fxNft.CreateReceipt!.Token,
            SerialNumbers = [.. serialNumbers],
            Signatory = fxNft.SupplyPrivateKey
        });
        var record = (TokenRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.TokenTransfers).IsEmpty();
        await Assert.That(record.NftTransfers.Count).IsEqualTo(amountToDestroy);
        await Assert.That(record.Royalties).IsEmpty();
        await Assert.That(record.Associations).IsEmpty();
        await Assert.That(record.Circulation).IsEqualTo(expectedCirculation);

        for (int ssn = 1; ssn <= amountToDestroy; ssn++)
        {
            var nft = new Hiero.Nft(fxNft.CreateReceipt!.Token, ssn);
            var xfer = record.NftTransfers.FirstOrDefault(x => x.Nft == nft);
            await Assert.That(xfer).IsNotNull();
            await Assert.That(xfer!.Sender).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
            await Assert.That(xfer.Receiver).IsEqualTo(EntityId.None);
        }

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedCirculation);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(expectedCirculation);
    }

    [Test]
    public async Task Can_Not_Burn_More_Nfts_Than_Are_In_Circulation()
    {
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);

        var serialNumbers = Enumerable.Range(1, fxNft.Metadata.Length + 1).Select(i => (long)i);

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length);
        await Assert.That((await (await TestNetwork.CreateClientAsync()).GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.BurnNftsAsync(new BurnNftParams
            {
                Token = fxNft.CreateReceipt!.Token,
                SerialNumbers = [.. serialNumbers],
                Signatory = fxNft.SupplyPrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidNftId);
        await Assert.That(tex.Message).StartsWith("Burn NFT failed with status: InvalidNftId");

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)fxNft.Metadata.Length);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
    }

    [Test]
    public async Task Burning_Coins_Requires_Supply_Key_To_Sign_Transaction()
    {
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);

        var amountToDestroy = fxNft.Metadata.Length / 3 + 1;
        var serialNumbers = Enumerable.Range(1, amountToDestroy).Select(i => (long)i);

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length);
        await Assert.That((await (await TestNetwork.CreateClientAsync()).GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.BurnNftsAsync(new BurnNftParams
            {
                Token = fxNft.CreateReceipt!.Token,
                SerialNumbers = [.. serialNumbers],
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Burn NFT failed with status: InvalidSignature");

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
    }

    [Test]
    public async Task Can_Not_Burn_More_Nfts_Than_Treasury_Has()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);

        var amountToDestroy = 2 * fxNft.Metadata.Length / 3;
        var amountToTransfer = amountToDestroy;
        var expectedTreasury = fxNft.Metadata.Length - amountToTransfer;
        var serialNumbersDestroyed = Enumerable.Range(1, amountToDestroy).Select(i => (long)i);
        var serialNumbersTransfered = Enumerable.Range(amountToDestroy / 2, amountToTransfer).Select(i => (long)i);

        await using var client = await TestNetwork.CreateClientAsync();
        var transferParams = new TransferParams
        {
            NftTransfers = serialNumbersTransfered.Select(sn => new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, sn), fxNft.TreasuryAccount, fxAccount)),
            Signatory = fxNft.TreasuryAccount.PrivateKey
        };
        var receipt = await client.TransferAsync(transferParams);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(amountToTransfer);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);

        var ex = await Assert.That(async () =>
        {
            await client.BurnNftsAsync(new BurnNftParams
            {
                Token = fxNft.CreateReceipt!.Token,
                SerialNumbers = [.. serialNumbersDestroyed],
                Signatory = fxNft.SupplyPrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TreasuryMustOwnBurnedNft);
        await Assert.That(tex.Message).StartsWith("Burn NFT failed with status: TreasuryMustOwnBurnedNft");

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(amountToTransfer);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
    }

    [Test]
    public async Task Can_Not_Burn_An_Nft_The_Treasury_Does_Not_Own()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);

        var amountToTransfer = 2 * fxNft.Metadata.Length / 3;
        var expectedTreasury = fxNft.Metadata.Length - amountToTransfer;
        var serialNumbersTransfered = Enumerable.Range(1, amountToTransfer).Select(i => (long)i);

        await using var client = await TestNetwork.CreateClientAsync();
        var transferParams = new TransferParams
        {
            NftTransfers = serialNumbersTransfered.Select(sn => new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, sn), fxNft.TreasuryAccount, fxAccount)),
            Signatory = fxNft.TreasuryAccount.PrivateKey
        };
        var receipt = await client.TransferAsync(transferParams);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(amountToTransfer);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);

        var ex = await Assert.That(async () =>
        {
            await client.BurnNftsAsync(new BurnNftParams
            {
                Token = fxNft.CreateReceipt!.Token,
                SerialNumbers = [1],
                Signatory = fxNft.SupplyPrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TreasuryMustOwnBurnedNft);
        await Assert.That(tex.Message).StartsWith("Burn NFT failed with status: TreasuryMustOwnBurnedNft");

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(amountToTransfer);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
    }

    [Test]
    public async Task Can_Not_Burn_Single_Nft_The_Treasury_Does_Not_Own()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);

        var amountToTransfer = 2 * fxNft.Metadata.Length / 3;
        var expectedTreasury = fxNft.Metadata.Length - amountToTransfer;
        var serialNumbersTransfered = Enumerable.Range(1, amountToTransfer).Select(i => (long)i);

        await using var client = await TestNetwork.CreateClientAsync();
        var transferParams = new TransferParams
        {
            NftTransfers = serialNumbersTransfered.Select(sn => new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, sn), fxNft.TreasuryAccount, fxAccount)),
            Signatory = fxNft.TreasuryAccount.PrivateKey
        };
        var receipt = await client.TransferAsync(transferParams);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(amountToTransfer);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);

        var ex = await Assert.That(async () =>
        {
            await client.BurnNftAsync(new Hiero.Nft(fxNft.CreateReceipt!.Token, 1), ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.SupplyPrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TreasuryMustOwnBurnedNft);
        await Assert.That(tex.Message).StartsWith("Burn NFT failed with status: TreasuryMustOwnBurnedNft");

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(amountToTransfer);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
    }

    [Test]
    public async Task Can_Schedule_Burn_Nft_Coins()
    {
        await using var fxPayer = await TestAccount.CreateAsync(ctx => ctx.CreateParams.InitialBalance = 40_00_000_000);
        await using var fxNft = await TestNft.CreateAsync();
        var amountToDestory = fxNft.Metadata.Length / 3 + 1;
        var expectedCirculation = (ulong)(fxNft.Metadata.Length - amountToDestory);
        var serialNumbers = Enumerable.Range(1, amountToDestory).Select(i => (long)i);

        await using var client = await TestNetwork.CreateClientAsync();
        var pendingReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new BurnNftParams
            {
                Token = fxNft.CreateReceipt!.Token,
                SerialNumbers = [.. serialNumbers],
                Signatory = fxNft.SupplyPrivateKey,
            },
            Payer = fxPayer
        });

        await Assert.That(pendingReceipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.NftBalanceAsync(fxNft, fxNft.TreasuryAccount, (ulong)fxNft.Metadata.Length);

        var signingReceipt = await client.SignScheduleAsync(pendingReceipt.Schedule, ctx =>
        {
            ctx.Payer = fxPayer;
            ctx.Signatory = fxPayer.PrivateKey;
        });
        await Assert.That(signingReceipt.Status).IsEqualTo(ResponseCode.Success);

        var executedReceipt = await client.GetReceiptAsync(pendingReceipt.ScheduledTransactionId) as TokenReceipt;
        await Assert.That(executedReceipt!.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(executedReceipt.Circulation).IsEqualTo(expectedCirculation);

        var executedRecord = await client.GetTransactionRecordAsync(pendingReceipt.ScheduledTransactionId) as TokenRecord;
        await Assert.That(executedRecord!.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(executedRecord.Circulation).IsEqualTo(expectedCirculation);

        await AssertHg.NftBalanceAsync(fxNft, fxNft.TreasuryAccount, expectedCirculation);

        var info = await client.GetTokenInfoAsync(fxNft);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo(expectedCirculation);
        await Assert.That(info.Decimals).IsEqualTo(0U);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.PauseEndorsement).IsEqualTo(fxNft.CreateParams.PauseEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
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
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Burn_Nft()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxNft = await TestNft.CreateAsync();
        var serialNumbers = new long[] { fxNft.MintReceipt!.SerialNumbers[0] };
        await using var client = await TestNetwork.CreateClientAsync();
        var scheduledReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new BurnNftParams
            {
                Token = fxNft.CreateReceipt!.Token,
                SerialNumbers = serialNumbers,
                Signatory = fxNft.SupplyPrivateKey,
            },
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

        var executedReceipt = await client.GetReceiptAsync(scheduledReceipt.ScheduledTransactionId) as TokenReceipt;
        await Assert.That(executedReceipt).IsNotNull();
        await Assert.That(executedReceipt!.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(executedReceipt.Circulation).IsEqualTo((ulong)(fxNft.Metadata.Length - 1));
    }
}
