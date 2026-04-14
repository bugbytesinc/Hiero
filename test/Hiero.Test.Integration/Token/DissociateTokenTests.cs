using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Token;

public class DissociateTokenTests
{
    [Test]
    public async Task Can_Dissociate_Token_From_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);

        var association = await AssertHg.TokenIsAssociatedAsync(fxToken, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.DissociateTokenAsync(new DissociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = [fxToken.CreateReceipt!.Token],
            Signatory = fxAccount.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.TokenNotAssociatedAsync(fxToken, fxAccount);
    }

    [Test]
    public async Task Dissociate_Token_From_Alias_Account_Is_Unsupported()
    {
        await using var fxAccount = await TestAliasAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount,
            Tokens = [fxToken.CreateReceipt!.Token],
            Signatory = fxAccount.PrivateKey
        });

        var association = await AssertHg.TokenIsAssociatedAsync(fxToken, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        var ex = await Assert.That(async () =>
        {
            await client.DissociateTokenAsync(new DissociateTokenParams
            {
                Account = fxAccount.Alias,
                Tokens = [fxToken.CreateReceipt!.Token],
                Signatory = fxAccount.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Dissociate Token from Account failed with status: InvalidAccountId");

        await AssertHg.TokenIsAssociatedAsync(fxToken, fxAccount);
    }

    [Test]
    public async Task Can_Dissociate_Token_From_Account_And_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);

        var association = await AssertHg.TokenIsAssociatedAsync(fxToken, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.DissociateTokenAsync(new DissociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = [fxToken.CreateReceipt!.Token],
            Signatory = fxAccount.PrivateKey
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        await AssertHg.TokenNotAssociatedAsync(fxToken, fxAccount);
    }

    [Test]
    public async Task Can_Dissociate_Token_From_Account_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);

        var association = await AssertHg.TokenIsAssociatedAsync(fxToken, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.DissociateTokenAsync(fxToken.CreateReceipt!.Token, fxAccount.CreateReceipt!.Address, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = fxAccount.PrivateKey;
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.TokenNotAssociatedAsync(fxToken, fxAccount);
    }

    [Test]
    public async Task Can_Dissociate_Token_From_Account_And_Get_Record_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);

        var association = await AssertHg.TokenIsAssociatedAsync(fxToken, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.DissociateTokenAsync(new DissociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = [fxToken.CreateReceipt!.Token],
        }, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = fxAccount.PrivateKey;
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(fxAccount.CreateReceipt!.Address);

        await AssertHg.TokenNotAssociatedAsync(fxToken, fxAccount);
    }

    [Test]
    public async Task Can_Dissociate_Multiple_Tokens_With_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken1 = await TestToken.CreateAsync(null, fxAccount);
        await using var fxToken2 = await TestToken.CreateAsync(null, fxAccount);

        var tokens = new EntityId[] { fxToken1.CreateReceipt!.Token, fxToken2.CreateReceipt!.Token };

        await AssertHg.TokenIsAssociatedAsync(fxToken1, fxAccount);
        await AssertHg.TokenIsAssociatedAsync(fxToken2, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.DissociateTokenAsync(new DissociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = tokens,
            Signatory = fxAccount.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.TokenNotAssociatedAsync(fxToken1, fxAccount);
        await AssertHg.TokenNotAssociatedAsync(fxToken2, fxAccount);
    }

    [Test]
    public async Task Can_Dissociate_Multiple_Tokens_With_Account_And_Get_Record()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken1 = await TestToken.CreateAsync(null, fxAccount);
        await using var fxToken2 = await TestToken.CreateAsync(null, fxAccount);

        var tokens = new EntityId[] { fxToken1.CreateReceipt!.Token, fxToken2.CreateReceipt!.Token };

        await AssertHg.TokenIsAssociatedAsync(fxToken1, fxAccount);
        await AssertHg.TokenIsAssociatedAsync(fxToken2, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.DissociateTokenAsync(new DissociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = tokens,
            Signatory = fxAccount.PrivateKey
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        await AssertHg.TokenNotAssociatedAsync(fxToken1, fxAccount);
        await AssertHg.TokenNotAssociatedAsync(fxToken2, fxAccount);
    }

    [Test]
    public async Task Can_Dissociate_Multiple_Tokens_With_Account_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxToken1 = await TestToken.CreateAsync(null, fxAccount);
        await using var fxToken2 = await TestToken.CreateAsync(null, fxAccount);

        var tokens = new EntityId[] { fxToken1.CreateReceipt!.Token, fxToken2.CreateReceipt!.Token };

        await AssertHg.TokenIsAssociatedAsync(fxToken1, fxAccount);
        await AssertHg.TokenIsAssociatedAsync(fxToken2, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.DissociateTokenAsync(new DissociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = tokens,
        }, ctx =>
        {
            ctx.Payer = fxAccount.CreateReceipt!.Address;
            ctx.Signatory = fxAccount.PrivateKey;
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.TokenNotAssociatedAsync(fxToken1, fxAccount);
        await AssertHg.TokenNotAssociatedAsync(fxToken2, fxAccount);
    }

    [Test]
    public async Task Can_Dissociate_Multiple_Tokens_With_Account_And_Get_Record_No_Extra_Signatory()
    {
        await using var fxAccount = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxToken1 = await TestToken.CreateAsync(null, fxAccount);
        await using var fxToken2 = await TestToken.CreateAsync(null, fxAccount);

        var tokens = new EntityId[] { fxToken1.CreateReceipt!.Token, fxToken2.CreateReceipt!.Token };

        await AssertHg.TokenIsAssociatedAsync(fxToken1, fxAccount);
        await AssertHg.TokenIsAssociatedAsync(fxToken2, fxAccount);

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.DissociateTokenAsync(new DissociateTokenParams
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
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.TransactionId.Payer).IsEqualTo(fxAccount.CreateReceipt!.Address);

        await AssertHg.TokenNotAssociatedAsync(fxToken1, fxAccount);
        await AssertHg.TokenNotAssociatedAsync(fxToken2, fxAccount);
    }

    [Test]
    public async Task No_Token_Balance_Record_Exists_When_Dissociated()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);

        var association = await AssertHg.TokenIsAssociatedAsync(fxToken, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.DissociateTokenAsync(new DissociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = [fxToken.CreateReceipt!.Token],
            Signatory = fxAccount.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await AssertHg.TokenNotAssociatedAsync(fxToken, fxAccount);
    }

    [Test]
    public async Task Dissociation_Requires_Signing_By_Target_Account()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);

        var association = await AssertHg.TokenIsAssociatedAsync(fxToken, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.DissociateTokenAsync(fxToken.CreateReceipt!.Token, fxAccount.CreateReceipt!.Address);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Dissociate Token from Account failed with status: InvalidSignature");

        association = await AssertHg.TokenIsAssociatedAsync(fxToken, fxAccount);
        await Assert.That(association.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.Revoked);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();
    }

    [Test]
    public async Task Dissociation_Requires_Token_Account()
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
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.DissociateTokenAsync(fxToken.CreateReceipt!.Token, null!);
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("account");
        await Assert.That(ane.Message).StartsWith("Account Address/Alias is missing. Please check that it is not null.");

        ex = await Assert.That(async () =>
        {
            await client.DissociateTokenAsync(fxToken.CreateReceipt!.Token, EntityId.None);
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
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fxAccount.PrivateKey
        });

        var ex = await Assert.That(async () =>
        {
            await client.DissociateTokenAsync(new DissociateTokenParams
            {
                Account = fxAccount.CreateReceipt!.Address,
                Tokens = [fxToken.CreateReceipt!.Token],
                Signatory = fxAccount.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AccountDeleted);
        await Assert.That(tex.Message).StartsWith("Dissociate Token from Account failed with status: AccountDelete");
    }

    [Test]
    public async Task Dissociating_With_Duplicate_Account_Raises_Error()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(null, fxAccount);
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            var tokens = new EntityId[] { fxToken.CreateReceipt!.Token, fxToken.CreateReceipt!.Token };
            await client.DissociateTokenAsync(new DissociateTokenParams
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
    public async Task Dissociate_With_Dissociated_Token_Raises_Error()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxToken1 = await TestToken.CreateAsync(null, fxAccount);
        await using var fxToken2 = await TestToken.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            var tokens = new EntityId[] { fxToken1.CreateReceipt!.Token, fxToken2.CreateReceipt!.Token };
            await client.DissociateTokenAsync(new DissociateTokenParams
            {
                Account = fxAccount.CreateReceipt!.Address,
                Tokens = tokens,
                Signatory = fxAccount.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenNotAssociatedToAccount);
        await Assert.That(tex.Message).StartsWith("Dissociate Token from Account failed with status: TokenNotAssociatedToAccount");
    }

    [Test]
    public async Task Can_Dissociate_Token_From_Contract()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null);
        var xferAmount = 2 * fxToken.CreateParams.Circulation / 3;
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxContract.ContractReceipt!.Contract,
            Tokens = [fxToken.CreateReceipt!.Token],
            Signatory = fxContract.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await fxContract.GetTokenBalancesAsync();
        await Assert.That(info).IsNotNull();

        var association = info.FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token);
        await Assert.That(association).IsNotNull();
        await Assert.That(association!.Token).IsEqualTo(fxToken.CreateReceipt!.Token);
        await Assert.That(association.Balance).IsEqualTo(0);
        await Assert.That(association.KycStatus).IsEqualTo(TokenKycStatus.NotApplicable);
        await Assert.That(association.FreezeStatus).IsEqualTo(TokenTradableStatus.Tradable);
        await Assert.That(association.AutoAssociated).IsFalse();

        receipt = await client.DissociateTokenAsync(new DissociateTokenParams
        {
            Account = fxContract.ContractReceipt!.Contract,
            Tokens = [fxToken.CreateReceipt!.Token],
            Signatory = fxContract.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        info = await fxContract.GetTokenBalancesAsync();
        await Assert.That(info).IsNotNull();
        await Assert.That(info.FirstOrDefault(t => t.Token == fxToken.CreateReceipt!.Token)).IsNull();

        var ex = await Assert.That(async () =>
        {
            await client.TransferTokensAsync(fxToken.CreateReceipt!.Token, fxToken.TreasuryAccount.CreateReceipt!.Address, fxContract.ContractReceipt!.Contract, (long)xferAmount, ctx =>
            {
                ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TokenNotAssociatedToAccount);
        await Assert.That(tex.Message).StartsWith("Token Transfer failed with status: TokenNotAssociatedToAccount");

        await Assert.That(await fxContract.GetTokenBalancesAsync()).IsEmpty();
    }

    [Test]
    public async Task Can_Delete_Account_Having_Token_Balance()
    {
        await using var fxAccount1 = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxAccount2 = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 120_00_000_000);
        await using var fxToken = await TestToken.CreateAsync(fx => fx.CreateParams.GrantKycEndorsement = null, fxAccount1, fxAccount2);
        var xferAmount = (long)fxToken.CreateParams.Circulation;
        await using var client = await TestNetwork.CreateClientAsync();

        await AssertHg.TokenBalanceAsync(fxToken, fxAccount1, 0);
        await AssertHg.TokenBalanceAsync(fxToken, fxAccount2, 0);
        await AssertHg.TokenBalanceAsync(fxToken, fxToken.TreasuryAccount, fxToken.CreateParams.Circulation);

        var receipt = await client.TransferTokensAsync(fxToken, fxToken.TreasuryAccount, fxAccount1, xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxToken.TreasuryAccount.PrivateKey);
        });

        await AssertHg.TokenBalanceAsync(fxToken, fxAccount1, fxToken.CreateParams.Circulation);
        await AssertHg.TokenBalanceAsync(fxToken, fxAccount2, 0);
        await AssertHg.TokenBalanceAsync(fxToken, fxToken.TreasuryAccount, 0);

        // Can't delete the account because it has tokens associated with it.
        var ex = await Assert.That(async () =>
        {
            await client.DeleteAccountAsync(new DeleteAccountParams
            {
                Account = fxAccount1.CreateReceipt!.Address,
                FundsReceiver = fxAccount2.CreateReceipt!.Address,
                Signatory = fxAccount1.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.TransactionRequiresZeroTokenBalances);
        await Assert.That(tex.Message).StartsWith("Delete Account failed with status: TransactionRequiresZeroTokenBalances");

        await AssertHg.TokenBalanceAsync(fxToken, fxAccount1, fxToken.CreateParams.Circulation);
        await AssertHg.TokenBalanceAsync(fxToken, fxAccount2, 0);
        await AssertHg.TokenBalanceAsync(fxToken, fxToken.TreasuryAccount, 0);

        await client.TransferTokensAsync(fxToken, fxAccount1, fxAccount2, xferAmount, ctx =>
        {
            ctx.Signatory = new Signatory(ctx.Signatory!, fxAccount1.PrivateKey);
        });
        receipt = await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = fxAccount1.CreateReceipt!.Address,
            FundsReceiver = fxAccount2.CreateReceipt!.Address,
            Signatory = fxAccount1.PrivateKey
        });

        await AssertHg.TokenBalanceAsync(fxToken, fxAccount2, fxToken.CreateParams.Circulation);
        await AssertHg.TokenBalanceAsync(fxToken, fxToken.TreasuryAccount, 0);
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Dissociate_Token()
    {
        await using var fxToken = await TestToken.CreateAsync();
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        // First, associate the token with the account
        await client.AssociateTokensAsync(new AssociateTokenParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            Tokens = new[] { fxToken.CreateReceipt!.Token },
            Signatory = fxAccount.PrivateKey,
        });

        // Now schedule the dissociation without the account's key
        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new DissociateTokenParams
                {
                    Account = fxAccount.CreateReceipt!.Address,
                    Tokens = new[] { fxToken.CreateReceipt!.Token },
                },
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<TransactionException>();
        await Assert.That(((TransactionException)tex!).Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    }
}
