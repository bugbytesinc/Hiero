using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.NftTokens;

public class ConfiscateNftTests
{
    [Test]
    public async Task Can_Confiscate_Nft_Coins()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = (ulong)fxNft.Metadata.Length;
        var xferAmount = circulation / (ulong)Generator.Integer(3, 5) + 1;
        var expectedTreasury = (ulong)fxNft.Metadata.Length - xferAmount;
        var serialNumbersToConfiscate = Enumerable.Range(1, (int)xferAmount).Select(i => (long)i);

        var xferRecipt = await client.TransferAsync(new TransferParams
        {
            NftTransfers = serialNumbersToConfiscate.Select(sn => new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, sn), fxNft.TreasuryAccount, fxAccount)),
            Signatory = fxNft.TreasuryAccount
        });

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(circulation);

        var receipt = await client.ConfiscateNftsAsync(new ConfiscateNftParams
        {
            Token = fxNft.CreateReceipt!.Token,
            SerialNumbers = serialNumbersToConfiscate.ToArray(),
            Account = fxAccount,
            Signatory = fxNft.ConfiscatePrivateKey
        });

        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.Circulation).IsEqualTo(expectedTreasury);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(expectedTreasury);
    }

    [Test]
    public async Task Can_Confiscate_Single_Nft()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = (ulong)fxNft.Metadata.Length;
        var xferAmount = circulation / (ulong)Generator.Integer(3, 5) + 1;
        var expectedTreasury = (ulong)fxNft.Metadata.Length - xferAmount;
        var expectedCirculation = (ulong)fxNft.Metadata.Length - 1;
        var serialNumbersToXfer = Enumerable.Range(1, (int)xferAmount).Select(i => (long)i);

        var xferReceipt = await client.TransferAsync(new TransferParams
        {
            NftTransfers = serialNumbersToXfer.Select(sn => new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, sn), fxNft.TreasuryAccount, fxAccount)),
            Signatory = fxNft.TreasuryAccount
        });

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(circulation);

        var receipt = await client.ConfiscateNftAsync(new Hiero.Nft(fxNft.CreateReceipt!.Token, 1), fxAccount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.ConfiscatePrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.Circulation).IsEqualTo(expectedCirculation);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)(xferAmount - 1));
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(expectedCirculation);
    }

    [Test]
    public async Task Can_Confiscate_Nft_Coins_From_Alias()
    {
        await using var fxAccount = await TestAliasAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var client = await TestNetwork.CreateClientAsync();
        await client.AssociateTokenAsync(fxAccount, fxNft.CreateReceipt!.Token, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount.PrivateKey);
        });
        var circulation = (ulong)fxNft.Metadata.Length;
        var xferAmount = circulation / (ulong)Generator.Integer(3, 5) + 1;
        var expectedTreasury = (ulong)fxNft.Metadata.Length - xferAmount;
        var serialNumbersToConfiscate = Enumerable.Range(1, (int)xferAmount).Select(i => (long)i);

        var xfrReceipt = await client.TransferAsync(new TransferParams
        {
            NftTransfers = serialNumbersToConfiscate.Select(sn => new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, sn), fxNft.TreasuryAccount, fxAccount)),
            Signatory = fxNft.TreasuryAccount
        });

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(circulation);

        var receipt = await client.ConfiscateNftsAsync(new ConfiscateNftParams
        {
            Token = fxNft.CreateReceipt!.Token,
            SerialNumbers = serialNumbersToConfiscate.ToArray(),
            Account = fxAccount.Alias,
            Signatory = fxNft.ConfiscatePrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.Circulation).IsEqualTo(expectedTreasury);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(expectedTreasury);
    }

    [Test]
    public async Task Can_Confiscate_A_Single_Nft()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var initialCirculation = (ulong)fxNft.Metadata.Length;
        var xferAmount = 2ul;
        var expectedTreasury = initialCirculation - xferAmount;
        var serialNumbersTransfered = Enumerable.Range(1, 2).Select(i => (long)i);

        var transferParams = new TransferParams
        {
            NftTransfers = serialNumbersTransfered.Select(sn => new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, sn), fxNft.TreasuryAccount, fxAccount)),
            Signatory = fxNft.TreasuryAccount
        };

        var xferReceipt = await client.TransferAsync(transferParams);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(initialCirculation);

        var receipt = await client.ConfiscateNftAsync(new Hiero.Nft(fxNft.CreateReceipt!.Token, 1), fxAccount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.ConfiscatePrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.Circulation).IsEqualTo(initialCirculation - 1);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(initialCirculation - 1);
    }

    [Test]
    public async Task Can_Confiscate_An_Nft_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var initialCirculation = (ulong)fxNft.Metadata.Length;
        var xferAmount = 2ul;
        var expectedTreasury = initialCirculation - xferAmount;
        var serialNumbersTransfered = Enumerable.Range(1, 2).Select(i => (long)i);

        var transferParams = new TransferParams
        {
            NftTransfers = serialNumbersTransfered.Select(sn => new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, sn), fxNft.TreasuryAccount, fxAccount)),
            Signatory = fxNft.TreasuryAccount
        };

        var xferReceipt = await client.TransferAsync(transferParams);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(initialCirculation);

        var receipt = await client.ConfiscateNftAsync(new Hiero.Nft(fxNft.CreateReceipt!.Token, 1), fxAccount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.ConfiscatePrivateKey);
        });
        var record = (TokenRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.Circulation).IsEqualTo(initialCirculation - 1);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(initialCirculation - 1);
        await Assert.That(record.ParentTransactionConsensus).IsNull();
    }

    [Test]
    public async Task Can_Confiscate_An_Nfts_And_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var initialCirculation = (ulong)fxNft.Metadata.Length;
        var xferAmount = 2ul;
        var expectedTreasury = initialCirculation - xferAmount;
        var expectedCirculation = initialCirculation - xferAmount;
        var serialNumbersTransfered = Enumerable.Range(1, 2).Select(i => (long)i);

        var transferParams = new TransferParams
        {
            NftTransfers = serialNumbersTransfered.Select(sn => new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, sn), fxNft.TreasuryAccount, fxAccount)),
            Signatory = fxNft.TreasuryAccount
        };

        var xferReceipt = await client.TransferAsync(transferParams);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(initialCirculation);

        var receipt = await client.ConfiscateNftsAsync(new ConfiscateNftParams
        {
            Token = fxNft.CreateReceipt!.Token,
            SerialNumbers = serialNumbersTransfered.ToArray(),
            Account = fxAccount,
            Signatory = fxNft.ConfiscatePrivateKey
        });
        var record = (TokenRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.Circulation).IsEqualTo(expectedCirculation);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(expectedCirculation);
        await Assert.That(record.ParentTransactionConsensus).IsNull();
    }

    [Test]
    public async Task Can_Confiscate_A_Small_Amount_Nft_Coins_And_Get_Record_Without_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var initialCirculation = (ulong)fxNft.Metadata.Length;
        var xferAmount = 2ul;
        var expectedTreasury = initialCirculation - xferAmount;
        var serialNumbersTransfered = Enumerable.Range(1, 2).Select(i => (long)i);

        var transferParams = new TransferParams
        {
            NftTransfers = serialNumbersTransfered.Select(sn => new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, sn), fxNft.TreasuryAccount, fxAccount)),
            Signatory = fxNft.TreasuryAccount
        };

        var xferReceipt = await client.TransferAsync(transferParams);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(initialCirculation);

        var receipt = await client.ConfiscateNftsAsync(new ConfiscateNftParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Account = fxAccount,
            SerialNumbers = serialNumbersTransfered.ToArray(),
            Signatory = fxNft.ConfiscatePrivateKey
        });
        var record = (TokenRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.Circulation).IsEqualTo(expectedTreasury);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(0);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(expectedTreasury);
    }

    [Test]
    public async Task Can_Confiscate_Single_Nft_And_Get_Record_Without_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var initialCirculation = (ulong)fxNft.Metadata.Length;
        var xferAmount = 2ul;
        var expectedTreasury = initialCirculation - xferAmount;
        var expectedCirculation = initialCirculation - 1;
        var serialNumbersTransfered = Enumerable.Range(1, 2).Select(i => (long)i);

        var transferParams = new TransferParams
        {
            NftTransfers = serialNumbersTransfered.Select(sn => new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, sn), fxNft.TreasuryAccount, fxAccount)),
            Signatory = fxNft.TreasuryAccount
        };

        await client.TransferAsync(transferParams);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(initialCirculation);

        var receipt = await client.ConfiscateNftAsync(new Hiero.Nft(fxNft.CreateReceipt!.Token, 1), fxAccount, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.ConfiscatePrivateKey));
        var record = (TokenRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.Circulation).IsEqualTo(expectedCirculation);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)(xferAmount - 1));
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(expectedCirculation);
    }

    [Test]
    public async Task Can_Confiscate_An_Nft_From_Any_Account_With_Confiscate_Key()
    {
        await using var fxOther = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var initialCirculation = (ulong)fxNft.Metadata.Length;
        var xferAmount = 2ul;
        var expectedTreasury = initialCirculation - xferAmount;
        var serialNumbersTransfered = Enumerable.Range(1, 2).Select(i => (long)i);

        var transferParams = new TransferParams
        {
            NftTransfers = serialNumbersTransfered.Select(sn => new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, sn), fxNft.TreasuryAccount, fxAccount)),
            Signatory = fxNft.TreasuryAccount
        };

        var xferReceipt = await client.TransferAsync(transferParams);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(initialCirculation);

        var receipt = await client.ConfiscateNftAsync(new Hiero.Nft(fxNft, 1), fxAccount, ctx =>
        {
            ctx.Payer = fxOther.CreateReceipt!.Address;
            ctx.Signatory = new Signatory(fxOther.PrivateKey, fxNft.ConfiscatePrivateKey);
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(receipt.Circulation).IsEqualTo(initialCirculation - 1);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo(1);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(initialCirculation - 1);
    }

    [Test]
    public async Task Can_Not_Confiscate_More_Nfts_Than_Account_Has()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var initialCirculation = (ulong)fxNft.Metadata.Length;
        var xferAmount = 2ul;
        var expectedTreasury = initialCirculation - xferAmount;
        var serialNumbersTransfered = Enumerable.Range(1, 2).Select(i => (long)i);

        var transferParams = new TransferParams
        {
            NftTransfers = serialNumbersTransfered.Select(sn => new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, sn), fxNft.TreasuryAccount, fxAccount)),
            Signatory = fxNft.TreasuryAccount
        };

        var xferReceipt = await client.TransferAsync(transferParams);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(initialCirculation);

        var ex = await Assert.That(async () =>
        {
            await client.ConfiscateNftsAsync(new ConfiscateNftParams
            {
                Token = fxNft.CreateReceipt!.Token,
                SerialNumbers = new long[] { 1, 2, 3 },
                Account = fxAccount,
                Signatory = fxNft.ConfiscatePrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountDoesNotOwnWipedNft);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.AccountDoesNotOwnWipedNft);
        await Assert.That(tex.Message).StartsWith("Confiscate NFT failed with status: AccountDoesNotOwnWipedNft");

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(initialCirculation);
    }

    [Test]
    public async Task Confiscate_Record_Includes_Nft_Transfers()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var initialCirculation = (ulong)fxNft.Metadata.Length;
        var xferAmount = 2ul;
        var expectedTreasury = initialCirculation - xferAmount;
        var serialNumbersTransfered = Enumerable.Range(1, 2).Select(i => (long)i);

        var transferParams = new TransferParams
        {
            NftTransfers = serialNumbersTransfered.Select(sn => new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, sn), fxNft.TreasuryAccount, fxAccount)),
            Signatory = fxNft.TreasuryAccount
        };

        var xferReceipt = await client.TransferAsync(transferParams);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(initialCirculation);

        var receipt = await client.ConfiscateNftAsync(new Hiero.Nft(fxNft, 1), fxAccount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.ConfiscatePrivateKey);
        });
        var record = (TokenRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Circulation).IsEqualTo(initialCirculation - 1);
        await Assert.That(record.TokenTransfers).IsEmpty();
        await Assert.That(record.NftTransfers).HasSingleItem();

        var xfer = record.NftTransfers[0];
        await Assert.That(xfer.Nft.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(xfer.From).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(xfer.To).IsEqualTo(EntityId.None);
    }

    [Test]
    public async Task Confiscation_Requires_Confiscate_Key_Signature()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var initialCirculation = (ulong)fxNft.Metadata.Length;
        var xferAmount = 2ul;
        var expectedTreasury = initialCirculation - xferAmount;
        var serialNumbersTransfered = Enumerable.Range(1, 2).Select(i => (long)i);

        var transferParams = new TransferParams
        {
            NftTransfers = serialNumbersTransfered.Select(sn => new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, sn), fxNft.TreasuryAccount, fxAccount)),
            Signatory = fxNft.TreasuryAccount
        };

        var xferReceipt = await client.TransferAsync(transferParams);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(initialCirculation);

        var ex = await Assert.That(async () =>
        {
            await client.ConfiscateNftsAsync(new ConfiscateNftParams
            {
                Token = fxNft.CreateReceipt!.Token,
                SerialNumbers = new long[] { 1 },
                Account = fxAccount,
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Confiscate NFT failed with status: InvalidSignature");

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(initialCirculation);
    }

    [Test]
    public async Task Cannot_Confiscate_When_No_Confiscation_Endorsement()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.ConfiscateEndorsement = null;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var initialCirculation = (ulong)fxNft.Metadata.Length;
        var xferAmount = 2ul;
        var expectedTreasury = initialCirculation - xferAmount;
        var serialNumbersTransfered = Enumerable.Range(1, 2).Select(i => (long)i);

        var transferParams = new TransferParams
        {
            NftTransfers = serialNumbersTransfered.Select(sn => new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, sn), fxNft.TreasuryAccount, fxAccount)),
            Signatory = fxNft.TreasuryAccount
        };

        var xferRecipt = await client.TransferAsync(transferParams);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(initialCirculation);

        var ex = await Assert.That(async () =>
        {
            await client.ConfiscateNftAsync(new Hiero.Nft(fxNft, 1), fxAccount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.ConfiscatePrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoWipeKey);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.TokenHasNoWipeKey);
        await Assert.That(tex.Message).StartsWith("Confiscate NFT failed with status: TokenHasNoWipeKey");

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(initialCirculation);
    }

    [Test]
    public async Task Can_Not_Schedule_Confiscate_Nft_Coins()
    {
        await using var fxPayer = await TestAccount.CreateAsync();
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var initialCirculation = (ulong)fxNft.Metadata.Length;
        var xferAmount = 2ul;
        var expectedTreasury = initialCirculation - xferAmount;
        var serialNumbersTransfered = Enumerable.Range(1, 2).Select(i => (long)i);

        var transferParams = new TransferParams
        {
            NftTransfers = serialNumbersTransfered.Select(sn => new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, sn), fxNft.TreasuryAccount, fxAccount)),
            Signatory = fxNft.TreasuryAccount
        };

        var xferReceipt = await client.TransferAsync(transferParams);

        await Assert.That(await fxAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)xferAmount);
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)expectedTreasury);
        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(initialCirculation);

        var ex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ConfiscateNftParams
            {
                Token = fxNft,
                SerialNumbers = [1],
                Account = fxAccount,
                Signatory = fxNft.ConfiscatePrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
        await Assert.That(tex.Message).StartsWith("Scheduling Confiscate NFT failed with status: ScheduledTransactionNotInWhitelist");
    }

    [Test]
    public async Task Can_Confiscate_Nft_From_Treasury()
    {
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = (ulong)fxNft.Metadata.Length;

        var ex = await Assert.That(async () =>
        {
            await client.ConfiscateNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.ConfiscatePrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.CannotWipeTokenTreasuryAccount);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.CannotWipeTokenTreasuryAccount);
        await Assert.That(tex.Message).StartsWith("Confiscate NFT failed with status: CannotWipeTokenTreasuryAccount");

        await Assert.That((await client.GetTokenInfoAsync(fxNft)).Circulation).IsEqualTo(circulation);
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Confiscate_Nft()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var serialNumber = fxNft.MintReceipt!.SerialNumbers[0];
        await client.TransferAsync(new TransferParams
        {
            NftTransfers = new[] { new NftTransfer(new Hiero.Nft(fxNft.CreateReceipt!.Token, serialNumber), fxNft.TreasuryAccount, fxAccount) },
            Signatory = fxNft.TreasuryAccount
        });

        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new ConfiscateNftParams
                {
                    Token = fxNft.CreateReceipt!.Token,
                    SerialNumbers = new[] { serialNumber },
                    Account = fxAccount.CreateReceipt!.Address,
                    Signatory = fxNft.ConfiscatePrivateKey,
                },
                Payer = fxPayer
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<TransactionException>();
        await Assert.That(((TransactionException)tex!).Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    }
}
