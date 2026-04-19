using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.NftTokens;

public class GrantNftTests
{
    [Test]
    public async Task Can_Grant_Nfts()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftKycStatusAsync(fxNft, fxAccount, TokenKycStatus.Revoked);

        var receipt = await client.GrantTokenKycAsync(fxNft.CreateReceipt!.Token, fxAccount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.GrantPrivateKey);
        });

        await AssertHg.NftKycStatusAsync(fxNft, fxAccount, TokenKycStatus.Granted);

        receipt = await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.NftKycStatusAsync(fxNft, fxAccount, TokenKycStatus.Granted);
    }

    [Test]
    public async Task Grant_Nfts_To_Alias_Account_Defect()
    {
        // Granting Access to an asset with an account using its alias address has not yet been
        // implemented by the network, although it will accept the transaction.
        await using var fxAccount = await TestAliasAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await client.AssociateTokenAsync(fxNft.CreateReceipt!.Token, fxAccount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount.PrivateKey);
        });

        await AssertHg.NftKycStatusAsync(fxNft, fxAccount, TokenKycStatus.Revoked);

        var ex = await Assert.That(async () =>
        {
            await client.GrantTokenKycAsync(fxNft.CreateReceipt!.Token, fxAccount.Alias, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.GrantPrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Grant Token KYC failed with status: InvalidAccountId");

        await AssertHg.NftKycStatusAsync(fxNft, fxAccount, TokenKycStatus.Revoked);
    }

    [Test]
    public async Task Can_Grant_Nfts_And_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftKycStatusAsync(fxNft, fxAccount, TokenKycStatus.Revoked);

        var receipt = await client.GrantTokenKycAsync(fxNft.CreateReceipt!.Token, fxAccount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.GrantPrivateKey);
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
        await Assert.That(record.ParentTransactionConsensus).IsNull();

        await AssertHg.NftKycStatusAsync(fxNft, fxAccount, TokenKycStatus.Granted);

        await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.NftKycStatusAsync(fxNft, fxAccount, TokenKycStatus.Granted);
    }

    [Test]
    public async Task Can_Grant_Nfts_And_Get_Record_Without_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftKycStatusAsync(fxNft, fxAccount, TokenKycStatus.Revoked);

        var receipt = await client.GrantTokenKycAsync(fxNft.CreateReceipt!.Token, fxAccount, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.GrantPrivateKey));
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

        await AssertHg.NftKycStatusAsync(fxNft, fxAccount, TokenKycStatus.Granted);

        await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.NftKycStatusAsync(fxNft, fxAccount, TokenKycStatus.Granted);
    }

    [Test]
    public async Task Can_Grant_Nft_Coins_From_Any_Account_With_Grant_Key()
    {
        await using var fxOther = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftKycStatusAsync(fxNft, fxAccount, TokenKycStatus.Revoked);

        var receipt = await client.GrantTokenKycAsync(fxNft.CreateReceipt!.Token, fxAccount, ctx =>
        {
            ctx.Payer = fxOther.CreateReceipt!.Address;
            ctx.Signatory = new Signatory(fxOther.PrivateKey, fxNft.GrantPrivateKey);
        });

        await AssertHg.NftKycStatusAsync(fxNft, fxAccount, TokenKycStatus.Granted);

        await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.NftKycStatusAsync(fxNft, fxAccount, TokenKycStatus.Granted);
    }

    [Test]
    public async Task Grant_Nft_Coins_Requires_Grant_Key_Signature()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);

        var ex = await Assert.That(async () =>
        {
            await client.GrantTokenKycAsync(fxNft.CreateReceipt!.Token, fxAccount);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Grant Token KYC failed with status: InvalidSignature");
    }

    [Test]
    public async Task Cannot_Grant_Nft_Coins_When_Grant_KYC_Turned_Off()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftKycStatusAsync(fxNft, fxAccount, TokenKycStatus.NotApplicable);

        var ex = await Assert.That(async () =>
        {
            await client.GrantTokenKycAsync(fxNft.CreateReceipt!.Token, fxAccount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.GrantPrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoKycKey);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.TokenHasNoKycKey);
        await Assert.That(tex.Message).StartsWith("Grant Token KYC failed with status: TokenHasNoKycKey");
    }

    [Test]
    public async Task Can_Not_Schedule_Grant_Nft_Coins()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();
        var circulation = fxNft.Metadata.Length;
        var xferAmount = circulation / 3;

        await AssertHg.NftKycStatusAsync(fxNft, fxAccount, TokenKycStatus.Revoked);

        var ex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new GrantTokenKycParams
            {
                Holder = fxAccount,
                Token = fxNft.CreateReceipt!.Token,
                Signatory = fxNft.GrantPrivateKey,
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
        await Assert.That(tex.Receipt.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
        await Assert.That(tex.Message).StartsWith("Scheduling Grant Token KYC failed with status: ScheduledTransactionNotInWhitelist");
    }
}
