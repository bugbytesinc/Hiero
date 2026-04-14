using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.NftTokens;

public class AssociateNftTests
{
    [Test]
    public async Task Can_Associate_Nft_With_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.Metadata = null!);

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = [fxNft.CreateReceipt!.Token],
            Signatory = fxAccount.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var association = await AssertHg.NftIsAssociatedAsync(fxNft, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Associate_Nft_With_Alias_Account_Defect()
    {
        // Associating an NFT with an account using its alias address has not yet been
        // implemented by the network, although it will accept the transaction.
        await using var fxAccount = await TestAliasAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.Metadata = null!);

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.AssociateTokensAsync(new AssociateTokenParams
            {
                Account = fxAccount.Alias,
                Tokens = [fxNft.CreateReceipt!.Token],
                Signatory = fxAccount.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Associate Token with Account failed with status: InvalidAccountId");

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);
    }

    [Test]
    public async Task Can_Associate_Nft_With_Account_And_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.Metadata = null!);

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = [fxNft.CreateReceipt!.Token],
            Signatory = fxAccount.PrivateKey
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsGreaterThanOrEqualTo(0UL);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);
        await Assert.That(record.ParentTransactionConsensus).IsNull();

        var association = await AssertHg.NftIsAssociatedAsync(fxNft, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Associate_Nft_With_Account_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxNft = await TestNft.CreateAsync();

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokenAsync(fxAccount.CreateReceipt!.Address, fxNft.CreateReceipt!.Token, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = fxAccount.PrivateKey;
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var association = await AssertHg.NftIsAssociatedAsync(fxNft, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Associate_Nft_With_Account_And_Get_Record_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxNft = await TestNft.CreateAsync();

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = [fxNft.CreateReceipt!.Token],
        }, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = fxAccount.PrivateKey;
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsGreaterThanOrEqualTo(0UL);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(fxAccount.CreateReceipt!.Address);

        var association = await AssertHg.NftIsAssociatedAsync(fxNft, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Associate_Multiple_Nfts_With_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft1 = await TestNft.CreateAsync();
        await using var fxNft2 = await TestNft.CreateAsync();
        await using var fxToken3 = await TestToken.CreateAsync();

        var tokens = new EntityId[] { fxNft1.CreateReceipt!.Token, fxNft2.CreateReceipt!.Token, fxToken3.CreateReceipt!.Token };

        await AssertHg.NftNotAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftNotAssociatedAsync(fxNft2, fxAccount);
        await AssertHg.TokenNotAssociatedAsync(fxToken3, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = tokens,
            Signatory = fxAccount.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var association = await AssertHg.NftIsAssociatedAsync(fxNft1, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft1.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        association = await AssertHg.NftIsAssociatedAsync(fxNft2, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft2.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        association = await AssertHg.TokenIsAssociatedAsync(fxToken3, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken3.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Associate_Multiple_Nfts_With_Account_And_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft1 = await TestNft.CreateAsync();
        await using var fxNft2 = await TestNft.CreateAsync();

        var tokens = new EntityId[] { fxNft1.CreateReceipt!.Token, fxNft2.CreateReceipt!.Token };

        await AssertHg.NftNotAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftNotAssociatedAsync(fxNft2, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = tokens,
            Signatory = fxAccount.PrivateKey
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsGreaterThanOrEqualTo(0UL);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        var association = await AssertHg.NftIsAssociatedAsync(fxNft1, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft1.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        association = await AssertHg.NftIsAssociatedAsync(fxNft2, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft2.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Associate_Multiple_Nfts_With_Account_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxNft1 = await TestNft.CreateAsync();
        await using var fxNft2 = await TestNft.CreateAsync();

        var tokens = new EntityId[] { fxNft1.CreateReceipt!.Token, fxNft2.CreateReceipt!.Token };

        await AssertHg.NftNotAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftNotAssociatedAsync(fxNft2, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = tokens,
        }, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = fxAccount.PrivateKey;
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var association = await AssertHg.NftIsAssociatedAsync(fxNft1, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft1.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        association = await AssertHg.NftIsAssociatedAsync(fxNft2, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft2.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Associate_Multiple_Nfts_With_Account_And_Get_Record_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxNft1 = await TestNft.CreateAsync();
        await using var fxNft2 = await TestNft.CreateAsync();

        var tokens = new EntityId[] { fxNft1.CreateReceipt!.Token, fxNft2.CreateReceipt!.Token };

        await AssertHg.NftNotAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftNotAssociatedAsync(fxNft2, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = tokens,
        }, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = fxAccount.PrivateKey;
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsGreaterThanOrEqualTo(0UL);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(fxAccount.CreateReceipt!.Address);

        var association = await AssertHg.NftIsAssociatedAsync(fxNft1, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft1.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        association = await AssertHg.NftIsAssociatedAsync(fxNft2, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft2.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task No_Nft_Balance_Record_Exists_When_Not_Associated()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync();

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = [fxNft.CreateReceipt!.Token],
            Signatory = fxAccount.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var association = await AssertHg.NftIsAssociatedAsync(fxNft, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Association_Requires_Signing_By_Target_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync();

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.AssociateTokenAsync(fxAccount.CreateReceipt!.Address, fxNft.CreateReceipt!.Token);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Associate Token with Account failed with status: InvalidSignature");

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);
    }

    [Test]
    public async Task Association_Requires_Nft_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.AssociateTokenAsync(fxAccount.CreateReceipt!.Address, EntityId.None);
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("token");
        await Assert.That(ane.Message).StartsWith("Token is missing. Please check that it is not null or empty.");

        ex = await Assert.That(async () =>
        {
            await client.AssociateTokenAsync(fxAccount.CreateReceipt!.Address, null!);
        }).ThrowsException();
        ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("token");
        await Assert.That(ane.Message).StartsWith("Token is missing. Please check that it is not null or empty.");

        ex = await Assert.That(async () =>
        {
            await client.AssociateTokensAsync(new AssociateTokenParams
            {
                Account = fxAccount.CreateReceipt!.Address,
                Tokens = null!,
            });
        }).ThrowsException();
        ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("Tokens");
        await Assert.That(ane.Message).StartsWith("The list of tokens cannot be null.");

        ex = await Assert.That(async () =>
        {
            await client.AssociateTokensAsync(new AssociateTokenParams
            {
                Account = fxAccount.CreateReceipt!.Address,
                Tokens = [null!],
            });
        }).ThrowsException();
        var aoe = ex as ArgumentOutOfRangeException;
        await Assert.That(aoe).IsNotNull();
        await Assert.That(aoe!.ParamName).IsEqualTo("Tokens");
        await Assert.That(aoe.Message).StartsWith("The list of tokens cannot contain an empty or null address.");
    }

    [Test]
    public async Task Association_Requires_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync();

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.AssociateTokenAsync(null!, fxNft.CreateReceipt!.Token);
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("account");
        await Assert.That(ane.Message).StartsWith("Account Address/Alias is missing. Please check that it is not null.");

        ex = await Assert.That(async () =>
        {
            await client.AssociateTokenAsync(EntityId.None, fxNft.CreateReceipt!.Token);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: InvalidAccountId");
    }

    [Test]
    public async Task Associating_With_Deleted_Account_Raises_Error()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync();

        await using var client = await TestNetwork.CreateClientAsync();
        await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fxAccount.PrivateKey
        });

        var ex = await Assert.That(async () =>
        {
            await client.AssociateTokensAsync(new AssociateTokenParams
            {
                Account = fxAccount.CreateReceipt!.Address,
                Tokens = [fxNft.CreateReceipt!.Token],
                Signatory = fxAccount.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountDeleted);
        await Assert.That(tex.Message).StartsWith("Associate Token with Account failed with status: AccountDeleted");
    }

    [Test]
    public async Task Associating_With_Duplicate_Nft_Raises_Error()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync();

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            var tokens = new EntityId[] { fxNft.CreateReceipt!.Token, fxNft.CreateReceipt!.Token };
            await client.AssociateTokensAsync(new AssociateTokenParams
            {
                Account = fxAccount.CreateReceipt!.Address,
                Tokens = tokens,
                Signatory = fxAccount.PrivateKey
            });
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.TokenIdRepeatedInTokenList);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: TokenIdRepeatedInTokenList");
    }

    [Test]
    public async Task Associate_With_Associated_Nft_Raises_Error()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft1 = await TestNft.CreateAsync(null, fxAccount);
        await using var fxNft2 = await TestNft.CreateAsync();

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            var tokens = new EntityId[] { fxNft1.CreateReceipt!.Token, fxNft2.CreateReceipt!.Token };
            await client.AssociateTokensAsync(new AssociateTokenParams
            {
                Account = fxAccount.CreateReceipt!.Address,
                Tokens = tokens,
                Signatory = fxAccount.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenAlreadyAssociatedToAccount);
        await Assert.That(tex.Message).StartsWith("Associate Token with Account failed with status: TokenAlreadyAssociatedToAccount");
    }

    [Test]
    public async Task Can_Associate_Nft_With_Contract()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync();

        // Assert Not Associated
        var info = await fxContract.GetTokenBalancesAsync();
        await Assert.That(info.FirstOrDefault(t => t.Token == fxNft.CreateReceipt!.Token)).IsNull();

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxContract.ContractReceipt!.Contract,
            Tokens = [fxNft.CreateReceipt!.Token],
            Signatory = fxContract.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        info = await fxContract.GetTokenBalancesAsync();
        var association = info.FirstOrDefault(t => t.Token == fxNft.CreateReceipt!.Token);
        await Assert.That(association).IsNotNull();
        await Assert.That(association!.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Can_Not_Schedule_Associate_Nft_With_Account()
    {
        await using var fxPayer = await TestAccount.CreateAsync();
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync();

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new AssociateTokenParams
            {
                Account = fxAccount.CreateReceipt!.Address,
                Tokens = [fxNft.CreateReceipt!.Token],
                Signatory = fxAccount.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
        await Assert.That(tex.Message).StartsWith("Scheduling Associate Token with Account failed with status: ScheduledTransactionNotInWhitelist");
    }
}
