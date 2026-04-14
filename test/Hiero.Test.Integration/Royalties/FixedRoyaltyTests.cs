using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Royalties;

public class FixedRoyaltyTests
{
    [Test]
    public async Task Transferring_Asset_Applies_Single_Fixed_Commission()
    {
        await using var fxBuyer = await TestAccount.CreateAsync();
        await using var fxSeller = await TestAccount.CreateAsync();
        await using var fxBenefactor = await TestAccount.CreateAsync();
        await using var fxPaymentToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxBuyer, fxSeller, fxBenefactor);
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new FixedRoyalty(fxBenefactor.CreateReceipt!.Address, fxPaymentToken.CreateReceipt!.Token, 10)
            };
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxBenefactor.PrivateKey);
        }, fxBuyer, fxSeller);
        await using var client = await TestNetwork.CreateClientAsync();

        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxPaymentToken.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxBuyer.CreateReceipt!.Address, 100)
            },
            Signatory = fxPaymentToken.TreasuryAccount.PrivateKey
        });

        await client.TransferNftAsync(new Nft(fxNft.CreateReceipt!.Token, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxSeller.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBuyer, 100);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxSeller, 0);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBenefactor, 0);
        await AssertHg.NftBalanceAsync(fxNft, fxSeller, 1);
        await AssertHg.NftBalanceAsync(fxNft, fxBuyer, 0);

        var receipt = await client.TransferAsync(new TransferParams
        {
            NftTransfers = new[]
            {
                new NftTransfer(new Nft(fxNft.CreateReceipt!.Token, 1), fxSeller.CreateReceipt!.Address, fxBuyer.CreateReceipt!.Address)
            },
            TokenTransfers = new[]
            {
                new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxBuyer.CreateReceipt!.Address, -100),
                new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxSeller.CreateReceipt!.Address, 100)
            },
            Signatory = new Signatory(fxBuyer.PrivateKey, fxSeller.PrivateKey)
        });

        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TokenTransfers.Count).IsEqualTo(3);
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxBuyer.CreateReceipt!.Address, -100));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxSeller.CreateReceipt!.Address, 90));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxBenefactor.CreateReceipt!.Address, 10));
        await Assert.That(record.NftTransfers).HasSingleItem();
        await Assert.That(record.Royalties).HasSingleItem();
        AssertHg.ContainsRoyalty(fxPaymentToken, fxSeller, fxBenefactor, 10, record.Royalties);

        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBuyer, 0);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxSeller, 90);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBenefactor, 10);
        await AssertHg.NftBalanceAsync(fxNft, fxSeller, 0);
        await AssertHg.NftBalanceAsync(fxNft, fxBuyer, 1);
    }

    [Test]
    public async Task Transferring_Asset_Applies_Single_Fixed_Hbar_Commission()
    {
        await using var fxBuyer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 10_00_000_000);
        await using var fxSeller = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 0);
        await using var fxBenefactor = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 0);
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Royalties =
            [
                new FixedRoyalty(fxBenefactor, EntityId.None, 1_00_000_000)
            ];
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxBenefactor.PrivateKey);
        }, fxBuyer, fxSeller);
        await using var client = await TestNetwork.CreateClientAsync();

        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await client.TransferNftAsync(new Nft(fxNft.CreateReceipt!.Token, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxSeller.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.CryptoBalanceAsync(fxBuyer, 10_00_000_000);
        await AssertHg.CryptoBalanceAsync(fxSeller, 0);
        await AssertHg.CryptoBalanceAsync(fxBenefactor, 0);
        await AssertHg.NftBalanceAsync(fxNft, fxSeller, 1);
        await AssertHg.NftBalanceAsync(fxNft, fxBuyer, 0);

        var receipt = await client.TransferAsync(new TransferParams
        {
            NftTransfers = new[]
            {
                new NftTransfer(new Nft(fxNft.CreateReceipt!.Token, 1), fxSeller.CreateReceipt!.Address, fxBuyer.CreateReceipt!.Address)
            },
            CryptoTransfers = new[]
            {
                new CryptoTransfer(fxBuyer.CreateReceipt!.Address, -5_00_000_000),
                new CryptoTransfer(fxSeller.CreateReceipt!.Address, 5_00_000_000)
            },
            Signatory = new Signatory(fxBuyer.PrivateKey, fxSeller.PrivateKey)
        });

        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.NftTransfers).HasSingleItem();
        await Assert.That(record.Royalties).HasSingleItem();
        AssertHg.ContainsHbarRoyalty(fxSeller, fxBenefactor, 1_00_000_000, record.Royalties);

        await AssertHg.CryptoBalanceAsync(fxBuyer, 10_00_000_000 - 5_00_000_000);
        await AssertHg.CryptoBalanceAsync(fxSeller, 5_00_000_000 - 1_00_000_000);
        await AssertHg.CryptoBalanceAsync(fxBenefactor, 1_00_000_000);
        await AssertHg.NftBalanceAsync(fxNft, fxSeller, 0);
        await AssertHg.NftBalanceAsync(fxNft, fxBuyer, 1);
    }

    [Test]
    public async Task Transferring_Asset_Applies_Fixed_Commissions_When_Token_And_HBar_Exchanged()
    {
        await using var fxBuyer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 10_00_000_000);
        await using var fxSeller = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 0);
        await using var fxBenefactor = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 0);
        await using var fxPaymentToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxBuyer, fxSeller, fxBenefactor);
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Royalties = [new FixedRoyalty(fxBenefactor, fxPaymentToken, 50)];
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxBuyer, fxSeller);
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var movedNft = new Nft(fxNft, 1);

        await using var client = await TestNetwork.CreateClientAsync();
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers =
            [
                new TokenTransfer(fxPaymentToken, fxPaymentToken.TreasuryAccount, -100),
                new TokenTransfer(fxPaymentToken, fxBuyer, 100)
            ],
            Signatory = fxPaymentToken.TreasuryAccount
        });

        await client.TransferNftAsync(movedNft, fxNft.TreasuryAccount, fxSeller, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount);
        });

        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBuyer, 100);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxSeller, 0);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBenefactor, 0);
        await AssertHg.CryptoBalanceAsync(fxBuyer, 10_00_000_000);
        await AssertHg.CryptoBalanceAsync(fxSeller, 0);
        await AssertHg.CryptoBalanceAsync(fxBenefactor, 0);
        await AssertHg.NftBalanceAsync(fxNft, fxSeller, 1);
        await AssertHg.NftBalanceAsync(fxNft, fxBuyer, 0);

        var receipt = await client.TransferAsync(new TransferParams
        {
            NftTransfers =
            [
                new NftTransfer(movedNft, fxSeller, fxBuyer)
            ],
            TokenTransfers =
            [
                new TokenTransfer(fxPaymentToken, fxBuyer, -100),
                new TokenTransfer(fxPaymentToken, fxSeller, 100)
            ],
            CryptoTransfers =
            [
                new CryptoTransfer(fxBuyer, -10_00_000_000),
                new CryptoTransfer(fxSeller, 10_00_000_000)
            ],
            Signatory = new Signatory(fxBuyer, fxSeller)
        });

        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TokenTransfers.Count).IsEqualTo(3);
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxBuyer.CreateReceipt!.Address, -100));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxSeller.CreateReceipt!.Address, 50));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxBenefactor.CreateReceipt!.Address, 50));
        await Assert.That(record.NftTransfers).HasSingleItem();
        await Assert.That(record.Royalties).HasSingleItem();
        AssertHg.ContainsRoyalty(fxPaymentToken, fxSeller, fxBenefactor, 50, record.Royalties);

        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBuyer, 0);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxSeller, 50);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBenefactor, 50);
        await AssertHg.CryptoBalanceAsync(fxBuyer, 10_00_000_000 - 10_00_000_000);
        await AssertHg.CryptoBalanceAsync(fxSeller, 10_00_000_000);
        await AssertHg.CryptoBalanceAsync(fxBenefactor, 0);
        await AssertHg.NftBalanceAsync(fxNft, fxSeller, 0);
        await AssertHg.NftBalanceAsync(fxNft, fxBuyer, 1);
    }

    [Test]
    public async Task Transferring_Asset_Applies_Multiple_Fixed_Commission_Deduction_Destinations()
    {
        await using var fxBuyer = await TestAccount.CreateAsync();
        await using var fxSeller = await TestAccount.CreateAsync();
        await using var fxBenefactor1 = await TestAccount.CreateAsync();
        await using var fxBenefactor2 = await TestAccount.CreateAsync();
        await using var fxBenefactor3 = await TestAccount.CreateAsync();
        await using var fxPaymentToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Decimals = 2;
            fx.CreateParams.Circulation = 1_000_00;
        }, fxBuyer, fxSeller, fxBenefactor1, fxBenefactor2, fxBenefactor3);
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new FixedRoyalty(fxBenefactor1.CreateReceipt!.Address, fxPaymentToken.CreateReceipt!.Token, 20),
                new FixedRoyalty(fxBenefactor2.CreateReceipt!.Address, fxPaymentToken.CreateReceipt!.Token, 20),
                new FixedRoyalty(fxBenefactor3.CreateReceipt!.Address, fxPaymentToken.CreateReceipt!.Token, 40)
            };
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxBenefactor1.PrivateKey, fxBenefactor2.PrivateKey, fxBenefactor3.PrivateKey);
        }, fxBuyer, fxSeller);
        await using var client = await TestNetwork.CreateClientAsync();

        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxPaymentToken.TreasuryAccount.CreateReceipt!.Address, -100_00),
                new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxBuyer.CreateReceipt!.Address, 100_00)
            },
            Signatory = fxPaymentToken.TreasuryAccount.PrivateKey
        });

        await client.TransferNftAsync(new Nft(fxNft.CreateReceipt!.Token, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxSeller.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBuyer, 100_00);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxSeller, 0);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBenefactor1, 0);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBenefactor2, 0);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBenefactor3, 0);
        await AssertHg.NftBalanceAsync(fxNft, fxSeller, 1);
        await AssertHg.NftBalanceAsync(fxNft, fxBuyer, 0);

        var receipt = await client.TransferAsync(new TransferParams
        {
            NftTransfers = new[]
            {
                new NftTransfer(new Nft(fxNft.CreateReceipt!.Token, 1), fxSeller.CreateReceipt!.Address, fxBuyer.CreateReceipt!.Address)
            },
            TokenTransfers = new[]
            {
                new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxBuyer.CreateReceipt!.Address, -100_00),
                new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxSeller.CreateReceipt!.Address, 100_00)
            },
            Signatory = new Signatory(fxBuyer.PrivateKey, fxSeller.PrivateKey)
        });

        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TokenTransfers.Count).IsEqualTo(5);
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxBuyer.CreateReceipt!.Address, -100_00));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxSeller.CreateReceipt!.Address, 100_00 - 20 - 20 - 40));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxBenefactor1.CreateReceipt!.Address, 20));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxBenefactor2.CreateReceipt!.Address, 20));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxBenefactor3.CreateReceipt!.Address, 40));
        await Assert.That(record.NftTransfers).HasSingleItem();
        await Assert.That(record.Royalties.Count).IsEqualTo(3);
        AssertHg.ContainsRoyalty(fxPaymentToken, fxSeller, fxBenefactor1, 20, record.Royalties);
        AssertHg.ContainsRoyalty(fxPaymentToken, fxSeller, fxBenefactor2, 20, record.Royalties);
        AssertHg.ContainsRoyalty(fxPaymentToken, fxSeller, fxBenefactor3, 40, record.Royalties);

        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBuyer, 0);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxSeller, 100_00 - 20 - 20 - 40);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBenefactor1, 20);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBenefactor2, 20);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBenefactor3, 40);
        await AssertHg.NftBalanceAsync(fxNft, fxSeller, 0);
        await AssertHg.NftBalanceAsync(fxNft, fxBuyer, 1);
    }

    [Test]
    public async Task Transferring_Asset_Applies_Multiple_Fixed_Commission_Fee_Even_Without_Payment()
    {
        await using var fxBuyer = await TestAccount.CreateAsync();
        await using var fxSeller = await TestAccount.CreateAsync();
        await using var fxBenefactor1 = await TestAccount.CreateAsync();
        await using var fxBenefactor2 = await TestAccount.CreateAsync();
        await using var fxBenefactor3 = await TestAccount.CreateAsync();
        await using var fxPaymentToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Decimals = 2;
            fx.CreateParams.Circulation = 1_000_00;
        }, fxBuyer, fxSeller, fxBenefactor1, fxBenefactor2, fxBenefactor3);
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new FixedRoyalty(fxBenefactor1.CreateReceipt!.Address, fxPaymentToken.CreateReceipt!.Token, 20),
                new FixedRoyalty(fxBenefactor2.CreateReceipt!.Address, fxPaymentToken.CreateReceipt!.Token, 20),
                new FixedRoyalty(fxBenefactor3.CreateReceipt!.Address, fxPaymentToken.CreateReceipt!.Token, 40)
            };
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Signatory = new Signatory(fx.CreateParams.Signatory!, fxBenefactor1.PrivateKey, fxBenefactor2.PrivateKey, fxBenefactor3.PrivateKey);
        }, fxBuyer, fxSeller);
        await using var client = await TestNetwork.CreateClientAsync();

        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxPaymentToken.TreasuryAccount.CreateReceipt!.Address, -100_00),
                new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxSeller.CreateReceipt!.Address, 100_00)
            },
            Signatory = fxPaymentToken.TreasuryAccount.PrivateKey
        });

        await client.TransferNftAsync(new Nft(fxNft.CreateReceipt!.Token, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxSeller.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBuyer, 0);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxSeller, 100_00);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBenefactor1, 0);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBenefactor2, 0);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBenefactor3, 0);
        await AssertHg.NftBalanceAsync(fxNft, fxSeller, 1);
        await AssertHg.NftBalanceAsync(fxNft, fxBuyer, 0);

        var receipt = await client.TransferNftAsync(new Nft(fxNft.CreateReceipt!.Token, 1), fxSeller.CreateReceipt!.Address, fxBuyer.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxSeller.PrivateKey);
        });

        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TokenTransfers.Count).IsEqualTo(4);
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxSeller.CreateReceipt!.Address, -80));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxBenefactor1.CreateReceipt!.Address, 20));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxBenefactor2.CreateReceipt!.Address, 20));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken.CreateReceipt!.Token, fxBenefactor3.CreateReceipt!.Address, 40));
        await Assert.That(record.NftTransfers).HasSingleItem();
        await Assert.That(record.Royalties.Count).IsEqualTo(3);
        AssertHg.ContainsRoyalty(fxPaymentToken, fxSeller, fxBenefactor1, 20, record.Royalties);
        AssertHg.ContainsRoyalty(fxPaymentToken, fxSeller, fxBenefactor2, 20, record.Royalties);
        AssertHg.ContainsRoyalty(fxPaymentToken, fxSeller, fxBenefactor3, 40, record.Royalties);

        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBuyer, 0);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxSeller, 100_00 - 80);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBenefactor1, 20);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBenefactor2, 20);
        await AssertHg.TokenBalanceAsync(fxPaymentToken, fxBenefactor3, 40);
        await AssertHg.NftBalanceAsync(fxNft, fxSeller, 0);
        await AssertHg.NftBalanceAsync(fxNft, fxBuyer, 1);
    }
}
