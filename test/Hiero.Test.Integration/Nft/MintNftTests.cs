using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.NftTokens;

public class MintNftTests
{
    [Test]
    public async Task Can_Mint_Nfts()
    {
        await using var fxNft = await TestNft.CreateAsync(fx => fx.Metadata = null!);
        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var metadata = Enumerable.Range(1, Generator.Integer(2, 10)).Select(_ => Generator.SHA384Hash()).ToArray();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.MintNftsAsync(new MintNftParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Metadata = metadata,
            Signatory = fxNft.SupplyPrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.SerialNumbers.Count).IsEqualTo(metadata.Length);
        foreach (var serialNumber in receipt.SerialNumbers)
        {
            await Assert.That(serialNumber > 0).IsTrue();
        }
        await Assert.That(receipt.Circulation).IsEqualTo((ulong)metadata.Length);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)metadata.Length);
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

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)metadata.Length);
    }

    [Test]
    public async Task Can_Mint_Nfts_Without_Extra_Signatory()
    {
        await using var fxNft = await TestNft.CreateAsync(fx => fx.Metadata = null!);
        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var metadata = Enumerable.Range(1, Generator.Integer(2, 10)).Select(_ => Generator.SHA384Hash()).ToArray();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.MintNftsAsync(new MintNftParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Metadata = metadata,
        }, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.SupplyPrivateKey));
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.SerialNumbers.Count).IsEqualTo(metadata.Length);
        foreach (var serialNumber in receipt.SerialNumbers)
        {
            await Assert.That(serialNumber > 0).IsTrue();
        }
        await Assert.That(receipt.Circulation).IsEqualTo((ulong)metadata.Length);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)metadata.Length);
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

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)metadata.Length);
    }

    [Test]
    public async Task Can_Mint_Nfts_And_Get_Record()
    {
        await using var fxNft = await TestNft.CreateAsync(fx => fx.Metadata = null!);
        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var metadata = Enumerable.Range(1, Generator.Integer(2, 10)).Select(_ => Generator.SHA384Hash()).ToArray();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.MintNftsAsync(new MintNftParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Metadata = metadata,
        }, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.SupplyPrivateKey));
        var record = (NftMintRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.SerialNumbers.Count).IsEqualTo(metadata.Length);
        foreach (var serialNumber in record.SerialNumbers)
        {
            await Assert.That(serialNumber > 0).IsTrue();
        }
        await Assert.That(record.Circulation).IsEqualTo((ulong)metadata.Length);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsGreaterThanOrEqualTo(0UL);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.SerialNumbers.Count).IsEqualTo(metadata.Length);
        await Assert.That(record.Circulation).IsEqualTo((ulong)metadata.Length);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)metadata.Length);
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

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)metadata.Length);
    }

    [Test]
    public async Task Can_Mint_Nfts_From_Any_Account_With_Supply_Key()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 100_00_000_000);
        await using var fxNft = await TestNft.CreateAsync(fx => fx.Metadata = null!);
        await Assert.That(fxNft.CreateReceipt).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token).IsNotNull();
        await Assert.That(fxNft.CreateReceipt!.Token.AccountNum > 0).IsTrue();
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var metadata = Enumerable.Range(1, Generator.Integer(2, 10)).Select(_ => Generator.SHA384Hash()).ToArray();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.MintNftsAsync(new MintNftParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Metadata = metadata,
        }, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = new Signatory(fxAccount.PrivateKey, fxNft.SupplyPrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.SerialNumbers.Count).IsEqualTo(metadata.Length);
        foreach (var serialNumber in receipt.SerialNumbers)
        {
            await Assert.That(serialNumber > 0).IsTrue();
        }
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.SerialNumbers.Count).IsEqualTo(metadata.Length);
        await Assert.That(receipt.Circulation).IsEqualTo((ulong)metadata.Length);

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)metadata.Length);
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

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)metadata.Length);
    }

    [Test]
    public async Task Mint_Nft_Record_Includes_Nft_Transfers()
    {
        await using var fxNft = await TestNft.CreateAsync(fx => fx.Metadata = null!);

        var metadata = Enumerable.Range(1, Generator.Integer(2, 10)).Select(_ => Generator.SHA384Hash()).ToArray();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.MintNftsAsync(new MintNftParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Metadata = metadata,
            Signatory = fxNft.SupplyPrivateKey
        });
        var record = (NftMintRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.SerialNumbers.Count).IsEqualTo(metadata.Length);
        await Assert.That(record.TokenTransfers).IsEmpty();
        await Assert.That(record.NftTransfers.Count).IsEqualTo(metadata.Length);
        await Assert.That(record.Royalties).IsEmpty();
        await Assert.That(record.Associations).IsEmpty();

        for (var i = 0; i < metadata.Length; i++)
        {
            var ssn = record.SerialNumbers[i];
            var xfer = record.NftTransfers.FirstOrDefault(x => x.Nft.SerialNumber == ssn);
            await Assert.That(xfer).IsNotNull();
            await Assert.That((EntityId)xfer!.Nft).IsEqualTo(fxNft.CreateReceipt!.Token);
            await Assert.That(xfer.From).IsEqualTo(EntityId.None);
            await Assert.That(xfer.To).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        }

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(metadata.Length);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo((ulong)metadata.Length);
    }

    [Test]
    public async Task Mint_Nft_Requires_Signature_By_Supply_Key()
    {
        await using var fxNft = await TestNft.CreateAsync(fx => fx.Metadata = null!);
        var metadata = Enumerable.Range(1, Generator.Integer(2, 10)).Select(_ => Generator.SHA384Hash()).ToArray();

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.MintNftsAsync(new MintNftParams
            {
                Token = fxNft.CreateReceipt!.Token,
                Metadata = metadata,
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Mint NFT failed with status: InvalidSignature");

        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(0UL);
    }

    [Test]
    public async Task Can_Not_More_Mint_Nfts_Than_Ceiling()
    {
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.Ceiling = fx.Metadata.Length);

        var metadata = new ReadOnlyMemory<byte>[] { Generator.SHA384Hash() };

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.MintNftsAsync(new MintNftParams
            {
                Token = fxNft.CreateReceipt!.Token,
                Metadata = metadata,
                Signatory = fxNft.SupplyPrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenMaxSupplyReached);
        await Assert.That(tex.Message).StartsWith("Mint NFT failed with status: TokenMaxSupplyReached");

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That((long)info.Circulation).IsEqualTo(fxNft.CreateParams.Ceiling);
        await Assert.That(info.Ceiling).IsEqualTo(fxNft.CreateParams.Ceiling);
    }

    [Test]
    public async Task Can_Schedule_Mint_Nft_Coins()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxNft = await TestNft.CreateAsync();
        var metadata = Enumerable.Range(1, Generator.Integer(2, 10)).Select(_ => Generator.SHA384Hash()).ToArray();

        await using var client = await TestNetwork.CreateClientAsync();
        var pendingReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new MintNftParams
            {
                Token = fxNft.CreateReceipt!.Token,
                Metadata = metadata,
                Signatory = fxNft.SupplyPrivateKey,
            },
            Payer = fxPayer
        });
        await AssertHg.NftBalanceAsync(fxNft, fxNft.TreasuryAccount, (ulong)fxNft.Metadata.Length);
        var schedulingReceipt = await client.SignScheduleAsync(pendingReceipt.Schedule, ctx =>
        {
            ctx.Payer = fxPayer;
            ctx.Signatory = fxPayer.PrivateKey;
        });
        await Assert.That(schedulingReceipt.Status).IsEqualTo(ResponseCode.Success);

        // Can get receipt for original scheduled tx.
        var executedReceipt = await client.GetReceiptAsync(pendingReceipt.ScheduledTxId) as NftMintReceipt;
        await Assert.That(executedReceipt!.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(executedReceipt.SerialNumbers.Count).IsEqualTo(metadata.Length);
        foreach (var serialNumber in executedReceipt.SerialNumbers)
        {
            await Assert.That(serialNumber > 0).IsTrue();
        }
        await Assert.That(executedReceipt.Circulation).IsEqualTo((ulong)(fxNft.Metadata.Length + metadata.Length));

        // Can get record for original scheduled tx.
        var record = await client.GetTransactionRecordAsync(pendingReceipt.ScheduledTxId) as NftMintRecord;
        await Assert.That(record!.SerialNumbers.Count).IsEqualTo(metadata.Length);
        foreach (var serialNumber in record.SerialNumbers)
        {
            await Assert.That(serialNumber > 0).IsTrue();
        }
        await Assert.That(record.Circulation).IsEqualTo((ulong)(fxNft.Metadata.Length + metadata.Length));

        await AssertHg.NftBalanceAsync(fxNft, fxNft.TreasuryAccount, (ulong)(metadata.Length + fxNft.Metadata.Length));

        var info = await client.GetTokenInfoAsync(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(info.Symbol).IsEqualTo(fxNft.CreateParams.Symbol);
        await Assert.That(info.Type).IsEqualTo(TokenType.NonFungible);
        await Assert.That(info.Treasury).IsEqualTo(fxNft.TreasuryAccount.CreateReceipt!.Address);
        await Assert.That(info.Circulation).IsEqualTo((ulong)(metadata.Length + fxNft.Metadata.Length));
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
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Mint_Nft()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxNft = await TestNft.CreateAsync(fx => fx.Metadata = null!);
        var metadata = new[] { Generator.SHA384Hash() };
        await using var client = await TestNetwork.CreateClientAsync();
        var scheduledReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new MintNftParams
            {
                Token = fxNft.CreateReceipt!.Token,
                Metadata = metadata,
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

        var executedReceipt = await client.GetReceiptAsync(scheduledReceipt.ScheduledTxId) as NftMintReceipt;
        await Assert.That(executedReceipt).IsNotNull();
        await Assert.That(executedReceipt!.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(executedReceipt.SerialNumbers.Count).IsEqualTo(metadata.Length);
    }
}
