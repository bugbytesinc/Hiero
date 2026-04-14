using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.NftTokens;

public class DissociateNftTests
{
    [Test]
    public async Task Can_Dissociate_Nft_From_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.Metadata = null!, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var association = await AssertHg.NftIsAssociatedAsync(fxNft, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        var receipt = await client.DissociateTokenAsync(new DissociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = [fxNft.CreateReceipt!.Token],
            Signatory = fxAccount.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);
    }

    [Test]
    public async Task Can_Dissociate_Nft_From_Alias_Account_Defect()
    {
        // DisAssociating an NFT with an account using its alias address has not yet been
        // implemented by the network, although it will accept the transaction.
        await using var fxAccount = await TestAliasAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.Metadata = null!);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount,
            Tokens = [fxNft.CreateReceipt!.Token],
            Signatory = fxAccount.PrivateKey
        });

        var association = await AssertHg.NftIsAssociatedAsync(fxNft, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        var ex = await Assert.That(async () =>
        {
            await client.DissociateTokenAsync(new DissociateTokenParams
            {
                Account = fxAccount.Alias,
                Tokens = [fxNft.CreateReceipt!.Token],
                Signatory = fxAccount.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Dissociate Token from Account failed with status: InvalidAccountId");

        await AssertHg.NftIsAssociatedAsync(fxNft, fxAccount);
    }

    [Test]
    public async Task Can_Dissociate_Nft_From_Account_And_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var association = await AssertHg.NftIsAssociatedAsync(fxNft, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        var receipt = await client.DissociateTokenAsync(new DissociateTokenParams
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
        await Assert.That(record.Fee >= 0UL && record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);
    }

    [Test]
    public async Task Can_Dissociate_Nft_From_Account_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxNft = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var association = await AssertHg.NftIsAssociatedAsync(fxNft, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        var receipt = await client.DissociateTokenAsync(fxNft.CreateReceipt!.Token, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = fxAccount.PrivateKey;
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);
    }

    [Test]
    public async Task Can_Dissociate_Nft_From_Account_And_Get_Record_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxNft = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var association = await AssertHg.NftIsAssociatedAsync(fxNft, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        var receipt = await client.DissociateTokenAsync(fxNft.CreateReceipt!.Token, fxAccount.CreateReceipt!.Address, ctx =>
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
        await Assert.That(record.Fee >= 0UL && record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(fxAccount.CreateReceipt!.Address);

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);
    }

    [Test]
    public async Task Can_Dissociate_Multiple_Nfts_With_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft1 = await TestNft.CreateAsync(null, fxAccount);
        await using var fxNft2 = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var nfts = new EntityId[] { fxNft1.CreateReceipt!.Token, fxNft2.CreateReceipt!.Token };

        await AssertHg.NftIsAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftIsAssociatedAsync(fxNft2, fxAccount);

        var receipt = await client.DissociateTokenAsync(new DissociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = nfts,
            Signatory = fxAccount.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.NftNotAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftNotAssociatedAsync(fxNft2, fxAccount);
    }

    [Test]
    public async Task Can_Dissociate_Multiple_Nfts_With_Account_And_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft1 = await TestNft.CreateAsync(null, fxAccount);
        await using var fxNft2 = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var nfts = new EntityId[] { fxNft1.CreateReceipt!.Token, fxNft2.CreateReceipt!.Token };

        await AssertHg.NftIsAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftIsAssociatedAsync(fxNft2, fxAccount);

        var receipt = await client.DissociateTokenAsync(new DissociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = nfts,
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
        await Assert.That(record.Fee >= 0UL && record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        await AssertHg.NftNotAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftNotAssociatedAsync(fxNft2, fxAccount);
    }

    [Test]
    public async Task Can_Dissociate_Multiple_Nfts_With_Account_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxNft1 = await TestNft.CreateAsync(null, fxAccount);
        await using var fxNft2 = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var nfts = new EntityId[] { fxNft1.CreateReceipt!.Token, fxNft2.CreateReceipt!.Token };

        await AssertHg.NftIsAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftIsAssociatedAsync(fxNft2, fxAccount);

        var receipt = await client.DissociateTokenAsync(new DissociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = nfts,
        }, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = fxAccount.PrivateKey;
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.NftNotAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftNotAssociatedAsync(fxNft2, fxAccount);
    }

    [Test]
    public async Task Can_Dissociate_Multiple_Nfts_With_Account_And_Get_Record_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxNft1 = await TestNft.CreateAsync(null, fxAccount);
        await using var fxNft2 = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var nfts = new EntityId[] { fxNft1.CreateReceipt!.Token, fxNft2.CreateReceipt!.Token };

        await AssertHg.NftIsAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftIsAssociatedAsync(fxNft2, fxAccount);

        var receipt = await client.DissociateTokenAsync(new DissociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = nfts,
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
        await Assert.That(record.Fee >= 0UL && record.Fee <= ulong.MaxValue).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(fxAccount.CreateReceipt!.Address);

        await AssertHg.NftNotAssociatedAsync(fxNft1, fxAccount);
        await AssertHg.NftNotAssociatedAsync(fxNft2, fxAccount);
    }

    [Test]
    public async Task No_Nft_Balance_Record_Exists_When_Dissociated()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var association = await AssertHg.NftIsAssociatedAsync(fxNft, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        var receipt = await client.DissociateTokenAsync(new DissociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = [fxNft.CreateReceipt!.Token],
            Signatory = fxAccount.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.NftNotAssociatedAsync(fxNft, fxAccount);
    }

    [Test]
    public async Task Dissociation_Requires_Signing_By_Target_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var association = await AssertHg.NftIsAssociatedAsync(fxNft, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        var ex = await Assert.That(async () =>
        {
            await client.DissociateTokenAsync(fxNft.CreateReceipt!.Token, fxAccount.CreateReceipt!.Address);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Dissociate Token from Account failed with status: InvalidSignature");

        association = await AssertHg.NftIsAssociatedAsync(fxNft, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Dissociation_Requires_Nft_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.DissociateTokenAsync(EntityId.None, fxAccount.CreateReceipt!.Address);
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("token");
        await Assert.That(ane.Message).StartsWith("Token is missing. Please check that it is not null or empty.");

        ex = await Assert.That(async () =>
        {
            await client.DissociateTokenAsync(null!, fxAccount.CreateReceipt!.Address);
        }).ThrowsException();
        ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("token");
        await Assert.That(ane.Message).StartsWith("Token is missing. Please check that it is not null or empty.");

        ex = await Assert.That(async () =>
        {
            await client.DissociateTokenAsync(new DissociateTokenParams
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
            await client.DissociateTokenAsync(new DissociateTokenParams
            {
                Account = fxAccount.CreateReceipt!.Address,
                Tokens = new EntityId[] { null! },
            });
        }).ThrowsException();
        var aoe = ex as ArgumentOutOfRangeException;
        await Assert.That(aoe).IsNotNull();
        await Assert.That(aoe!.ParamName).IsEqualTo("Tokens");
        await Assert.That(aoe.Message).StartsWith("The list of tokens cannot contain an empty or null address.");
    }

    [Test]
    public async Task Dissociation_Requires_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.DissociateTokenAsync(fxNft.CreateReceipt!.Token, null!);
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("account");
        await Assert.That(ane.Message).StartsWith("Account Address/Alias is missing. Please check that it is not null.");

        ex = await Assert.That(async () =>
        {
            await client.DissociateTokenAsync(fxNft.CreateReceipt!.Token, EntityId.None);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: InvalidAccountId");
    }

    [Test]
    public async Task Dissociating_With_Deleted_Account_Raises_Error()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = fxAccount,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fxAccount.PrivateKey
        });

        var ex = await Assert.That(async () =>
        {
            await client.DissociateTokenAsync(new DissociateTokenParams
            {
                Account = fxAccount.CreateReceipt!.Address,
                Tokens = [fxNft.CreateReceipt!.Token],
                Signatory = fxAccount.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountDeleted);
        await Assert.That(tex.Message).StartsWith("Dissociate Token from Account failed with status: AccountDelete");
    }

    [Test]
    public async Task Dissociating_With_Duplicate_Nft_Raises_Error()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.DissociateTokenAsync(new DissociateTokenParams
            {
                Account = fxAccount.CreateReceipt!.Address,
                Tokens = new EntityId[] { fxNft.CreateReceipt!.Token, fxNft.CreateReceipt!.Token },
                Signatory = fxAccount.PrivateKey
            });
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.TokenIdRepeatedInTokenList);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: TokenIdRepeatedInTokenList");
    }

    [Test]
    public async Task Dissociate_With_Dissociated_Nft_Raises_Error()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft1 = await TestNft.CreateAsync(null, fxAccount);
        await using var fxNft2 = await TestNft.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.DissociateTokenAsync(new DissociateTokenParams
            {
                Account = fxAccount.CreateReceipt!.Address,
                Tokens = new EntityId[] { fxNft1.CreateReceipt!.Token, fxNft2.CreateReceipt!.Token },
                Signatory = fxAccount.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenNotAssociatedToAccount);
        await Assert.That(tex.Message).StartsWith("Dissociate Token from Account failed with status: TokenNotAssociatedToAccount");
    }

    [Test]
    public async Task Can_Dissociate_Nft_From_Contract()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxContract.ContractReceipt!.Contract,
            Tokens = [fxNft.CreateReceipt!.Token],
            Signatory = fxContract.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetContractInfoAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(info).IsNotNull();

        var balances = await fxContract.GetTokenBalancesAsync();
        await Assert.That(balances).HasSingleItem();

        var association = balances[0];
        await Assert.That(association).IsNotNull();
        await Assert.That(association.Token).IsEqualTo(fxNft.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        receipt = await client.DissociateTokenAsync(new DissociateTokenParams
        {
            Account = fxContract.ContractReceipt!.Contract,
            Tokens = [fxNft.CreateReceipt!.Token],
            Signatory = fxContract.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        info = await client.GetContractInfoAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(info).IsNotNull();

        await Assert.That(await fxContract.GetTokenBalancesAsync()).IsEmpty();

        var ex = await Assert.That(async () =>
        {
            await client.TransferNftAsync(new Hiero.Nft(fxNft, 1), fxNft.TreasuryAccount.CreateReceipt!.Address, fxContract.ContractReceipt!.Contract, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenNotAssociatedToAccount);
        await Assert.That(tex.Message).StartsWith("NFT Transfer failed with status: TokenNotAssociatedToAccount");

        await Assert.That(await fxContract.GetTokenBalancesAsync()).IsEmpty();
        await Assert.That(await fxNft.TreasuryAccount.GetTokenBalanceAsync(fxNft)).IsEqualTo((long)fxNft.Metadata.Length);
    }

    [Test]
    public async Task Can_Not_Delete_Account_Having_Nft_Balance()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxAccount2 = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxNft = await TestNft.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        await using var client = await TestNetwork.CreateClientAsync();

        var nft = new Hiero.Nft(fxNft, 1);

        await AssertHg.NftBalanceAsync(fxNft, fxAccount1, 0);
        await AssertHg.NftBalanceAsync(fxNft, fxAccount2, 0);
        await AssertHg.NftBalanceAsync(fxNft, fxNft.TreasuryAccount, (ulong)fxNft.Metadata.Length);

        var receipt = await client.TransferNftAsync(nft, fxNft.TreasuryAccount, fxAccount1, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxNft.TreasuryAccount.PrivateKey);
        });

        await AssertHg.NftBalanceAsync(fxNft, fxAccount1, 1);
        await AssertHg.NftBalanceAsync(fxNft, fxAccount2, 0);
        await AssertHg.NftBalanceAsync(fxNft, fxNft.TreasuryAccount, (ulong)fxNft.Metadata.Length - 1);

        // Can't delete the account because it has NFTs associated with it.
        var ex = await Assert.That(async () =>
        {
            await client.DeleteAccountAsync(new DeleteAccountParams
            {
                Account = fxAccount1,
                FundsReceiver = fxAccount2,
                Signatory = fxAccount1.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TransactionRequiresZeroTokenBalances);
        await Assert.That(tex.Message).StartsWith("Delete Account failed with status: TransactionRequiresZeroTokenBalances");

        await AssertHg.NftBalanceAsync(fxNft, fxAccount1, 1);
        await AssertHg.NftBalanceAsync(fxNft, fxAccount2, 0);
        await AssertHg.NftBalanceAsync(fxNft, fxNft.TreasuryAccount, (ulong)fxNft.Metadata.Length - 1);

        await client.TransferNftAsync(nft, fxAccount1, fxAccount2, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey);
        });
        receipt = await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = fxAccount1,
            FundsReceiver = fxAccount2,
            Signatory = fxAccount1.PrivateKey
        });

        await AssertHg.NftBalanceAsync(fxNft, fxAccount2, 1);
        await AssertHg.NftBalanceAsync(fxNft, fxNft.TreasuryAccount, (ulong)fxNft.Metadata.Length - 1);
    }

    [Test]
    public async Task Can_Not_Schedule_Dissociate_Nft_From_Account()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxNft = await TestNft.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.NftIsAssociatedAsync(fxNft, fxAccount);

        var ex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new DissociateTokenParams
            {
                Tokens = [fxNft.CreateReceipt!.Token],
                Signatory = fxAccount.PrivateKey,
                Account = fxPayer
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
        await Assert.That(tex.Message).StartsWith("Scheduling Dissociate Token from Account failed with status: ScheduledTransactionNotInWhitelist");
    }
}
