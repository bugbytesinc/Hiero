using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
namespace Hiero.Test.Integration.NftTokens;

public class SuspendNftTests
{
    [Test]
    public async Task Can_Suspend_Nft_Trading()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = false;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);

        await client.SuspendTokenAsync(new SuspendTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Holder = fxAccount.CreateReceipt!.Address,
            Signatory = fxNft.SuspendPrivateKey,
        });

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Suspended);

        var ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: AccountFrozenForToken");

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Suspended);
    }

    [Test]
    public async Task Suspend_Nft_Trading_Of_Alias_Account_Defect()
    {
        // Defect 0.21.0: Suspending an NFT with an account using its alias address has not yet been
        // implemented by the network, although it will accept the transaction.
        await using var fxAccount = await TestAliasAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = false;
        });
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = [fxNft.CreateReceipt!.Token],
            Signatory = fxAccount.PrivateKey,
        });

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);

        var ex = await Assert.That(async () =>
        {
            await client.SuspendTokenAsync(new SuspendTokenParams
            {
                Token = fxNft.CreateReceipt!.Token,
                Holder = fxAccount.Alias,
                Signatory = fxNft.SuspendPrivateKey,
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Suspend Token failed with status: InvalidAccountId");

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);
    }

    [Test]
    public async Task Can_Suspend_Nft_Trading_And_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = false;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);

        var receipt = await client.SuspendTokenAsync(new SuspendTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Holder = fxAccount.CreateReceipt!.Address,
            Signatory = fxNft.SuspendPrivateKey,
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Suspended);

        var ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: AccountFrozenForToken");

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Suspended);
    }

    [Test]
    public async Task Can_Suspend_Nft_Trading_And_Get_Record_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = false;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);

        var receipt = await client.SuspendTokenAsync(fxNft.CreateReceipt!.Token, fxAccount.CreateReceipt!.Address, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.SuspendPrivateKey));
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Suspended);

        var ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: AccountFrozenForToken");

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Suspended);
    }

    [Test]
    public async Task Can_Suspend_Nft_Trading_From_Any_Account_With_Suspend_Key()
    {
        await using var fxOther = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = false;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);

        await client.SuspendTokenAsync(new SuspendTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Holder = fxAccount.CreateReceipt!.Address,
            Signatory = fxNft.SuspendPrivateKey,
        }, ctx =>
        {
            ctx.Payer = fxOther.CreateReceipt!.Address;
            ctx.Signatory = fxOther.PrivateKey;
        });

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Suspended);

        var ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: AccountFrozenForToken");

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Suspended);
    }

    [Test]
    public async Task Suspending_A_Frozen_Account_Is_A_Noop()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = true;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Suspended);

        var ex = await Assert.That(async () =>
        {
            await client.SuspendTokenAsync(new SuspendTokenParams
            {
                Token = fxNft.CreateReceipt!.Token,
                Holder = fxAccount.CreateReceipt!.Address,
                Signatory = fxNft.SuspendPrivateKey,
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("Suspend Token failed with status: AccountFrozenForToken");

        ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: AccountFrozenForToken");
    }

    [Test]
    public async Task Can_Suspend_A_Resumed_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = true;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Suspended);

        await client.ResumeTokenAsync(new ResumeTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Holder = fxAccount.CreateReceipt!.Address,
            Signatory = fxNft.SuspendPrivateKey,
        });

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);

        await client.SuspendTokenAsync(new SuspendTokenParams
        {
            Token = fxNft.CreateReceipt!.Token,
            Holder = fxAccount.CreateReceipt!.Address,
            Signatory = fxNft.SuspendPrivateKey,
        });

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Suspended);

        var ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountFrozenForToken);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: AccountFrozenForToken");

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Suspended);
    }

    [Test]
    public async Task Suspend_Nft_Requires_Suspend_Key_To_Sign_Transaction()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = false;
        });
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);

        var ex = await Assert.That(async () =>
        {
            await client.SuspendTokenAsync(fxNft.CreateReceipt!.Token, fxAccount.CreateReceipt!.Address);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Suspend Token failed with status: InvalidSignature");

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);
    }

    [Test]
    public async Task Cannot_Suspend_Nft_When_Freeze_Not_Enabled()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.SuspendEndorsement = null;
            fx.CreateParams.InitializeSuspended = false;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.NotApplicable);

        var ex = await Assert.That(async () =>
        {
            await client.SuspendTokenAsync(new SuspendTokenParams
            {
                Token = fxNft.CreateReceipt!.Token,
                Holder = fxAccount.CreateReceipt!.Address,
                Signatory = fxNft.SuspendPrivateKey,
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenHasNoFreezeKey);
        await Assert.That(tex.Message).StartsWith("Suspend Token failed with status: TokenHasNoFreezeKey");

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.NotApplicable);

        await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.NotApplicable);
    }

    [Test]
    public async Task Can_Not_Schedule_Suspend_Nft_Trading()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx =>
        {
            fx.CreateParams.GrantKycEndorsement = null;
            fx.CreateParams.InitializeSuspended = false;
        }, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftTradableStatusAsync(fxNft, fxAccount, TokenTradableStatus.Tradable);
        var ex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new SuspendTokenParams
            {
                Token = fxNft.CreateReceipt!.Token,
                Holder = fxAccount.CreateReceipt!.Address,
                Signatory = fxNft.SuspendPrivateKey,
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
        await Assert.That(tex.Message).StartsWith("Scheduling Suspend Token failed with status: ScheduledTransactionNotInWhitelist");
    }
}
