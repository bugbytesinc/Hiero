using Hiero.Mirror;
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.Token;

public class TransferTokenTests
{
    [Test]
    public async Task Can_Transfer_Tokens()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        var xferAmount = 2 * fxToken.CreateParams.Circulation / 3;
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.TransferTokenAsync(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
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
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxToken.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        await Assert.That(await fxAccount.GetCryptoBalanceAsync()).IsEqualTo(fxAccount.CreateParams.InitialBalance);
        await Assert.That(await fxAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);

        await Assert.That(await fxToken.TreasuryAccount.GetCryptoBalanceAsync()).IsEqualTo(fxToken.TreasuryAccount.CreateParams.InitialBalance);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)(fxToken.CreateParams.Circulation - xferAmount));
    }

    [Test]
    public async Task Can_Transfer_Tokens_And_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        var xferAmount = 2 * fxToken.CreateParams.Circulation / 3;
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.TransferTokenAsync(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });
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
        await Assert.That(record.TokenTransfers.Count).IsEqualTo(2);
        await Assert.That(record.NftTransfers).IsEmpty();
        await Assert.That(record.Royalties).IsEmpty();
        await Assert.That(record.Associations).IsEmpty();

        var xferFrom = record.TokenTransfers.First(x => x.Account == fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(xferFrom).IsNotNull();
        await Assert.That(xferFrom.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(xferFrom.Amount).IsEqualTo(-(long)xferAmount);

        var xferTo = record.TokenTransfers.First(x => x.Account == fxAccount.CreateReceipt!.Address);
        await Assert.That(xferTo).IsNotNull();
        await Assert.That(xferFrom.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(xferTo.Amount).IsEqualTo((long)xferAmount);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
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
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxToken.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        await Assert.That(await fxAccount.GetCryptoBalanceAsync()).IsEqualTo(fxAccount.CreateParams.InitialBalance);
        await Assert.That(await fxAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);

        await Assert.That(await fxToken.TreasuryAccount.GetCryptoBalanceAsync()).IsEqualTo(fxToken.TreasuryAccount.CreateParams.InitialBalance);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)(fxToken.CreateParams.Circulation - xferAmount));
    }

    [Test]
    public async Task Can_Transfer_Tokens_And_Get_Record_With_Signatories_In_Context_Param()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        var xferAmount = 2 * fxToken.CreateParams.Circulation / 3;
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.TransferTokenAsync(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, (long)xferAmount, ctx => ctx.Signatory = new Signatory(TestNetwork.PrivateKey, fxToken.TreasuryAccount.PrivateKey));
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
        await Assert.That(record.TokenTransfers.Count).IsEqualTo(2);
        await Assert.That(record.NftTransfers).IsEmpty();
        await Assert.That(record.Royalties).IsEmpty();
        await Assert.That(record.Associations).IsEmpty();

        var xferFrom = record.TokenTransfers.First(x => x.Account == fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(xferFrom).IsNotNull();
        await Assert.That(xferFrom.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(xferFrom.Amount).IsEqualTo(-(long)xferAmount);

        var xferTo = record.TokenTransfers.First(x => x.Account == fxAccount.CreateReceipt!.Address);
        await Assert.That(xferTo).IsNotNull();
        await Assert.That(xferFrom.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(xferTo.Amount).IsEqualTo((long)xferAmount);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
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
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxToken.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        await Assert.That(await fxAccount.GetCryptoBalanceAsync()).IsEqualTo(fxAccount.CreateParams.InitialBalance);
        await Assert.That(await fxAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);

        await Assert.That(await fxToken.TreasuryAccount.GetCryptoBalanceAsync()).IsEqualTo(fxToken.TreasuryAccount.CreateParams.InitialBalance);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)(fxToken.CreateParams.Circulation - xferAmount));
    }

    [Test]
    public async Task Can_Execute_Multi_Transfer_Tokens()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        var xferAmount = fxToken.CreateParams.Circulation / 3;
        var expectedTreasury = fxToken.CreateParams.Circulation - 2 * xferAmount;
        await using var client = await TestNetwork.CreateClientAsync();
        var transfers = new TransferParams
        {
            TokenTransfers = new TokenTransfer[]
            {
                    new TokenTransfer(fxToken,fxAccount1,(long)xferAmount),
                    new TokenTransfer(fxToken,fxAccount2,(long)xferAmount),
                    new TokenTransfer(fxToken,fxToken.TreasuryAccount,-2*(long)xferAmount)
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey
        };
        var receipt = await client.TransferAsync(transfers);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
    }

    [Test]
    public async Task Can_Execute_Multi_Transfer_Tokens_With_Record()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        var xferAmount = fxToken.CreateParams.Circulation / 3;
        var expectedTreasury = fxToken.CreateParams.Circulation - 2 * xferAmount;
        await using var client = await TestNetwork.CreateClientAsync();
        var transfers = new TransferParams
        {
            TokenTransfers = new TokenTransfer[]
            {
                    new TokenTransfer(fxToken, fxAccount1, (long)xferAmount),
                    new TokenTransfer(fxToken, fxAccount2, (long)xferAmount),
                    new TokenTransfer(fxToken, fxToken.TreasuryAccount, -2 * (long)xferAmount)
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey
        };
        var receipt = await client.TransferAsync(transfers);
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
        await Assert.That(record.TokenTransfers.Count).IsEqualTo(3);
        await Assert.That(record.NftTransfers).IsEmpty();
        await Assert.That(record.Royalties).IsEmpty();
        await Assert.That(record.Associations).IsEmpty();

        var xferFrom = record.TokenTransfers.First(x => x.Account == fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(xferFrom).IsNotNull();
        await Assert.That(xferFrom.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(xferFrom.Amount).IsEqualTo(-2 * (long)xferAmount);

        var xferTo1 = record.TokenTransfers.First(x => x.Account == fxAccount1.CreateReceipt!.Address);
        await Assert.That(xferTo1).IsNotNull();
        await Assert.That(xferFrom.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(xferTo1.Amount).IsEqualTo((long)xferAmount);

        var xferTo2 = record.TokenTransfers.First(x => x.Account == fxAccount2.CreateReceipt!.Address);
        await Assert.That(xferTo2).IsNotNull();
        await Assert.That(xferFrom.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(xferTo2.Amount).IsEqualTo((long)xferAmount);

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
    }

    [Test]
    public async Task Can_Execute_Multi_Transfer_Tokens_And_Crypto()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        var tokenAmount = fxToken.CreateParams.Circulation / 3;
        var expectedTreasury = fxToken.CreateParams.Circulation - 2 * tokenAmount;
        var cryptoAmount = (long)Generator.Integer(100, 200);
        await using var client = await TestNetwork.CreateClientAsync();
        var transfers = new TransferParams
        {
            CryptoTransfers = new[]
                {
                    new CryptoTransfer( TestNetwork.Payer, -2 * cryptoAmount ),
                    new CryptoTransfer(fxAccount1, cryptoAmount ),
                    new CryptoTransfer(fxAccount2, cryptoAmount )
                },
            TokenTransfers = new TokenTransfer[]
            {
                    new TokenTransfer(fxToken,fxAccount1,(long)tokenAmount),
                    new TokenTransfer(fxToken,fxAccount2,(long)tokenAmount),
                    new TokenTransfer(fxToken,fxToken.TreasuryAccount,-2*(long)tokenAmount)
            },
            Signatory = new Signatory(TestNetwork.PrivateKey, fxToken.TreasuryAccount.PrivateKey)
        };
        var receipt = await client.TransferAsync(transfers);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.CurrentExchangeRate).IsNotNull();
        await Assert.That(receipt.NextExchangeRate).IsNotNull();
        await Assert.That(receipt.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        await Assert.That(await client.GetAccountBalanceAsync(fxAccount1)).IsEqualTo(fxAccount1.CreateParams.InitialBalance + (ulong)cryptoAmount);
        await Assert.That(await client.GetAccountBalanceAsync(fxAccount2)).IsEqualTo(fxAccount2.CreateParams.InitialBalance + (ulong)cryptoAmount);
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)tokenAmount);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)tokenAmount);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
    }

    [Test]
    public async Task Can_Execute_Multi_Transfer_Tokens_And_Crypto_With_Record()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        var tokenAmount = fxToken.CreateParams.Circulation / 3;
        var expectedTreasury = fxToken.CreateParams.Circulation - 2 * tokenAmount;
        var cryptoAmount = (long)Generator.Integer(100, 200);
        await using var client = await TestNetwork.CreateClientAsync();
        var transfers = new TransferParams
        {
            CryptoTransfers = new[]
                {
                    new CryptoTransfer( TestNetwork.Payer, -2 * cryptoAmount ),
                    new CryptoTransfer( fxAccount1, cryptoAmount ),
                    new CryptoTransfer( fxAccount2, cryptoAmount )
                },
            TokenTransfers = new TokenTransfer[]
            {
                    new TokenTransfer(fxToken,fxAccount1,(long)tokenAmount),
                    new TokenTransfer(fxToken,fxAccount2,(long)tokenAmount),
                    new TokenTransfer(fxToken,fxToken.TreasuryAccount,-2*(long)tokenAmount)
            },
            Signatory = new Signatory(TestNetwork.PrivateKey, fxToken.TreasuryAccount.PrivateKey)
        };
        var receipt = await client.TransferAsync(transfers);
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
        await Assert.That(record.Transfers.Count >= 4 && record.Transfers.Count <= 7).IsTrue();
        await Assert.That(record.TokenTransfers.Count).IsEqualTo(3);
        await Assert.That(record.NftTransfers).IsEmpty();
        await Assert.That(record.Royalties).IsEmpty();
        await Assert.That(record.Associations).IsEmpty();

        await Assert.That(record.Transfers[fxAccount1]).IsEqualTo(cryptoAmount);
        await Assert.That(record.Transfers[fxAccount2]).IsEqualTo(cryptoAmount);

        var xferFrom = record.TokenTransfers.First(x => x.Account == fxToken.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(xferFrom).IsNotNull();
        await Assert.That(xferFrom.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(xferFrom.Amount).IsEqualTo(-2 * (long)tokenAmount);

        var xferTo1 = record.TokenTransfers.First(x => x.Account == fxAccount1.CreateReceipt!.Address);
        await Assert.That(xferTo1).IsNotNull();
        await Assert.That(xferFrom.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(xferTo1.Amount).IsEqualTo((long)tokenAmount);

        var xferTo2 = record.TokenTransfers.First(x => x.Account == fxAccount2.CreateReceipt!.Address);
        await Assert.That(xferTo2).IsNotNull();
        await Assert.That(xferFrom.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(xferTo2.Amount).IsEqualTo((long)tokenAmount);

        await Assert.That(await client.GetAccountBalanceAsync(fxAccount1)).IsEqualTo(fxAccount1.CreateParams.InitialBalance + (ulong)cryptoAmount);
        await Assert.That(await client.GetAccountBalanceAsync(fxAccount2)).IsEqualTo(fxAccount2.CreateParams.InitialBalance + (ulong)cryptoAmount);
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)tokenAmount);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)tokenAmount);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
    }

    [Test]
    public async Task Can_Pass_A_Token()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        var xferAmount = fxToken.CreateParams.Circulation / 3;
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount1, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);

        receipt = await client.TransferTokenAsync(fxToken, fxAccount1, fxAccount2, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey);
        });
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
    }

    [Test]
    public async Task Receive_Signature_Requirement_Applies_To_Tokens()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync(fx =>
        {
            fx.CreateParams.RequireReceiveSignature = true;
            fx.CreateParams.Signatory = fx.PrivateKey;
        });
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        var xferAmount = fxToken.CreateParams.Circulation / 3;
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount1, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);

        var ex = await Assert.That(async () =>
        {
            await client.TransferTokenAsync(fxToken, fxAccount1, fxAccount2, (long)xferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: InvalidSignature");

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);

        receipt = await client.TransferTokenAsync(fxToken, fxAccount1, fxAccount2, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey, fxAccount2.PrivateKey);
        });

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
    }

    [Test]
    public async Task Cannot_Pass_To_A_Frozen_Account()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        var xferAmount = 2 * fxToken.CreateParams.Circulation / 3;
        await using var client = await TestNetwork.CreateClientAsync();

        await client.SuspendTokenAsync(new SuspendTokenParams
        {
            Token = fxToken,
            Holder = fxAccount2,
            Signatory = fxToken.SuspendPrivateKey
        });

        var receipt = await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount1, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);

        var ex = await Assert.That(async () =>
        {
            await client.TransferTokenAsync(fxToken, fxAccount1, fxAccount2, (long)xferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: AccountFrozenForToken");

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
    }

    [Test]
    public async Task Cannot_Pass_To_AKCY_Non_Granted_Account()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount1, fxAccount2);
        var xferAmount = 2 * fxToken.CreateParams.Circulation / 3;
        await using var client = await TestNetwork.CreateClientAsync();

        await client.GrantTokenKycAsync(new GrantTokenKycParams
        {
            Token = fxToken,
            Holder = fxAccount1,
            Signatory = fxToken.GrantPrivateKey
        });

        var receipt = await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount1, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);

        var ex = await Assert.That(async () =>
        {
            await client.TransferTokenAsync(fxToken, fxAccount1, fxAccount2, (long)xferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountKycNotGrantedForToken);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: AccountKycNotGrantedForToken");

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Transfer_Tokens_After_Resume()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = true;
        }, fxAccount1, fxAccount2);
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;
        await using var client = await TestNetwork.CreateClientAsync();

        // Address one, by default should not recievie coins.
        var ex = await Assert.That(async () =>
        {
            await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount1, (long)xferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: AccountFrozenForToken");

        // Resume Participating Accounts
        await client.ResumeTokenAsync(new ResumeTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount1,
            Signatory = fxToken.SuspendPrivateKey
        });
        await client.ResumeTokenAsync(new ResumeTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount2,
            Signatory = fxToken.SuspendPrivateKey
        });

        // Move coins to account 2 via 1
        await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount1, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });
        var receipt = await client.TransferTokenAsync(fxToken, fxAccount1, fxAccount2, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey);
        });

        // Check our Balances
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)(circulation - xferAmount));
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);

        // Suppend Address One from Receiving Coins
        await client.SuspendTokenAsync(new SuspendTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount1,
            Signatory = fxToken.SuspendPrivateKey
        });
        ex = await Assert.That(async () =>
        {
            await client.TransferTokenAsync(fxToken, fxAccount2, fxAccount1, (long)xferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey);
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: AccountFrozenForToken");

        // Can we suspend the treasury?
        await client.SuspendTokenAsync(new SuspendTokenParams
        {
            Token = fxToken,
            Holder = fxToken.TreasuryAccount,
            Signatory = fxToken.SuspendPrivateKey
        });
        ex = await Assert.That(async () =>
        {
            await client.TransferTokenAsync(fxToken, fxAccount2, fxToken.TreasuryAccount, (long)xferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey);
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: AccountFrozenForToken");

        // Double Check can't send from frozen treasury.
        ex = await Assert.That(async () =>
        {
            await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount2, (long)xferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: AccountFrozenForToken");

        // Balances should not have changed
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)(circulation - xferAmount));
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
    }

    [Test]
    public async Task Cannot_Transfer_Tokens_After_Suspend()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = false;
        }, fxAccount1, fxAccount2);
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;
        await using var client = await TestNetwork.CreateClientAsync();

        // Move coins to account 2 via 1
        await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount1, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });
        var receipt = await client.TransferTokenAsync(fxToken, fxAccount1, fxAccount2, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey);
        });

        // Check our Balances
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)(circulation - xferAmount));
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);

        // Suppend Address One from Receiving Coins
        await client.SuspendTokenAsync(new SuspendTokenParams
        {
            Token = fxToken.CreateReceipt!.Token,
            Holder = fxAccount1,
            Signatory = fxToken.SuspendPrivateKey
        });
        var ex = await Assert.That(async () =>
        {
            await client.TransferTokenAsync(fxToken, fxAccount2, fxAccount1, (long)xferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: AccountFrozenForToken");

        // Can we suspend the treasury?
        await client.SuspendTokenAsync(new SuspendTokenParams
        {
            Token = fxToken,
            Holder = fxToken.TreasuryAccount,
            Signatory = fxToken.SuspendPrivateKey
        });
        ex = await Assert.That(async () =>
        {
            await client.TransferTokenAsync(fxToken, fxAccount2, fxToken.TreasuryAccount, (long)xferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey);
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: AccountFrozenForToken");

        // Double Check can't send from frozen treasury.
        ex = await Assert.That(async () =>
        {
            await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount2, (long)xferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: AccountFrozenForToken");

        // Balances should not have changed
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)(circulation - xferAmount));
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);

        // Resume Participating Accounts
        await client.ResumeTokenAsync(new ResumeTokenParams
        {
            Token = fxToken,
            Holder = fxAccount1,
            Signatory = fxToken.SuspendPrivateKey
        });
        await client.ResumeTokenAsync(new ResumeTokenParams
        {
            Token = fxToken,
            Holder = fxToken.TreasuryAccount,
            Signatory = fxToken.SuspendPrivateKey
        });

        // Move coins to back via 1
        await client.TransferTokenAsync(fxToken, fxAccount2, fxAccount1, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey);
        });
        receipt = await client.TransferTokenAsync(fxToken, fxAccount1, fxToken.TreasuryAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey);
        });

        // Check our Final Balances
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)circulation);
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Transfer_Tokens_To_Contract()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = false;
        });
        await using var client = await TestNetwork.CreateClientAsync();
        await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxContract.ContractReceipt!.Contract,
            Tokens = [fxToken.CreateReceipt!.Token],
            Signatory = fxContract.PrivateKey
        });
        var xferAmount = fxToken.CreateParams.Circulation / 3;

        var receipt = await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxContract.ContractReceipt!.Contract, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var tokens = await fxContract.GetTokenBalancesAsync();
        var association = tokens.FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(association).IsNotNull();
        await Assert.That(association!.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo((long)xferAmount);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Transfer_Tokens_To_Contract_And_Back()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        var totalCirculation = fxToken.CreateParams.Circulation;
        var xferAmount = 2 * fxToken.CreateParams.Circulation / 3;
        var expectedTreasuryBalance = totalCirculation - xferAmount;
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxContract,
            Tokens = [fxToken],
            Signatory = fxContract.PrivateKey
        });

        var receipt = await client.TransferTokenAsync(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, fxContract.ContractReceipt!.Contract, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await Assert.That(await fxContract.GetCryptoBalanceAsync()).IsEqualTo(0UL);
        await Assert.That(await fxContract.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxContract.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);

        await Assert.That(await fxToken.TreasuryAccount.GetCryptoBalanceAsync()).IsEqualTo(fxToken.TreasuryAccount.CreateParams.InitialBalance);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasuryBalance);

        receipt = await client.TransferTokenAsync(fxToken.CreateReceipt!.Token, fxContract.ContractReceipt!.Contract, fxToken.TreasuryAccount.CreateReceipt!.Address, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxContract.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)totalCirculation);
        await Assert.That(await fxContract.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Move_Coins_By_Moving_The_Treasury()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.ConfiscateEndorsement = null;
        }, fxAccount);
        var circulation = fxToken.CreateParams.Circulation;
        var xferAmount = circulation / 3;
        var partialTreasury = circulation - xferAmount;
        await using var client = await TestNetwork.CreateClientAsync();

        // Transfer a third of the treasury to the other account.
        var receipt = await client.TransferTokenAsync(fxToken, fxToken.TreasuryAccount, fxAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        // Double check balances.
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)partialTreasury);

        // Move the treasury to an existing account
        receipt = await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxToken,
            Treasury = fxAccount,
            Signatory = new Signatory(fxToken.AdminPrivateKey, fxAccount.PrivateKey)
        });

        // All coins swept into new treasury account.
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)circulation);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);

        // What does the info say now?
        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.Fungible);
        await Assert.That(info.Symbol).IsEqualTo(fxToken.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxAccount.CreateReceipt!.Address);
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

        // Move the treasury back
        receipt = await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxToken,
            Treasury = fxToken.TreasuryAccount,
            Signatory = new Signatory(fxToken.AdminPrivateKey, fxToken.TreasuryAccount.PrivateKey)
        });

        // All coins swept back to original treasury.
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)circulation);
    }

    [Test]
    public async Task Can_Schedule_Multi_Transfer_Token_Coins()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        var xferAmount = fxToken.CreateParams.Circulation / 3;
        var expectedTreasury = fxToken.CreateParams.Circulation - 2 * xferAmount;
        await using var client = await TestNetwork.CreateClientAsync();
        var transfers = new TransferParams
        {
            TokenTransfers = new TokenTransfer[]
            {
                    new TokenTransfer(fxToken,fxAccount1,(long)xferAmount),
                    new TokenTransfer(fxToken,fxAccount2,(long)xferAmount),
                    new TokenTransfer(fxToken,fxToken.TreasuryAccount,-2*(long)xferAmount)
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey,
        };
        var schedulingReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = transfers,
            Payer = fxPayer
        });
        await Assert.That(schedulingReceipt.Status).IsEqualTo(ResponseCode.Success);

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)fxToken.CreateParams.Circulation);

        var counterReceipt = await client.SignScheduleAsync(schedulingReceipt.Schedule, ctx =>
        {
            ctx.Payer = fxPayer;
            ctx.Signatory = fxPayer;
        });
        await Assert.That(counterReceipt.Status).IsEqualTo(ResponseCode.Success);

        var transferReceipt = await client.GetReceiptAsync(schedulingReceipt.ScheduledTransactionId);
        await Assert.That(transferReceipt.Status).IsEqualTo(ResponseCode.Success);

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)expectedTreasury);
    }

    [Test]
    public async Task Can_Transfer_Tokens_To_Alias_Account()
    {
        await using var fxAccount = await TestAliasAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var client = await TestNetwork.CreateClientAsync();
        await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount,
            Tokens = [fxToken.CreateReceipt!.Token],
            Signatory = fxAccount.PrivateKey
        });
        var xferAmount = 2 * fxToken.CreateParams.Circulation / 3;

        var receipt = await client.TransferTokenAsync(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, fxAccount.Alias, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
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
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxToken.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        await Assert.That(await fxAccount.GetCryptoBalanceAsync() > 0).IsTrue();
        await Assert.That(await fxAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);

        await Assert.That(await fxToken.TreasuryAccount.GetCryptoBalanceAsync()).IsEqualTo(fxToken.TreasuryAccount.CreateParams.InitialBalance);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)(fxToken.CreateParams.Circulation - xferAmount));
    }

    [Test]
    public async Task Can_Transfer_Tokens_From_Alias_Account()
    {
        await using var fxFirstAccount = await TestAliasAccount.CreateAsync();
        await using var fxSecondAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxSecondAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxFirstAccount,
            Tokens = [fxToken.CreateReceipt!.Token],
            Signatory = fxFirstAccount.PrivateKey
        });
        var xferAmount = 2 * fxToken.CreateParams.Circulation / 3;

        var firstReceipt = await client.TransferTokenAsync(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, fxFirstAccount.Alias, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });
        await Assert.That(firstReceipt.Status).IsEqualTo(ResponseCode.Success);

        var secondReceipt = await client.TransferTokenAsync(fxToken.CreateReceipt!.Token, fxFirstAccount.Alias, fxSecondAccount, (long)xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxFirstAccount.PrivateKey);
        });
        await Assert.That(secondReceipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxToken.CreateReceipt!.Token);
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
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxToken.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxToken.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        await Assert.That(await fxFirstAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxFirstAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo(0);

        await Assert.That(await fxSecondAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxSecondAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)xferAmount);

        await Assert.That(await fxToken.TreasuryAccount.GetCryptoBalanceAsync()).IsEqualTo(fxToken.TreasuryAccount.CreateParams.InitialBalance);
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxToken.TreasuryAccount.GetTokenBalanceAsync(fxToken)).IsEqualTo((long)(fxToken.CreateParams.Circulation - xferAmount));
    }

    [Test]
    public async Task Can_Transfer_Tokens_Using_Contract_Using_Ed25519_Based_Accounts()
    {
        await using var fxTreasuryAccount = await TestAccount.CreateAsync(fx =>
        {
            var pair = Generator.Ed25519KeyPair();
            fx.PublicKey = pair.publicKey;
            fx.PrivateKey = pair.privateKey;
            fx.CreateParams.Endorsement = pair.publicKey;
        });

        await using var fxAccount = await TestAccount.CreateAsync(fx =>
        {
            var pair = Generator.Ed25519KeyPair();
            fx.PublicKey = pair.publicKey;
            fx.PrivateKey = pair.privateKey;
            fx.CreateParams.Endorsement = pair.publicKey;
        });

        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.TreasuryAccount = fxTreasuryAccount;
            fx.CreateParams.Treasury = fxTreasuryAccount.CreateReceipt!.Address;
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.RenewAccount.PrivateKey, fx.TreasuryAccount.PrivateKey);
        }, fxAccount);
        await using var fxContract = await TransferTokenContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await using var sigClient = client.Clone(ctx => ctx.SignaturePrefixTrimLimit = int.MaxValue);
        await client.TransferAsync(TestNetwork.Payer, fxTreasuryAccount, 2_00_000_000);

        long xferAmount = (long)(fxToken.CreateParams.Circulation / 3);

        await AssertHg.TokenBalanceAsync(fxToken, fxAccount, 0);

        await client.AllocateAllowanceAsync(new AllowanceParams
        {
            TokenAllowances = new[] {
                new TokenAllowance(
                    fxToken.CreateReceipt!.Token,
                    fxToken.TreasuryAccount.CreateReceipt!.Address,
                    fxContract.ContractReceipt!.Contract,
                    xferAmount)},
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });

        var receipt = await sigClient.CallContractAsync(new CallContractParams
        {
            Contract = fxContract.ContractReceipt!.Contract,
            MethodName = "transferToken",
            MethodArgs = new object[]
            {
                fxToken.CreateReceipt!.Token,
                fxToken.TreasuryAccount.CreateReceipt!.Address,
                fxAccount.CreateReceipt!.Address,
                xferAmount
            },
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        }, ctx =>
        {
            ctx.Payer = fxToken.TreasuryAccount;
            ctx.Signatory = fxToken.TreasuryAccount;
        });

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId) as CallContractRecord;
        await Assert.That(record!.Status).IsEqualTo(ResponseCode.Success);

        var result = record.Result!.Result.As<long>();
        await Assert.That(result).IsEqualTo((long)ResponseCode.Success);

        await AssertHg.TokenBalanceAsync(fxToken, fxToken.TreasuryAccount, fxToken.CreateParams.Circulation - (ulong)xferAmount);
        await AssertHg.TokenBalanceAsync(fxToken, fxAccount, (ulong)xferAmount);
    }

    [Test]
    public async Task Can_Transfer_Tokens_Using_Contract_Using_Secp256k1_Based_Accounts()
    {
        await using var fxTreasuryAccount = await TestAccount.CreateAsync(fx =>
        {
            var pair = Generator.Secp256k1KeyPair();
            fx.PublicKey = pair.publicKey;
            fx.PrivateKey = pair.privateKey;
            fx.CreateParams.Endorsement = pair.publicKey;
        });

        await using var fxAccount = await TestAccount.CreateAsync(fx =>
        {
            var pair = Generator.Secp256k1KeyPair();
            fx.PublicKey = pair.publicKey;
            fx.PrivateKey = pair.privateKey;
            fx.CreateParams.Endorsement = pair.publicKey;
        });

        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.TreasuryAccount = fxTreasuryAccount;
            fx.CreateParams.Treasury = fxTreasuryAccount.CreateReceipt!.Address;
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.RenewAccount.PrivateKey, fx.TreasuryAccount.PrivateKey);
        }, fxAccount);
        await using var fxContract = await TransferTokenContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await using var sigClient = client.Clone(ctx => ctx.SignaturePrefixTrimLimit = int.MaxValue);

        long xferAmount = (long)(fxToken.CreateParams.Circulation / 3);

        await AssertHg.TokenBalanceAsync(fxToken, fxAccount, 0);

        await client.AllocateAllowanceAsync(new AllowanceParams
        {
            TokenAllowances = new[] {
                new TokenAllowance(
                    fxToken.CreateReceipt!.Token,
                    fxToken.TreasuryAccount.CreateReceipt!.Address,
                    fxContract.ContractReceipt!.Contract,
                    xferAmount)},
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });

        var receipt = await sigClient.CallContractAsync(new CallContractParams
        {
            Contract = fxContract.ContractReceipt!.Contract,
            MethodName = "transferToken",
            MethodArgs = new object[]
            {
                fxToken.CreateReceipt!.Token,
                fxToken.TreasuryAccount.CreateReceipt!.Address,
                fxAccount.CreateReceipt!.Address,
                xferAmount
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey,
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        });

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId) as CallContractRecord;
        await Assert.That(record!.Status).IsEqualTo(ResponseCode.Success);

        var result = record.Result!.Result.As<long>();
        await Assert.That(result).IsEqualTo((long)ResponseCode.Success);

        await AssertHg.TokenBalanceAsync(fxToken, fxToken.TreasuryAccount, fxToken.CreateParams.Circulation - (ulong)xferAmount);
        await AssertHg.TokenBalanceAsync(fxToken, fxAccount, (ulong)xferAmount);
    }

    [Test]
    public async Task Can_Transfer_Tokens_Using_Contract_Using_Secp256k1_Key_Pair_Treasury()
    {
        await using var fxTreasuryAccount = await TestAccount.CreateAsync(fx =>
        {
            var pair = Generator.Secp256k1KeyPair();
            fx.PublicKey = pair.publicKey;
            fx.PrivateKey = pair.privateKey;
            fx.CreateParams.Endorsement = pair.publicKey;
        });

        await using var fxAccount = await TestAccount.CreateAsync(fx =>
        {
            var pair = Generator.Ed25519KeyPair();
            fx.PublicKey = pair.publicKey;
            fx.PrivateKey = pair.privateKey;
            fx.CreateParams.Endorsement = pair.publicKey;
        });

        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.TreasuryAccount = fxTreasuryAccount;
            fx.CreateParams.Treasury = fxTreasuryAccount.CreateReceipt!.Address;
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.RenewAccount.PrivateKey, fx.TreasuryAccount.PrivateKey);
        }, fxAccount);
        await using var fxContract = await TransferTokenContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await using var sigClient = client.Clone(ctx => ctx.SignaturePrefixTrimLimit = int.MaxValue);

        long xferAmount = (long)(fxToken.CreateParams.Circulation / 3);

        await AssertHg.TokenBalanceAsync(fxToken, fxAccount, 0);

        await client.AllocateAllowanceAsync(new AllowanceParams
        {
            TokenAllowances = new[] {
                new TokenAllowance(
                    fxToken.CreateReceipt!.Token,
                    fxToken.TreasuryAccount.CreateReceipt!.Address,
                    fxContract.ContractReceipt!.Contract,
                    xferAmount)},
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });

        var receipt = await sigClient.CallContractAsync(new CallContractParams
        {
            Contract = fxContract.ContractReceipt!.Contract,
            MethodName = "transferToken",
            MethodArgs = new object[]
            {
                fxToken.CreateReceipt!.Token,
                fxToken.TreasuryAccount.CreateReceipt!.Address,
                fxAccount.CreateReceipt!.Address,
                xferAmount
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey,
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        });

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId) as CallContractRecord;
        await Assert.That(record!.Status).IsEqualTo(ResponseCode.Success);

        var result = record.Result!.Result.As<long>();
        await Assert.That(result).IsEqualTo((long)ResponseCode.Success);

        await AssertHg.TokenBalanceAsync(fxToken, fxToken.TreasuryAccount, fxToken.CreateParams.Circulation - (ulong)xferAmount);
        await AssertHg.TokenBalanceAsync(fxToken, fxAccount, (ulong)xferAmount);
    }

    [Test]
    public async Task Can_Transfer_Tokens_Using_Contract_Using_Secp256k1_Key_Pair_Receiver()
    {
        await using var fxTreasuryAccount = await TestAccount.CreateAsync(fx =>
        {
            var pair = Generator.Ed25519KeyPair();
            fx.PublicKey = pair.publicKey;
            fx.PrivateKey = pair.privateKey;
            fx.CreateParams.Endorsement = pair.publicKey;
        });

        await using var fxAccount = await TestAccount.CreateAsync(fx =>
        {
            var pair = Generator.Secp256k1KeyPair();
            fx.PublicKey = pair.publicKey;
            fx.PrivateKey = pair.privateKey;
            fx.CreateParams.Endorsement = pair.publicKey;
        });

        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.TreasuryAccount = fxTreasuryAccount;
            fx.CreateParams.Treasury = fxTreasuryAccount.CreateReceipt!.Address;
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.RenewAccount.PrivateKey, fx.TreasuryAccount.PrivateKey);
        }, fxAccount);
        await using var fxContract = await TransferTokenContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await using var sigClient = client.Clone(ctx => ctx.SignaturePrefixTrimLimit = int.MaxValue);

        long xferAmount = (long)(fxToken.CreateParams.Circulation / 3);

        await AssertHg.TokenBalanceAsync(fxToken, fxAccount, 0);

        await client.AllocateAllowanceAsync(new AllowanceParams
        {
            TokenAllowances = new[] {
                new TokenAllowance(
                    fxToken.CreateReceipt!.Token,
                    fxToken.TreasuryAccount.CreateReceipt!.Address,
                    fxContract.ContractReceipt!.Contract,
                    xferAmount)},
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });

        var receipt = await sigClient.CallContractAsync(new CallContractParams
        {
            Contract = fxContract.ContractReceipt!.Contract,
            MethodName = "transferToken",
            MethodArgs = new object[]
            {
                fxToken.CreateReceipt!.Token,
                fxToken.TreasuryAccount.CreateReceipt!.Address,
                fxAccount.CreateReceipt!.Address,
                xferAmount
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey,
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        });

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId) as CallContractRecord;
        await Assert.That(record!.Status).IsEqualTo(ResponseCode.Success);

        var result = record.Result!.Result.As<long>();
        await Assert.That(result).IsEqualTo((long)ResponseCode.Success);

        await AssertHg.TokenBalanceAsync(fxToken, fxToken.TreasuryAccount, fxToken.CreateParams.Circulation - (ulong)xferAmount);
        await AssertHg.TokenBalanceAsync(fxToken, fxAccount, (ulong)xferAmount);
    }

    [Test]
    public async Task Can_Transfer_Tokens_Using_Contract_Using_Secp256k1_Key_List_Receiver()
    {
        await using var fxTreasuryAccount = await TestAccount.CreateAsync(fx =>
        {
            var pair = Generator.Ed25519KeyPair();
            fx.PublicKey = pair.publicKey;
            fx.PrivateKey = pair.privateKey;
            fx.CreateParams.Endorsement = pair.publicKey;
        });

        await using var fxAccount = await TestAccount.CreateAsync(fx =>
        {
            var pair = Generator.Secp256k1KeyPair();
            fx.PublicKey = pair.publicKey;
            fx.PrivateKey = pair.privateKey;
            fx.CreateParams.Endorsement = new Endorsement(1, new[] { new Endorsement(pair.publicKey) });
        });

        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.TreasuryAccount = fxTreasuryAccount;
            fx.CreateParams.Treasury = fxTreasuryAccount.CreateReceipt!.Address;
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Signatory = new Signatory(fx.AdminPrivateKey, fx.RenewAccount.PrivateKey, fx.TreasuryAccount.PrivateKey);
        }, fxAccount);
        await using var fxContract = await TransferTokenContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await using var sigClient = client.Clone(ctx => ctx.SignaturePrefixTrimLimit = int.MaxValue);

        long xferAmount = (long)(fxToken.CreateParams.Circulation / 3);

        await AssertHg.TokenBalanceAsync(fxToken, fxAccount, 0);

        await client.AllocateAllowanceAsync(new AllowanceParams
        {
            TokenAllowances = new[] {
                new TokenAllowance(
                    fxToken.CreateReceipt!.Token,
                    fxToken.TreasuryAccount.CreateReceipt!.Address,
                    fxContract.ContractReceipt!.Contract,
                    xferAmount)},
            Signatory = fxToken.TreasuryAccount.PrivateKey
        });

        var receipt = await sigClient.CallContractAsync(new CallContractParams
        {
            Contract = fxContract.ContractReceipt!.Contract,
            MethodName = "transferToken",
            MethodArgs = new object[]
            {
                fxToken.CreateReceipt!.Token,
                fxToken.TreasuryAccount.CreateReceipt!.Address,
                fxAccount.CreateReceipt!.Address,
                xferAmount
            },
            Signatory = fxToken.TreasuryAccount.PrivateKey,
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        });

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId) as CallContractRecord;
        await Assert.That(record!.Status).IsEqualTo(ResponseCode.Success);

        var result = record.Result!.Result.As<long>();
        await Assert.That(result).IsEqualTo((long)ResponseCode.Success);

        await AssertHg.TokenBalanceAsync(fxToken, fxToken.TreasuryAccount, fxToken.CreateParams.Circulation - (ulong)xferAmount);
        await AssertHg.TokenBalanceAsync(fxToken, fxAccount, (ulong)xferAmount);
    }

    [Test]
    public async Task Can_Transfer_Via_Ethereum_Transaction_From_Non_Hydrated_EVM_Account()
    {
        await using var fxToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        });
        await using var fxReceiver = await TestAccount.CreateAsync();
        var (publicKey, privateKey) = Generator.Secp256k1KeyPair();
        var initialTransferAmount = (long)(fxToken.CreateParams.Circulation / 3);
        var senderEndorsement = new Endorsement(publicKey);
        var senderEvmAddress = new EvmAddress(senderEndorsement);
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.TransferTokenAsync(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, senderEvmAddress, initialTransferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });
        var receipts = await client.GetAllReceiptsAsync(receipt.TransactionId);
        var senderHapiAddress = ((CreateAccountReceipt)receipts[1]).Address;
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var chainId = await mirror.GetChainIdAsync();
        var balanceData = (await mirror.GetAccountAsync(senderEvmAddress))!.Balances.Tokens.FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(balanceData).IsNotNull();
        var balance = balanceData!.Balance;
        await Assert.That(balance).IsEqualTo(initialTransferAmount);

        var oldBalanceData = (await mirror.GetAccountAsync(fxReceiver.CreateReceipt!.Address))!.Balances.Tokens.FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(oldBalanceData).IsNull();

        var transferAmount = initialTransferAmount / 2;

        var transaction = new EvmTransactionInput
        {
            EvmNonce = 0,
            GasPrice = 0,
            GasLimit = 50_000,
            ToEvmAddress = fxToken.CreateReceipt!.Token.CastToEvmAddress(),
            MethodName = "transfer",
            MethodParameters = [fxReceiver.CreateReceipt!.Address.CastToEvmAddress(), (BigInteger)transferAmount],
            ChainId = chainId,
        }.RlpEncode(privateKey);

        await client.ExecuteEvmTransactionAsync(new EvmTransactionParams
        {
            Transaction = transaction,
            AdditionalGasAllowance = 10_00_000_000
        }, ctx => ctx.FeeLimit = 20_00_000_000);

        mirror = await TestNetwork.GetMirrorRestClientAsync();
        var newBalanceData = (await mirror.GetAccountAsync(fxReceiver.CreateReceipt!.Address))!.Balances.Tokens.FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(balanceData).IsNotNull();
        var receiverNewBalance = newBalanceData!.Balance;
        await Assert.That(receiverNewBalance).IsEqualTo(transferAmount);
    }
}
