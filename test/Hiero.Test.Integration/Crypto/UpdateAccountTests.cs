using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.Crypto;

public class UpdateAccountTests
{
    [Test]
    public async Task Can_Update_Key()
    {
        var (publicKey, privateKey) = Generator.KeyPair();
        var updatedKeyPair = Generator.KeyPair();
        await using var client = await TestNetwork.CreateClientAsync();
        var createResult = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = 1,
            Endorsement = publicKey
        });
        await Assert.That(createResult.Status).IsEqualTo(ResponseCode.Success);

        var originalInfo = await client.GetAccountInfoAsync(createResult.Address);
        await Assert.That(originalInfo.Endorsement).IsEqualTo(new Endorsement(publicKey));

        var updateResult = await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = createResult.Address,
            Endorsement = new Endorsement(updatedKeyPair.publicKey),
            Signatory = new Signatory(privateKey, updatedKeyPair.privateKey)
        });
        await Assert.That(updateResult.Status).IsEqualTo(ResponseCode.Success);

        var updatedInfo = await client.GetAccountInfoAsync(createResult.Address);
        await Assert.That(updatedInfo.Endorsement).IsEqualTo(new Endorsement(updatedKeyPair.publicKey));
    }

    [Test]
    public async Task Can_Update_Key_With_Record()
    {
        var (originalPublicKey, originalPrivateKey) = Generator.KeyPair();
        var (updatedPublicKey, updatedPrivateKey) = Generator.KeyPair();
        await using var client = await TestNetwork.CreateClientAsync();
        var createReceipt = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = 1,
            Endorsement = originalPublicKey
        });
        var createRecord = (CreateAccountRecord)await client.GetTransactionRecordAsync(createReceipt.TransactionId);
        await Assert.That(createRecord.Status).IsEqualTo(ResponseCode.Success);

        var originalInfo = await client.GetAccountInfoAsync(createRecord.Address);
        await Assert.That(originalInfo.Endorsement).IsEqualTo(new Endorsement(originalPublicKey));

        var receipt = await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = createRecord.Address,
            Endorsement = new Endorsement(updatedPublicKey),
            Signatory = new Signatory(originalPrivateKey, updatedPrivateKey)
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee >= 0UL).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        var updatedInfo = await client.GetAccountInfoAsync(createRecord.Address);
        await Assert.That(updatedInfo.Endorsement).IsEqualTo(new Endorsement(updatedPublicKey));
    }

    [Test]
    public async Task Can_Update_Memo()
    {
        var newMemo = Generator.Memo(20, 40);
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = fxAccount,
            Memo = newMemo,
            Signatory = fxAccount
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee >= 0UL).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        var info = await client.GetAccountInfoAsync(fxAccount);
        await Assert.That(info.Memo).IsEqualTo(newMemo);
    }

    [Test]
    public async Task Can_Update_Auto_Association_Limit()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        var newLimit = Generator.Integer(fxAccount.CreateParams.AutoAssociationLimit + 1, fxAccount.CreateParams.AutoAssociationLimit + 100);
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = fxAccount,
            AutoAssociationLimit = newLimit,
            Signatory = fxAccount
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee >= 0UL).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        var info = await client.GetAccountInfoAsync(fxAccount);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(newLimit);
    }

    // Defect 0.21.0
    [Test]
    public async Task Update_Memo_Using_Alias_Is_Not_Supported_Defect()
    {
        // Updating an account using its alias address has not yet been
        // implemented by the network, although it will accept the transaction.
        var newMemo = Generator.Memo(20, 40);
        await using var fxAccount = await TestAliasAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            var receipt = await client.UpdateAccountAsync(new UpdateAccountParams
            {
                Account = fxAccount.Alias,
                Memo = newMemo,
                Signatory = fxAccount
            });
            await client.GetTransactionRecordAsync(receipt.TransactionId);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Account Update failed with status: InvalidAccountId");

        var info = await client.GetAccountInfoAsync(fxAccount);
        await Assert.That(info.Memo).IsEmpty();
    }

    [Test]
    public async Task Can_Update_Memo_To_Empty()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = fxAccount,
            Memo = string.Empty,
            Signatory = fxAccount
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee >= 0UL).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        var info = await client.GetAccountInfoAsync(fxAccount);
        await Assert.That(info.Memo).IsEmpty();
    }

    [Test]
    public async Task Can_Update_Require_Receive_Signature()
    {
        var (publicKey, privateKey) = Generator.KeyPair();
        var originalValue = Generator.Integer(0, 1) == 1;
        await using var client = await TestNetwork.CreateClientAsync();
        var createResult = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = 1,
            Endorsement = publicKey,
            RequireReceiveSignature = originalValue,
            Signatory = originalValue ? new Signatory(privateKey) : null   // When True, you need to include signature on create
        });
        await Assert.That(createResult.Status).IsEqualTo(ResponseCode.Success);

        var originalInfo = await client.GetAccountInfoAsync(createResult.Address);
        await Assert.That(originalInfo.ReceiveSignatureRequired).IsEqualTo(originalValue);

        var newValue = !originalValue;
        var updateResult = await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = createResult.Address,
            Signatory = privateKey,
            RequireReceiveSignature = newValue
        });
        await Assert.That(updateResult.Status).IsEqualTo(ResponseCode.Success);

        var updatedInfo = await client.GetAccountInfoAsync(createResult.Address);
        await Assert.That(updatedInfo.ReceiveSignatureRequired).IsEqualTo(newValue);
    }

    [Test]
    public async Task Can_Update_Auto_Renew_Period()
    {
        var (publicKey, privateKey) = Generator.KeyPair();
        var originalValue = TimeSpan.FromSeconds(7890000);
        await using var client = await TestNetwork.CreateClientAsync();
        var createResult = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = 1,
            Endorsement = publicKey,
            AutoRenewPeriod = originalValue
        });
        await Assert.That(createResult.Status).IsEqualTo(ResponseCode.Success);

        var originalInfo = await client.GetAccountInfoAsync(createResult.Address);
        await Assert.That(originalInfo.AutoRenewPeriod).IsEqualTo(originalValue);

        var newValue = originalValue.Add(TimeSpan.FromDays(Generator.Integer(10, 20)));

        var ex = await Assert.That(async () =>
        {
            var updateResult = await client.UpdateAccountAsync(new UpdateAccountParams
            {
                Account = createResult.Address,
                Signatory = privateKey,
                AutoRenewPeriod = newValue
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AutorenewDurationNotInRange);
        await Assert.That(tex.Message).StartsWith("Account Update failed with status: AutorenewDurationNotInRange");

        var updatedInfo = await client.GetAccountInfoAsync(createResult.Address);
        await Assert.That(updatedInfo.AutoRenewPeriod).IsEqualTo(originalValue);
    }

    // Defect 0.14.0
    [Test]
    public async Task Update_With_Insufficient_Funds_Returns_Required_Fee_Fails_Defect()
    {
        var (publicKey, privateKey) = Generator.KeyPair();
        await using var client = await TestNetwork.CreateClientAsync();
        var createResult = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = 1,
            Endorsement = publicKey,
            Signatory = privateKey,
            RequireReceiveSignature = true
        });
        await Assert.That(createResult.Status).IsEqualTo(ResponseCode.Success);

        var originalInfo = await client.GetAccountInfoAsync(createResult.Address);
        await Assert.That(originalInfo.ReceiveSignatureRequired).IsTrue();

        var ex1 = await Assert.That(async () =>
        {
            await client.UpdateAccountAsync(new UpdateAccountParams
            {
                Account = createResult.Address,
                Signatory = privateKey,
                RequireReceiveSignature = false,
            }, ctx =>
            {
                ctx.FeeLimit = 1;
            });
        }).ThrowsException();
        var pex = ex1 as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InsufficientTxFee);

        var ex2 = await Assert.That(async () =>
        {
            await client.UpdateAccountAsync(new UpdateAccountParams
            {
                Account = createResult.Address,
                Signatory = privateKey,
                RequireReceiveSignature = false
            }, ctx =>
            {
                ctx.FeeLimit = (long)pex.RequiredFee;
            });
        }).ThrowsException();
        var tex = ex2 as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InsufficientTxFee);
        await Assert.That(tex.Message).StartsWith("Account Update failed with status: InsufficientTxFee");

        var updatedInfo = await client.GetAccountInfoAsync(createResult.Address);
        await Assert.That(updatedInfo.ReceiveSignatureRequired).IsTrue();
    }

    [Test]
    public async Task Empty_Endorsement_Is_Not_Allowed()
    {
        var (originalPublicKey, originalPrivateKey) = Generator.KeyPair();
        await using var client = await TestNetwork.CreateClientAsync();
        var createResult = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = 10,
            Endorsement = originalPublicKey
        });
        await Assert.That(createResult.Status).IsEqualTo(ResponseCode.Success);

        var originalInfo = await client.GetAccountInfoAsync(createResult.Address);
        await Assert.That(originalInfo.Endorsement).IsEqualTo(new Endorsement(originalPublicKey));

        var ex = await Assert.That(async () =>
        {
            await client.UpdateAccountAsync(new UpdateAccountParams
            {
                Account = createResult.Address,
                Endorsement = Endorsement.None,
                Signatory = new Signatory(originalPrivateKey)
            });
        }).ThrowsException();
        var aoe = ex as ArgumentOutOfRangeException;
        await Assert.That(aoe).IsNotNull();
        await Assert.That(aoe!.ParamName).IsEqualTo("Endorsement");
        await Assert.That(aoe.Message).StartsWith("Endorsement can not be 'None', it must contain at least one key requirement.");

        var updatedInfo = await client.GetAccountInfoAsync(createResult.Address);
        await Assert.That(updatedInfo.Endorsement).IsEqualTo(originalInfo.Endorsement);

        var receipt = await client.TransferAsync(createResult.Address, TestNetwork.Payer, 5, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, originalPrivateKey));
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var newBalance = await client.GetAccountBalanceAsync(createResult.Address);
        await Assert.That(newBalance).IsEqualTo(5ul);
    }

    [Test]
    public async Task Nested_List_Endorsements_Is_Allowed()
    {
        var (originalPublicKey, originalPrivateKey) = Generator.KeyPair();
        await using var client = await TestNetwork.CreateClientAsync();
        var createResult = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = 10,
            Endorsement = originalPublicKey
        });
        await Assert.That(createResult.Status).IsEqualTo(ResponseCode.Success);

        var originalInfo = await client.GetAccountInfoAsync(createResult.Address);
        await Assert.That(originalInfo.Endorsement).IsEqualTo(new Endorsement(originalPublicKey));

        var nestedEndorsement = new Endorsement(new Endorsement(new Endorsement(new Endorsement(new Endorsement(new Endorsement(originalPublicKey))))));
        var updateResult = await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = createResult.Address,
            Endorsement = nestedEndorsement,
            Signatory = new Signatory(originalPrivateKey)
        });
        await Assert.That(updateResult.Status).IsEqualTo(ResponseCode.Success);

        var updatedInfo = await client.GetAccountInfoAsync(createResult.Address);
        await Assert.That(updatedInfo.Endorsement).IsEqualTo(nestedEndorsement);

        var receipt = await client.TransferAsync(createResult.Address, TestNetwork.Payer, 5, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, originalPrivateKey));
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var newBalance = await client.GetAccountBalanceAsync(createResult.Address);
        await Assert.That(newBalance).IsEqualTo(5ul);
    }

    [Test]
    public async Task Can_Update_Auto_Associaiton_Limit()
    {
        var newLimit = Generator.Integer(20, 40);
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = fxAccount,
            AutoAssociationLimit = newLimit,
            Signatory = fxAccount
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee >= 0UL).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        var info = await client.GetAccountInfoAsync(fxAccount);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(newLimit);
    }

    [Test]
    public async Task Can_Update_Auto_Association_Limit_To_Zero()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = fxAccount,
            AutoAssociationLimit = 0,
            Signatory = fxAccount
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee >= 0UL).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        var info = await client.GetAccountInfoAsync(fxAccount);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(0);
    }

    [Test]
    public async Task Cant_Update_Auto_Associate_Value_To_Less_Than_Negative_One()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            var receipt = await client.UpdateAccountAsync(new UpdateAccountParams
            {
                Account = fxAccount,
                AutoAssociationLimit = -5,
                Signatory = fxAccount
            });
            await client.GetTransactionRecordAsync(receipt.TransactionId);
        }).ThrowsException();
        var aoe = ex as ArgumentOutOfRangeException;
        await Assert.That(aoe).IsNotNull();
        await Assert.That(aoe!.ParamName).IsEqualTo("AutoAssociationLimit");
        await Assert.That(aoe.Message).StartsWith("The number of auto-associations must be greater than or equal to -1.");

        var info = await client.GetAccountInfoAsync(fxAccount);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(fxAccount.CreateParams.AutoAssociationLimit);
    }

    [Test]
    public async Task Can_Update_Auto_Associate_Value_To_Greather_Than_One_Thousand()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        var limit = Generator.Integer(1001, 5000);
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = fxAccount,
            AutoAssociationLimit = limit,
            Signatory = fxAccount
        }, ctx =>
        {
            ctx.FeeLimit = ctx.FeeLimit * limit / 100;
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);

        var info = await client.GetAccountInfoAsync(fxAccount);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(limit);
    }

    [Test]
    public async Task Can_Update_Auto_Associate_Value_To_Negative_One()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = fxAccount,
            AutoAssociationLimit = -1,
            Signatory = fxAccount
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);

        var info = await client.GetAccountInfoAsync(fxAccount);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(-1);
    }

    [Test]
    public async Task Can_Schedule_Update_Account()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxAccount = await TestAccount.CreateAsync();
        var newValue = !fxAccount.CreateParams.RequireReceiveSignature;

        await using var client = await TestNetwork.CreateClientAsync();
        var scheduledReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new UpdateAccountParams
            {
                Account = fxAccount,
                RequireReceiveSignature = newValue,
                Signatory = fxAccount
            },
            Payer = fxPayer
        });
        await Assert.That(scheduledReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var payerClient = client.Clone(ctx =>
        {
            ctx.Payer = fxPayer;
            ctx.Signatory = fxPayer;
        });
        var executionReceipt = await payerClient.SignScheduleAsync(scheduledReceipt.Schedule);
        var pendingReceipt = await payerClient.GetReceiptAsync(scheduledReceipt.ScheduledTransactionId);
        await Assert.That(pendingReceipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetAccountInfoAsync(fxAccount);
        await Assert.That(info.ReceiveSignatureRequired).IsEqualTo(newValue);
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Update_Account()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxAccount = await TestAccount.CreateAsync();
        var newMemo = Generator.Memo(10, 20);

        await using var client = await TestNetwork.CreateClientAsync();
        var scheduledReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new UpdateAccountParams
            {
                Account = fxAccount,
                Memo = newMemo,
            },
            Payer = fxPayer,
        });
        await Assert.That(scheduledReceipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(scheduledReceipt.Schedule).IsNotEqualTo(EntityId.None);

        var signReceipt = await client.SignScheduleAsync(new SignScheduleParams
        {
            Schedule = scheduledReceipt.Schedule,
            Signatory = new Signatory(fxPayer.PrivateKey, fxAccount.PrivateKey)
        });
        await Assert.That(signReceipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Update_Multiple_Properties_At_Once()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxTemplate = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = fxAccount,
            Signatory = new Signatory(fxAccount, fxTemplate),
            Endorsement = fxTemplate.CreateParams.Endorsement,
            RequireReceiveSignature = fxTemplate.CreateParams.RequireReceiveSignature,
            Memo = fxTemplate.CreateParams.Memo
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.CurrentExchangeRate).IsNotNull();
        await Assert.That(record.NextExchangeRate).IsNotNull();
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee >= 0UL).IsTrue();
        await Assert.That(record.TransactionId.Payer).IsEqualTo(TestNetwork.Payer);

        var info = await client.GetAccountInfoAsync(fxAccount);
        await Assert.That(info.Address).IsEqualTo(fxAccount.CreateReceipt!.Address);
        await Assert.That(info.EvmAddress).IsEqualTo(fxAccount.CreateReceipt!.Address.CastToEvmAddress());
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Endorsement).IsEqualTo(fxTemplate.PublicKey);
        await Assert.That(info.Balance).IsEqualTo(fxAccount.CreateParams.InitialBalance);
        await Assert.That(info.ReceiveSignatureRequired).IsEqualTo(fxTemplate.CreateParams.RequireReceiveSignature);
        await Assert.That(info.AutoRenewPeriod.TotalSeconds > 0).IsTrue();
        // v0.34.0 Churn
        //await Assert.That(info.AutoRenewAccount).IsEqualTo(Payer.None);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
        await Assert.That(info.Memo).IsEqualTo(fxTemplate.CreateParams.Memo);
        await Assert.That(info.NftCount).IsEqualTo(0);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(fxAccount.CreateParams.AutoAssociationLimit);
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo.Declined).IsFalse();
        await Assert.That(info.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(info.StakingInfo.Node).IsEqualTo(0);
        await Assert.That(info.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
    }

    // Defect 0.21.0
    [Test]
    public async Task Update_Key_Of_Alias_Account_Is_Not_Supported_Defect()
    {
        // Updating an account using its alias address has not yet been
        // implemented by the network, although it will accept the transaction.
        await using var fxAccount = await TestAliasAccount.CreateAsync();
        var (publicKey, privateKey) = Generator.KeyPair();
        await using var client = await TestNetwork.CreateClientAsync();

        var originalInfo = await client.GetAccountInfoAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(originalInfo.Endorsement).IsEqualTo(new Endorsement(fxAccount.PublicKey));

        var ex = await Assert.That(async () =>
        {
            await client.UpdateAccountAsync(new UpdateAccountParams
            {
                Account = fxAccount.Alias,
                Endorsement = new Endorsement(publicKey),
                Signatory = new Signatory(fxAccount.PrivateKey, privateKey)
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidAccountId);
        await Assert.That(tex.Message).StartsWith("Account Update failed with status: InvalidAccountId");

        var updatedInfo = await client.GetAccountInfoAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(updatedInfo.Endorsement).IsEqualTo(new Endorsement(fxAccount.PublicKey));
    }

    [Test]
    public async Task Protobuf_Coes_Not_Contain_Alias_Update_Functionality()
    {
        // This is a marker test as a backup to catch when the functionality
        // for updating an Alias re-appears in the protobuf (it was taken out)
        // When it re-appears, we re-implement the feature, basic tests are
        // already in place for when this happens.
        var type = typeof(Proto.CryptoUpdateTransactionBody);
        var definition = type.GetProperty("Alias");
        await Assert.That(definition).IsNull();
    }

    [Test]
    public async Task Can_Update_Staking_Node()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var nodeId = (await client.GetAddressBookAsync()).Select(n => n.Id).Max();
        await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = fxAccount,
            StakedNode = nodeId,
            Signatory = fxAccount
        });

        var info = await client.GetAccountInfoAsync(fxAccount);
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo.Declined).IsFalse();
        await Assert.That(info.StakingInfo.Node).IsEqualTo(nodeId);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Update_Staking_Prxoy_Address()
    {
        await using var fxProxied = await TestAccount.CreateAsync();
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = fxAccount,
            ProxyAccount = fxProxied.CreateReceipt!.Address,
            Signatory = fxAccount
        });

        var info = await client.GetAccountInfoAsync(fxAccount);
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo.Declined).IsFalse();
        await Assert.That(info.StakingInfo.Node).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(fxProxied.CreateReceipt!.Address);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Decline_State_Reward()
    {
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        await client.UpdateAccountAsync(new UpdateAccountParams
        {
            Account = fxAccount,
            DeclineStakeReward = true,
            Signatory = fxAccount
        });

        var info = await client.GetAccountInfoAsync(fxAccount);
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo.Declined).IsTrue();
        await Assert.That(info.StakingInfo.Node).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
    }
}
