using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.NftTokens;

public class TransferNftTests
{
    [Test]
    public async Task Can_Transfer_Nfts()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var nft = new Hiero.Nft(fxNft.CreateReceipt!.Token, 1);
        var receipt = await client.TransferNftAsync(nft, fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0U);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxNft.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        await Assert.That(await fxAccount.GetCryptoBalanceAsync()).IsEqualTo(fxAccount.CreateParams.InitialBalance);
        await Assert.That(await fxAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);

        await Assert.That(await fxNft.TreasuryAccount.GetCryptoBalanceAsync()).IsEqualTo(fxNft.TreasuryAccount.CreateParams.InitialBalance);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length - 1);
    }

    [Test]
    public async Task Can_Transfer_Single_Nft()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var nft = new Hiero.Nft(fxNft.CreateReceipt!.Token, 1);
        var receipt = await client.TransferNftAsync(nft, fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0U);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxNft.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        await Assert.That(await fxAccount.GetCryptoBalanceAsync()).IsEqualTo(fxAccount.CreateParams.InitialBalance);
        await Assert.That(await fxAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);

        await Assert.That(await fxNft.TreasuryAccount.GetCryptoBalanceAsync()).IsEqualTo(fxNft.TreasuryAccount.CreateParams.InitialBalance);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length - 1);
    }

    [Test]
    public async Task Can_Transfer_Nfts_To_Alias_Account()
    {
        await using var fxAccount = await TestAliasAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount,
            Tokens = [fxNft.CreateReceipt!.Token],
            Signatory = fxAccount.PrivateKey
        });

        var nft = new Hiero.Nft(fxNft.CreateReceipt!.Token, 1);
        var receipt = await client.TransferNftAsync(nft, fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.Alias, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
        await Assert.That(info.Decimals).IsEqualTo(0U);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Administrator).IsEqualTo(fxNft.CreateParams.Administrator);
        await Assert.That(info.GrantKycEndorsement).IsEqualTo(fxNft.CreateParams.GrantKycEndorsement);
        await Assert.That(info.SuspendEndorsement).IsEqualTo(fxNft.CreateParams.SuspendEndorsement);
        await Assert.That(info.ConfiscateEndorsement).IsEqualTo(fxNft.CreateParams.ConfiscateEndorsement);
        await Assert.That(info.SupplyEndorsement).IsEqualTo(fxNft.CreateParams.SupplyEndorsement);
        await Assert.That(info.RoyaltiesEndorsement).IsEqualTo(fxNft.CreateParams.RoyaltiesEndorsement);
        await Assert.That(info.TradableStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.PauseStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(info.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(info.Royalties).IsEmpty();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Memo).IsEqualTo(fxNft.CreateParams.Memo);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();

        await Assert.That(await fxAccount.GetCryptoBalanceAsync() > 0).IsTrue();
        await Assert.That(await fxAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);

        await Assert.That(await fxNft.TreasuryAccount.GetCryptoBalanceAsync()).IsEqualTo(fxNft.TreasuryAccount.CreateParams.InitialBalance);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length - 1);
    }

    [Test]
    public async Task Can_Transfer_Nft_And_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var nft = new Hiero.Nft(fxNft.CreateReceipt!.Token, 1);

        var receipt = await client.TransferNftAsync(nft, fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee >= 0UL && record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.TokenTransfers).IsEmpty();
        await Assert.That(record.NftTransfers).HasSingleItem();
        await Assert.That(record.Royalties).IsEmpty();
        await Assert.That(record.Associations).IsEmpty();

        var xfer = record.NftTransfers.First(x => x.To == fxAccount.CreateReceipt!.Address);
        await Assert.That(xfer).IsNotNull();
        await Assert.That(xfer.Nft.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(xfer.Nft.SerialNumber).IsEqualTo(1);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
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

        await Assert.That(await fxAccount.GetCryptoBalanceAsync()).IsEqualTo(fxAccount.CreateParams.InitialBalance);
        await Assert.That(await fxAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);

        await Assert.That(await fxNft.TreasuryAccount.GetCryptoBalanceAsync()).IsEqualTo(fxNft.TreasuryAccount.CreateParams.InitialBalance);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length - 1);
    }

    [Test]
    public async Task Can_Transfer_Nft_And_Get_Record_With_Signatory_In_Context()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var nft = new Hiero.Nft(fxNft.CreateReceipt!.Token, 1);

        var receipt = await client.TransferNftAsync(nft, fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee >= 0UL && record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.TokenTransfers).IsEmpty();
        await Assert.That(record.NftTransfers).HasSingleItem();
        await Assert.That(record.Royalties).IsEmpty();
        await Assert.That(record.Associations).IsEmpty();

        var xfer = record.NftTransfers.First(x => x.To == fxAccount.CreateReceipt!.Address);
        await Assert.That(xfer).IsNotNull();
        await Assert.That(xfer.Nft.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(xfer.Nft.SerialNumber).IsEqualTo(1);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
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

        await Assert.That(await fxAccount.GetCryptoBalanceAsync()).IsEqualTo(fxAccount.CreateParams.InitialBalance);
        await Assert.That(await fxAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);

        await Assert.That(await fxNft.TreasuryAccount.GetCryptoBalanceAsync()).IsEqualTo(fxNft.TreasuryAccount.CreateParams.InitialBalance);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length - 1);
    }

    [Test]
    public async Task Can_Transfer_Nfts_And_Get_Record_With_Signatories_In_Context_Param()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var nft = new Hiero.Nft(fxNft.CreateReceipt!.Token, 1);

        var receipt = await client.TransferNftAsync(nft, fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee >= 0UL && record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.TokenTransfers).IsEmpty();
        await Assert.That(record.NftTransfers).HasSingleItem();
        await Assert.That(record.Royalties).IsEmpty();
        await Assert.That(record.Associations).IsEmpty();

        var xfer = record.NftTransfers.First(x => x.To == fxAccount.CreateReceipt!.Address);
        await Assert.That(xfer).IsNotNull();
        await Assert.That(xfer.Nft.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(xfer.Nft.SerialNumber).IsEqualTo(1);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
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

        await Assert.That(await fxAccount.GetCryptoBalanceAsync()).IsEqualTo(fxAccount.CreateParams.InitialBalance);
        await Assert.That(await fxAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);

        await Assert.That(await fxNft.TreasuryAccount.GetCryptoBalanceAsync()).IsEqualTo(fxNft.TreasuryAccount.CreateParams.InitialBalance);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length - 1);
    }

    [Test]
    public async Task Can_Execute_Multi_Transfer_Nfts()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        await using var client = await TestNetwork.CreateClientAsync();

        var transfers = new TransferParams
        {
            NftTransfers = new NftTransfer[]
            {
                new NftTransfer(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount, fxAccount1),
                new NftTransfer(new Hiero.Nft(fxNft, 2), fxNft.TreasuryAccount, fxAccount2)
            },
            Signatory = fxNft.TreasuryAccount.PrivateKey
        };
        var receipt = await client.TransferAsync(transfers);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length - 2);
    }

    [Test]
    public async Task Can_Execute_Multi_Transfer_Nfts_With_Record()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        await using var client = await TestNetwork.CreateClientAsync();

        var transfers = new TransferParams
        {
            NftTransfers = new NftTransfer[]
            {
                new NftTransfer(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount, fxAccount1),
                new NftTransfer(new Hiero.Nft(fxNft, 2), fxNft.TreasuryAccount, fxAccount2)
            },
            Signatory = fxNft.TreasuryAccount.PrivateKey
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
        await Assert.That(record.Fee >= 0UL && record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.NftTransfers.Count).IsEqualTo(2);
        await Assert.That(record.ParentTransactionConsensus).IsNull();

        var xferTo1 = record.NftTransfers.First(x => x.To == fxAccount1.CreateReceipt!.Address);
        await Assert.That(xferTo1).IsNotNull();
        await Assert.That(xferTo1.Nft.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(xferTo1.From).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(xferTo1.Nft.SerialNumber).IsEqualTo(1U);

        var xferTo2 = record.NftTransfers.First(x => x.To == fxAccount2.CreateReceipt!.Address);
        await Assert.That(xferTo2).IsNotNull();
        await Assert.That(xferTo2.Nft.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(xferTo2.From).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(xferTo2.Nft.SerialNumber).IsEqualTo(2U);

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length - 2);
    }

    [Test]
    public async Task Can_Execute_Multi_Transfer_Nfts_And_Crypto()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        await using var client = await TestNetwork.CreateClientAsync();

        var cryptoAmount = (long)Generator.Integer(100, 200);
        var transfers = new TransferParams
        {
            CryptoTransfers = new[]
            {
                new CryptoTransfer(TestNetwork.Payer, -2 * cryptoAmount),
                new CryptoTransfer(fxAccount1, cryptoAmount),
                new CryptoTransfer(fxAccount2, cryptoAmount)
            },
            NftTransfers = new NftTransfer[]
            {
                new NftTransfer(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount, fxAccount1),
                new NftTransfer(new Hiero.Nft(fxNft, 2), fxNft.TreasuryAccount, fxAccount2)
            },
            Signatory = new Signatory(TestNetwork.PrivateKey, fxNft.TreasuryAccount.PrivateKey)
        };
        var receipt = await client.TransferAsync(transfers);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.CurrentExchangeRate).IsNotNull();
        await Assert.That(receipt.NextExchangeRate).IsNotNull();
        await Assert.That(receipt.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        await Assert.That(await client.GetAccountBalanceAsync(fxAccount1)).IsEqualTo(fxAccount1.CreateParams.InitialBalance + (ulong)cryptoAmount);
        await Assert.That(await client.GetAccountBalanceAsync(fxAccount2)).IsEqualTo(fxAccount2.CreateParams.InitialBalance + (ulong)cryptoAmount);
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length - 2);
    }

    [Test]
    public async Task Can_Execute_Multi_Transfer_Nfts_And_Crypto_With_Record()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        await using var client = await TestNetwork.CreateClientAsync();

        var cryptoAmount = (long)Generator.Integer(100, 200);
        var transfers = new TransferParams
        {
            CryptoTransfers = new[]
            {
                new CryptoTransfer(TestNetwork.Payer, -2 * cryptoAmount),
                new CryptoTransfer(fxAccount1, cryptoAmount),
                new CryptoTransfer(fxAccount2, cryptoAmount)
            },
            NftTransfers = new NftTransfer[]
            {
                new NftTransfer(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount, fxAccount1),
                new NftTransfer(new Hiero.Nft(fxNft, 2), fxNft.TreasuryAccount, fxAccount2)
            },
            Signatory = new Signatory(TestNetwork.PrivateKey, fxNft.TreasuryAccount.PrivateKey)
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
        await Assert.That(record.Fee >= 0UL && record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.Transfers.Count >= 4 && record.Transfers.Count <= 7).IsTrue();
        await Assert.That(record.NftTransfers.Count).IsEqualTo(2);

        await Assert.That(record.Transfers[fxAccount1]).IsEqualTo(cryptoAmount);
        await Assert.That(record.Transfers[fxAccount2]).IsEqualTo(cryptoAmount);

        var xferTo1 = record.NftTransfers.First(x => x.To == fxAccount1.CreateReceipt!.Address);
        await Assert.That(xferTo1).IsNotNull();
        await Assert.That(xferTo1.Nft.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(xferTo1.From).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(xferTo1.Nft.SerialNumber).IsEqualTo(1U);

        var xferTo2 = record.NftTransfers.First(x => x.To == fxAccount2.CreateReceipt!.Address);
        await Assert.That(xferTo2).IsNotNull();
        await Assert.That(xferTo2.Nft.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(xferTo2.From).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(xferTo2.Nft.SerialNumber).IsEqualTo(2U);

        await Assert.That(await client.GetAccountBalanceAsync(fxAccount1)).IsEqualTo(fxAccount1.CreateParams.InitialBalance + (ulong)cryptoAmount);
        await Assert.That(await client.GetAccountBalanceAsync(fxAccount2)).IsEqualTo(fxAccount2.CreateParams.InitialBalance + (ulong)cryptoAmount);
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length - 2);
    }

    [Test]
    public async Task Can_Pass_An_Nft()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        await using var client = await TestNetwork.CreateClientAsync();

        var nft = new Hiero.Nft(fxNft, 1);

        var receipt = await client.TransferNftAsync(nft, fxNft.TreasuryAccount, fxAccount1, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);

        receipt = await client.TransferNftAsync(nft, fxAccount1, fxAccount2, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey);
        });
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
    }

    [Test]
    public async Task Receive_Signature_Requirement_Applies_To_Nfts()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync(fx =>
        {
            fx.CreateParams.RequireReceiveSignature = true;
            fx.CreateParams.Signatory = fx.PrivateKey;
        });
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        await using var client = await TestNetwork.CreateClientAsync();

        var nft = new Hiero.Nft(fxNft, 1);

        var receipt = await client.TransferNftAsync(nft, fxNft.TreasuryAccount, fxAccount1, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);

        var ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(nft, fxAccount1, fxAccount2, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: InvalidSignature");

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);

        receipt = await client.TransferNftAsync(nft, fxAccount1, fxAccount2, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey, fxAccount2.PrivateKey);
        });
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
    }

    [Test]
    public async Task Cannot_Pass_To_A_Frozen_Account()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        await using var client = await TestNetwork.CreateClientAsync();

        var nft = new Hiero.Nft(fxNft, 1);

        await client.SuspendTokenAsync(new SuspendTokenParams { Token = fxNft, Holder = fxAccount2, Signatory = fxNft.SuspendPrivateKey });
        await client.TransferNftAsync(nft, fxNft.TreasuryAccount, fxAccount1, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);

        var ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(nft, fxAccount1, fxAccount2, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: AccountFrozenForToken");

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
    }

    [Test]
    public async Task Cannot_Pass_To_AKCY_Non_Granted_Account()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(null, fxAccount1, fxAccount2);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.GrantTokenKycAsync(new GrantTokenKycParams { Token = fxNft, Holder = fxAccount1, Signatory = fxNft.GrantPrivateKey });

        var nft = new Hiero.Nft(fxNft, 1);

        var receipt = await client.TransferNftAsync(nft, fxNft.TreasuryAccount, fxAccount1, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);

        var ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(nft, fxAccount1, fxAccount2, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountKycNotGrantedForToken);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: AccountKycNotGrantedForToken");

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Transfer_Nfts_After_Resume()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = true;
        }, fxAccount1, fxAccount2);
        await using var client = await TestNetwork.CreateClientAsync();

        var nft = new Hiero.Nft(fxNft, 1);

        // Account one, by default should not receive coins.
        var ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(nft, fxNft.TreasuryAccount, fxAccount1, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: AccountFrozenForToken");

        // Resume Participating Accounts
        await client.ResumeTokenAsync(new ResumeTokenParams { Token = fxNft.CreateReceipt!.Token, Holder = fxAccount1, Signatory = fxNft.SuspendPrivateKey });
        await client.ResumeTokenAsync(new ResumeTokenParams { Token = fxNft.CreateReceipt!.Token, Holder = fxAccount2, Signatory = fxNft.SuspendPrivateKey });

        // Move coins to account 2 via 1
        await client.TransferNftAsync(nft, fxNft.TreasuryAccount, fxAccount1, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });
        var receipt = await client.TransferNftAsync(nft, fxAccount1, fxAccount2, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey);
        });

        // Check our Balances
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length - 1);

        // Suspend Account One from Receiving Coins
        await client.SuspendTokenAsync(new SuspendTokenParams { Token = fxNft.CreateReceipt!.Token, Holder = fxAccount1, Signatory = fxNft.SuspendPrivateKey });
        ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(nft, fxAccount2, fxAccount1, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey);
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: AccountFrozenForToken");

        // Can we suspend the treasury?
        await client.SuspendTokenAsync(new SuspendTokenParams { Token = fxNft, Holder = fxNft.TreasuryAccount, Signatory = fxNft.SuspendPrivateKey });
        ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(nft, fxAccount2, fxNft.TreasuryAccount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey);
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: AccountFrozenForToken");

        // Double Check can't send from frozen treasury.
        ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(new Hiero.Nft(fxNft, 2), fxNft.TreasuryAccount, fxAccount2, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: AccountFrozenForToken");

        // Balances should not have changed
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length - 1);
    }

    [Test]
    public async Task Cannot_Transfer_Nfts_After_Suspend()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = false;
        }, fxAccount1, fxAccount2);
        await using var client = await TestNetwork.CreateClientAsync();

        var nft = new Hiero.Nft(fxNft, 1);

        // Move coins to account 2 via 1
        await client.TransferNftAsync(nft, fxNft.TreasuryAccount, fxAccount1, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });
        var receipt = await client.TransferNftAsync(nft, fxAccount1, fxAccount2, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey);
        });

        // Check our Balances
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length - 1);

        // Suspend Account One from Receiving Coins
        await client.SuspendTokenAsync(new SuspendTokenParams { Token = fxNft.CreateReceipt!.Token, Holder = fxAccount1, Signatory = fxNft.SuspendPrivateKey });
        var ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(nft, fxAccount2, fxAccount1, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: AccountFrozenForToken");

        // Can we suspend the treasury?
        await client.SuspendTokenAsync(new SuspendTokenParams { Token = fxNft, Holder = fxNft.TreasuryAccount, Signatory = fxNft.SuspendPrivateKey });
        ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(nft, fxAccount2, fxNft.TreasuryAccount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey);
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: AccountFrozenForToken");

        // Double Check can't send from frozen treasury.
        ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(new Hiero.Nft(fxNft, 2), fxNft.TreasuryAccount, fxAccount2, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: AccountFrozenForToken");

        // Balances should not have changed
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length - 1);

        // Resume Participating Accounts
        await client.ResumeTokenAsync(new ResumeTokenParams { Token = fxNft, Holder = fxAccount1, Signatory = fxNft.SuspendPrivateKey });
        await client.ResumeTokenAsync(new ResumeTokenParams { Token = fxNft, Holder = fxNft.TreasuryAccount, Signatory = fxNft.SuspendPrivateKey });

        // Move coins back via 1
        await client.TransferNftAsync(nft, fxAccount2, fxAccount1, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount2.PrivateKey);
        });
        receipt = await client.TransferNftAsync(nft, fxAccount1, fxNft.TreasuryAccount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey);
        });

        // Check our Final Balances
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length);
    }

    [Test]
    public async Task Can_Transfer_Nfts_To_Contract()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = false;
        });
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxContract.ContractReceipt!.Contract,
            Tokens = [fxNft.CreateReceipt!.Token],
            Signatory = fxContract.PrivateKey
        });

        var nft = new Hiero.Nft(fxNft, 1);

        var receipt = await client.TransferNftAsync(nft, fxNft.TreasuryAccount, fxContract.ContractReceipt!.Contract, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var tokens = await fxContract.GetTokenBalancesAsync();
        var association = tokens.FirstOrDefault(t => t.Token == fxNft.CreateReceipt!.Token);
        await Assert.That(association).IsNotNull();
        await Assert.That(association!.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(1U);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Transfer_Nfts_To_Contract_And_Back()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxContract.ContractReceipt!.Contract,
            Tokens = [fxNft.CreateReceipt!.Token],
            Signatory = fxContract.PrivateKey
        });

        var nft = new Hiero.Nft(fxNft, 1);

        var receipt = await client.TransferNftAsync(nft, fxNft.TreasuryAccount, fxContract.ContractReceipt!.Contract, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await Assert.That(await fxContract.GetCryptoBalanceAsync()).IsEqualTo(0U);
        await Assert.That(await fxContract.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxContract.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);

        await Assert.That(await fxNft.TreasuryAccount.GetCryptoBalanceAsync()).IsEqualTo(fxNft.TreasuryAccount.CreateParams.InitialBalance);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalancesAsync()).HasSingleItem();
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length - 1);

        receipt = await client.TransferNftAsync(nft, fxContract.ContractReceipt!.Contract, fxNft.TreasuryAccount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxContract.PrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length);
        await Assert.That(await fxContract.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Change_Treasury_Before_Emptying()
    {
        await using var fxTempTreasury = await TestAccount.CreateAsync();
        await using var fxNewTreasury = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.ConfiscateEndorsement = null;
        }, fxTempTreasury, fxNewTreasury);
        await using var client = await TestNetwork.CreateClientAsync();

        var serialNumbers = Enumerable.Range(1, fxNft.Metadata.Length).Select(i => (long)i);

        var transfers = new TransferParams
        {
            NftTransfers = serialNumbers.Select(sn => new NftTransfer(new Hiero.Nft(fxNft, sn), fxNft.TreasuryAccount, fxTempTreasury)),
            Signatory = fxNft.TreasuryAccount.PrivateKey
        };
        var receipt = await client.TransferAsync(transfers);

        // Double check balances.
        await Assert.That(await fxTempTreasury.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxNewTreasury.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);

        // Move the treasury to a new account having a token balance
        await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxNft,
            Treasury = fxTempTreasury,
            Signatory = new Signatory(fxNft.AdminPrivateKey, fxTempTreasury.PrivateKey)
        });

        await Assert.That(await fxTempTreasury.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxNewTreasury.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Treasury).IsEqualTo(fxTempTreasury.CreateReceipt!.Address);

        // Move the treasury to a new account having zero token balance
        await client.UpdateTokenAsync(new UpdateTokenParams
        {
            Token = fxNft,
            Treasury = fxNewTreasury,
            Signatory = new Signatory(fxNft.AdminPrivateKey, fxNewTreasury.PrivateKey)
        });

        // Coins HAVE moved.
        await Assert.That(await fxTempTreasury.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxNewTreasury.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length);

        // What does the info say now?
        info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNewTreasury.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)fxNft.Metadata.Length);
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
    public async Task Can_Schedule_Multi_Transfer_Nft_Coins()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        await using var client = await TestNetwork.CreateClientAsync();

        var transfers = new TransferParams
        {
            NftTransfers = [
                new NftTransfer(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount, fxAccount1),
                new NftTransfer(new Hiero.Nft(fxNft, 2), fxNft.TreasuryAccount, fxAccount2)
            ],
            Signatory = fxNft.TreasuryAccount.PrivateKey,
        };
        var schedulingReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = transfers,
            Payer = fxPayer
        });
        await Assert.That(schedulingReceipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length);

        var counterReceipt = await client.SignScheduleAsync(new SignScheduleParams
        {
            Schedule = schedulingReceipt.Schedule,
            Signatory = fxPayer.PrivateKey,
        }, ctx =>
        {
            ctx.Payer = fxPayer;
            ctx.Signatory = fxPayer.PrivateKey;
        });
        await Assert.That(counterReceipt.Status).IsEqualTo(ResponseCode.Success);

        var transferReceipt = await client.GetReceiptAsync(schedulingReceipt.ScheduledTxId);
        await Assert.That(transferReceipt.Status).IsEqualTo(ResponseCode.Success);

        await Assert.That(await fxAccount1.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxAccount2.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(fxNft.Metadata.Length - 2);
    }

    [Test]
    public async Task Metadata_And_Serial_Numbers_Transfer_Properly()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var circulation = (ulong)fxNft.Metadata.Length;
        var serialNumbers = Enumerable.Range(1, fxNft.Metadata.Length).Where(i => i % 2 == 0).Select(i => (long)i).ToArray();
        var xferCount = (ulong)serialNumbers.Length;
        var expectedTreasury = circulation - xferCount;

        var transfers = new TransferParams
        {
            NftTransfers = serialNumbers.Select(sn => new NftTransfer(new Hiero.Nft(fxNft, sn), fxNft.TreasuryAccount, fxAccount)),
            Signatory = fxNft.TreasuryAccount.PrivateKey
        };
        var receipt = await client.TransferAsync(transfers);

        // Double check balances.
        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)xferCount);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)(circulation - xferCount));

        // Double Check Metadata
        for (long sn = 1; sn <= (long)circulation; sn++)
        {
            var id = new Hiero.Nft(fxNft.CreateReceipt!.Token, sn);
            var nftInfo = await client.GetNftInfoAsync(id);
            await Assert.That(nftInfo.Nft.SerialNumber).IsEqualTo(sn);
            await Assert.That(nftInfo.Nft.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
            await Assert.That(nftInfo.Owner).IsEqualTo(nftInfo.Nft.SerialNumber % 2 == 0 ? fxAccount.CreateReceipt!.Address : fxNft.TreasuryAccount.CreateReceipt!.Address);
            await Assert.That(fxNft.Metadata[sn - 1].Span.SequenceEqual(nftInfo.Metadata.Span)).IsTrue();
            await Assert.That(nftInfo.Spender).IsEqualTo(EntityId.None);
        }
    }
}
