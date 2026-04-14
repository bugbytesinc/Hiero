using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Royalties;

public class NftRoyaltyTests
{
    [Test]
    public async Task Transferring_NFT_Applies_Single_Value_Commission()
    {
        await using var fxBenefactor = await TestAccount.CreateAsync();
        await using var fxSeller = await TestAccount.CreateAsync();
        await using var fxBuyer = await TestAccount.CreateAsync();
        await using var fxPaymentToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxBuyer, fxSeller, fxBenefactor);
        await using var fxNft = await TestNft.CreateAsync((Action<TestNft>)(fx =>
        {
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new NftRoyalty(fxBenefactor, 1, 2, 0, EntityId.None)
            };
            fx.CreateParams.GrantKycEndorsement = null;
        }), fxBuyer, fxSeller);
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();

        // Transfer payment tokens to buyer
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxPaymentToken, fxPaymentToken.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxPaymentToken, fxBuyer.CreateReceipt!.Address, 100)
            },
            Signatory = fxPaymentToken.TreasuryAccount.PrivateKey
        });

        // Transfer NFT from treasury to seller
        await client.TransferNftAsync(new Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxSeller.CreateReceipt!.Address, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey));

        // Buyer pays 100 tokens for NFT (seller sends NFT, buyer sends tokens)
        var receipt = await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxPaymentToken, fxBuyer.CreateReceipt!.Address, -100),
                new TokenTransfer(fxPaymentToken, fxSeller.CreateReceipt!.Address, 100)
            },
            NftTransfers = new[]
            {
                new NftTransfer(new Nft(fxNft, 1), fxSeller.CreateReceipt!.Address, fxBuyer.CreateReceipt!.Address)
            },
            Signatory = new Signatory(fxSeller.PrivateKey, fxBuyer.PrivateKey)
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TokenTransfers.Count).IsEqualTo(3);
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken, fxBuyer.CreateReceipt!.Address, -100));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken, fxSeller.CreateReceipt!.Address, 50));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken, fxBenefactor.CreateReceipt!.Address, 50));
        await Assert.That(record.NftTransfers).HasSingleItem();
        await Assert.That(record.Royalties.Count).IsEqualTo(1);
        AssertHg.ContainsRoyalty(fxPaymentToken, fxSeller, fxBenefactor, 50, record.Royalties);

        await AssertHg.NftBalanceAsync(fxNft, fxBuyer, 1);
        await AssertHg.NftBalanceAsync(fxNft, fxSeller, 0);
    }

    [Test]
    public async Task Transferring_NFT_Applies_Single_Value_Hbar_Commission()
    {
        await using var fxBenefactor = await TestAccount.CreateAsync();
        await using var fxSeller = await TestAccount.CreateAsync();
        await using var fxBuyer = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync((Action<TestNft>)(fx =>
        {
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new NftRoyalty(fxBenefactor, 1, 2, 0, EntityId.None)
            };
            fx.CreateParams.GrantKycEndorsement = null;
        }), fxBuyer, fxSeller);
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();

        // Fund the buyer with hbar
        await client.TransferAsync(TestNetwork.Payer, fxBuyer.CreateReceipt!.Address, 10_00_000_000);

        // Transfer NFT from treasury to seller
        await client.TransferNftAsync(new Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxSeller.CreateReceipt!.Address, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey));

        // Buyer pays 10_00_000_000 hbar for NFT
        var receipt = await client.TransferAsync(new TransferParams
        {
            CryptoTransfers = new[]
            {
                new CryptoTransfer(fxBuyer.CreateReceipt!.Address, -10_00_000_000),
                new CryptoTransfer(fxSeller.CreateReceipt!.Address, 10_00_000_000)
            },
            NftTransfers = new[]
            {
                new NftTransfer(new Nft(fxNft, 1), fxSeller.CreateReceipt!.Address, fxBuyer.CreateReceipt!.Address)
            },
            Signatory = new Signatory(fxSeller.PrivateKey, fxBuyer.PrivateKey)
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TokenTransfers).IsEmpty();
        await Assert.That(record.NftTransfers).HasSingleItem();
        await Assert.That(record.Royalties.Count).IsEqualTo(1);
        AssertHg.ContainsHbarRoyalty(fxSeller, fxBenefactor, 5_00_000_000, record.Royalties);

        await AssertHg.NftBalanceAsync(fxNft, fxBuyer, 1);
        await AssertHg.NftBalanceAsync(fxNft, fxSeller, 0);
    }

    [Test]
    public async Task Transferring_NFT_Applies_Value_Commissions_When_Token_And_HBar_Exchanged()
    {
        await using var fxBenefactor = await TestAccount.CreateAsync();
        await using var fxSeller = await TestAccount.CreateAsync();
        await using var fxBuyer = await TestAccount.CreateAsync();
        await using var fxPaymentToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxBuyer, fxSeller, fxBenefactor);
        await using var fxNft = await TestNft.CreateAsync((Action<TestNft>)(fx =>
        {
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new NftRoyalty(fxBenefactor, 1, 2, 0, EntityId.None)
            };
            fx.CreateParams.GrantKycEndorsement = null;
        }), fxBuyer, fxSeller);
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();

        // Transfer payment tokens to buyer
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxPaymentToken, fxPaymentToken.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxPaymentToken, fxBuyer.CreateReceipt!.Address, 100)
            },
            Signatory = fxPaymentToken.TreasuryAccount.PrivateKey
        });

        // Fund the buyer with hbar
        await client.TransferAsync(TestNetwork.Payer, fxBuyer.CreateReceipt!.Address, 10_00_000_000);

        // Transfer NFT from treasury to seller
        await client.TransferNftAsync(new Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxSeller.CreateReceipt!.Address, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey));

        // Buyer pays both 100 tokens and 10_00_000_000 hbar for NFT
        var receipt = await client.TransferAsync(new TransferParams
        {
            CryptoTransfers = new[]
            {
                new CryptoTransfer(fxBuyer.CreateReceipt!.Address, -10_00_000_000),
                new CryptoTransfer(fxSeller.CreateReceipt!.Address, 10_00_000_000)
            },
            TokenTransfers = new[]
            {
                new TokenTransfer(fxPaymentToken, fxBuyer.CreateReceipt!.Address, -100),
                new TokenTransfer(fxPaymentToken, fxSeller.CreateReceipt!.Address, 100)
            },
            NftTransfers = new[]
            {
                new NftTransfer(new Nft(fxNft, 1), fxSeller.CreateReceipt!.Address, fxBuyer.CreateReceipt!.Address)
            },
            Signatory = new Signatory(fxSeller.PrivateKey, fxBuyer.PrivateKey)
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TokenTransfers.Count).IsEqualTo(3);
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken, fxBuyer.CreateReceipt!.Address, -100));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken, fxSeller.CreateReceipt!.Address, 50));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken, fxBenefactor.CreateReceipt!.Address, 50));
        await Assert.That(record.NftTransfers).HasSingleItem();
        await Assert.That(record.Royalties.Count).IsEqualTo(2);
        AssertHg.ContainsRoyalty(fxPaymentToken, fxSeller, fxBenefactor, 50, record.Royalties);
        AssertHg.ContainsHbarRoyalty(fxSeller, fxBenefactor, 5_00_000_000, record.Royalties);

        await AssertHg.NftBalanceAsync(fxNft, fxBuyer, 1);
        await AssertHg.NftBalanceAsync(fxNft, fxSeller, 0);
    }

    [Test]
    public async Task Transferring_NFT_Applies_Single_Value_Commission_Without_Fallback()
    {
        await using var fxBenefactor = await TestAccount.CreateAsync();
        await using var fxSeller = await TestAccount.CreateAsync();
        await using var fxBuyer = await TestAccount.CreateAsync();
        await using var fxPaymentToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxBuyer, fxSeller, fxBenefactor);
        await using var fxNft = await TestNft.CreateAsync((Action<TestNft>)(fx =>
        {
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new NftRoyalty(fxBenefactor, 1, 2, 10_00_000_000, EntityId.None)
            };
            fx.CreateParams.GrantKycEndorsement = null;
        }), fxBuyer, fxSeller);
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();

        // Transfer payment tokens to buyer
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxPaymentToken, fxPaymentToken.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxPaymentToken, fxBuyer.CreateReceipt!.Address, 100)
            },
            Signatory = fxPaymentToken.TreasuryAccount.PrivateKey
        });

        // Transfer NFT from treasury to seller
        await client.TransferNftAsync(new Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxSeller.CreateReceipt!.Address, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey));

        // Buyer pays 100 tokens for NFT (fallback should not apply since token payment exists)
        var receipt = await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxPaymentToken, fxBuyer.CreateReceipt!.Address, -100),
                new TokenTransfer(fxPaymentToken, fxSeller.CreateReceipt!.Address, 100)
            },
            NftTransfers = new[]
            {
                new NftTransfer(new Nft(fxNft, 1), fxSeller.CreateReceipt!.Address, fxBuyer.CreateReceipt!.Address)
            },
            Signatory = new Signatory(fxSeller.PrivateKey, fxBuyer.PrivateKey)
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TokenTransfers.Count).IsEqualTo(3);
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken, fxBuyer.CreateReceipt!.Address, -100));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken, fxSeller.CreateReceipt!.Address, 50));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken, fxBenefactor.CreateReceipt!.Address, 50));
        await Assert.That(record.NftTransfers).HasSingleItem();
        await Assert.That(record.Royalties.Count).IsEqualTo(1);
        AssertHg.ContainsRoyalty(fxPaymentToken, fxSeller, fxBenefactor, 50, record.Royalties);

        await AssertHg.NftBalanceAsync(fxNft, fxBuyer, 1);
        await AssertHg.NftBalanceAsync(fxNft, fxSeller, 0);
    }

    [Test]
    public async Task Transferring_NFT_Applies_Single_Value_Commission_With_HBar_Fallback()
    {
        await using var fxBenefactor = await TestAccount.CreateAsync();
        await using var fxSeller = await TestAccount.CreateAsync();
        await using var fxBuyer = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync((Action<TestNft>)(fx =>
        {
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new NftRoyalty(fxBenefactor, 1, 2, 10_00_000_000, EntityId.None)
            };
            fx.CreateParams.GrantKycEndorsement = null;
        }), fxBuyer, fxSeller);
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();

        // Fund the buyer with hbar for the fallback
        await client.TransferAsync(TestNetwork.Payer, fxBuyer.CreateReceipt!.Address, 10_00_000_000);

        // Transfer NFT from treasury to seller
        await client.TransferNftAsync(new Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxSeller.CreateReceipt!.Address, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey));

        // NFT-only transfer (no payment), fallback kicks in
        var receipt = await client.TransferNftAsync(new Nft(fxNft, 1), fxSeller.CreateReceipt!.Address, fxBuyer.CreateReceipt!.Address, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxSeller.PrivateKey, fxBuyer.PrivateKey));
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TokenTransfers).IsEmpty();
        await Assert.That(record.NftTransfers).HasSingleItem();
        await Assert.That(record.Royalties.Count).IsEqualTo(1);
        AssertHg.ContainsHbarRoyalty(fxBuyer, fxBenefactor, 10_00_000_000, record.Royalties);

        await AssertHg.NftBalanceAsync(fxNft, fxBuyer, 1);
        await AssertHg.NftBalanceAsync(fxNft, fxSeller, 0);
    }

    [Test]
    public async Task Transferring_NFT_Applies_Single_Value_Commission_With_Token_Fallback()
    {
        await using var fxBenefactor = await TestAccount.CreateAsync();
        await using var fxSeller = await TestAccount.CreateAsync();
        await using var fxBuyer = await TestAccount.CreateAsync();
        await using var fxPaymentToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
        }, fxBuyer, fxSeller, fxBenefactor);
        await using var fxNft = await TestNft.CreateAsync((Action<TestNft>)(fx =>
        {
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new NftRoyalty(fxBenefactor, 1, 2, 10, fxPaymentToken)
            };
            fx.CreateParams.GrantKycEndorsement = null;
        }), fxBuyer, fxSeller);
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();

        // Transfer payment tokens to buyer for fallback
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxPaymentToken, fxPaymentToken.TreasuryAccount.CreateReceipt!.Address, -10),
                new TokenTransfer(fxPaymentToken, fxBuyer.CreateReceipt!.Address, 10)
            },
            Signatory = fxPaymentToken.TreasuryAccount.PrivateKey
        });

        // Transfer NFT from treasury to seller
        await client.TransferNftAsync(new Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxSeller.CreateReceipt!.Address, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey));

        // NFT-only transfer (no payment), token fallback kicks in
        var receipt = await client.TransferNftAsync(new Nft(fxNft, 1), fxSeller.CreateReceipt!.Address, fxBuyer.CreateReceipt!.Address, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxSeller.PrivateKey, fxBuyer.PrivateKey));
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TokenTransfers.Count).IsEqualTo(2);
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken, fxBuyer.CreateReceipt!.Address, -10));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken, fxBenefactor.CreateReceipt!.Address, 10));
        await Assert.That(record.NftTransfers).HasSingleItem();
        await Assert.That(record.Royalties.Count).IsEqualTo(1);
        AssertHg.ContainsRoyalty(fxPaymentToken, fxBuyer, fxBenefactor, 10, record.Royalties);

        await AssertHg.NftBalanceAsync(fxNft, fxBuyer, 1);
        await AssertHg.NftBalanceAsync(fxNft, fxSeller, 0);
    }

    [Test]
    public async Task No_Royalty_For_Single_Transfer_When_No_Fallback()
    {
        await using var fxBenefactor = await TestAccount.CreateAsync();
        await using var fxSeller = await TestAccount.CreateAsync();
        await using var fxBuyer = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync((Action<TestNft>)(fx =>
        {
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new NftRoyalty(fxBenefactor, 1, 2, 0, EntityId.None)
            };
            fx.CreateParams.GrantKycEndorsement = null;
        }), fxBuyer, fxSeller);
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();

        // Transfer NFT from treasury to seller
        await client.TransferNftAsync(new Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxSeller.CreateReceipt!.Address, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey));

        // NFT-only transfer (no payment, no fallback)
        var receipt = await client.TransferNftAsync(new Nft(fxNft, 1), fxSeller.CreateReceipt!.Address, fxBuyer.CreateReceipt!.Address, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxSeller.PrivateKey));
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TokenTransfers).IsEmpty();
        await Assert.That(record.NftTransfers).HasSingleItem();
        await Assert.That(record.Royalties).IsEmpty();

        await AssertHg.NftBalanceAsync(fxNft, fxBuyer, 1);
        await AssertHg.NftBalanceAsync(fxNft, fxSeller, 0);
    }

    [Test]
    public async Task Transferring_NFT_Applies_Multiple_Value_Commission_Destinations()
    {
        await using var fxBenefactor1 = await TestAccount.CreateAsync();
        await using var fxBenefactor2 = await TestAccount.CreateAsync();
        await using var fxBenefactor3 = await TestAccount.CreateAsync();
        await using var fxSeller = await TestAccount.CreateAsync();
        await using var fxBuyer = await TestAccount.CreateAsync();
        await using var fxPaymentToken = await TestToken.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.Decimals = 2;
            fx.CreateParams.Circulation = 1_000_00;
        }, fxBuyer, fxSeller, fxBenefactor1, fxBenefactor2, fxBenefactor3);
        await using var fxNft = await TestNft.CreateAsync((Action<TestNft>)(fx =>
        {
            fx.CreateParams.Royalties = new IRoyalty[]
            {
                new NftRoyalty(fxBenefactor1, 1, 10, 0, EntityId.None),
                new NftRoyalty(fxBenefactor2, 1, 10, 0, EntityId.None),
                new NftRoyalty(fxBenefactor3, 1, 5, 0, EntityId.None)
            };
            fx.CreateParams.GrantKycEndorsement = null;
        }), fxBuyer, fxSeller);
        await Assert.That(fxNft.CreateReceipt!.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();

        // Transfer payment tokens to buyer
        await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxPaymentToken, fxPaymentToken.TreasuryAccount.CreateReceipt!.Address, -100),
                new TokenTransfer(fxPaymentToken, fxBuyer.CreateReceipt!.Address, 100)
            },
            Signatory = fxPaymentToken.TreasuryAccount.PrivateKey
        });

        // Transfer NFT from treasury to seller
        await client.TransferNftAsync(new Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxSeller.CreateReceipt!.Address, ctx =>
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey));

        // Buyer pays 100 tokens for NFT
        var receipt = await client.TransferAsync(new TransferParams
        {
            TokenTransfers = new[]
            {
                new TokenTransfer(fxPaymentToken, fxBuyer.CreateReceipt!.Address, -100),
                new TokenTransfer(fxPaymentToken, fxSeller.CreateReceipt!.Address, 100)
            },
            NftTransfers = new[]
            {
                new NftTransfer(new Nft(fxNft, 1), fxSeller.CreateReceipt!.Address, fxBuyer.CreateReceipt!.Address)
            },
            Signatory = new Signatory(fxSeller.PrivateKey, fxBuyer.PrivateKey)
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TokenTransfers.Count).IsEqualTo(5);
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken, fxBuyer.CreateReceipt!.Address, -100));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken, fxSeller.CreateReceipt!.Address, 60));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken, fxBenefactor1.CreateReceipt!.Address, 10));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken, fxBenefactor2.CreateReceipt!.Address, 10));
        await Assert.That(record.TokenTransfers).Contains(new TokenTransfer(fxPaymentToken, fxBenefactor3.CreateReceipt!.Address, 20));
        await Assert.That(record.NftTransfers).HasSingleItem();
        await Assert.That(record.Royalties.Count).IsEqualTo(3);
        AssertHg.ContainsRoyalty(fxPaymentToken, fxSeller, fxBenefactor1, 10, record.Royalties);
        AssertHg.ContainsRoyalty(fxPaymentToken, fxSeller, fxBenefactor2, 10, record.Royalties);
        AssertHg.ContainsRoyalty(fxPaymentToken, fxSeller, fxBenefactor3, 20, record.Royalties);

        await AssertHg.NftBalanceAsync(fxNft, fxBuyer, 1);
        await AssertHg.NftBalanceAsync(fxNft, fxSeller, 0);
    }
}
