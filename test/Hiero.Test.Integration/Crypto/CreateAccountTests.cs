using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.Crypto;

public class CreateAccountTests
{
    [Test]
    public async Task Can_Create_Account_Async()
    {
        var initialBalance = (ulong)Generator.Integer(10, 200);
        var (publicKey, privateKey) = Generator.KeyPair();
        var client = await TestNetwork.CreateClientAsync();
        var createResult = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = initialBalance,
            Endorsement = publicKey
        });
        await Assert.That(createResult).IsNotNull();
        await Assert.That(createResult.Address).IsNotNull();
        await Assert.That(createResult.Address.AccountNum > 0).IsTrue();

        var info = await client.GetAccountInfoAsync(createResult.Address);
        await Assert.That(info.Balance).IsEqualTo(initialBalance);
        await Assert.That(info.Address.RealmNum).IsEqualTo(createResult.Address.RealmNum);
        await Assert.That(info.Address.ShardNum).IsEqualTo(createResult.Address.ShardNum);
        await Assert.That(info.Address.AccountNum).IsEqualTo(createResult.Address.AccountNum);
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.EvmNonce).IsEqualTo(0);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(0);
        await Assert.That(info.KeyAlias).IsEqualTo(Endorsement.None);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo.Declined).IsFalse();
        await Assert.That(info.StakingInfo.PeriodStart).IsEqualTo(ConsensusTimeStamp.MinValue);
        await Assert.That(info.StakingInfo.PendingReward).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(info.StakingInfo.Node).IsEqualTo(0);

        // Move remaining funds back to primary account.
        var from = createResult.Address;
        await client.TransferAsync(from, TestNetwork.Payer, (long)initialBalance, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey));

        var receipt = await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = createResult.Address,
            FundsReceiver = TestNetwork.Payer,
            Signatory = privateKey
        });
        await Assert.That(receipt).IsNotNull();
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var ex = await Assert.That(async () =>
        {
            await client.GetAccountInfoAsync(createResult.Address);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Message).StartsWith("Transaction Failed Pre-Check: AccountDeleted");
    }

    [Test]
    public async Task Can_Set_Signature_Required_True()
    {
        var (publicKey, privateKey) = Generator.KeyPair();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = 1,
            Endorsement = publicKey,
            Signatory = privateKey,
            RequireReceiveSignature = true
        });
        var record = (CreateAccountRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetAccountInfoAsync(record.Address);
        await Assert.That(info.ReceiveSignatureRequired).IsTrue();
    }

    [Test]
    public async Task Can_Set_Signature_Required_False()
    {
        var (publicKey, privateKey) = Generator.KeyPair();
        await using var client = await TestNetwork.CreateClientAsync();
        var createResult = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = 1,
            Endorsement = publicKey,
            RequireReceiveSignature = false
        });
        await Assert.That(createResult.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetAccountInfoAsync(createResult.Address);
        await Assert.That(info.ReceiveSignatureRequired).IsFalse();
    }

    [Test]
    public async Task Empty_Endorsement_Is_Not_Allowed()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            await client.CreateAccountAsync(new CreateAccountParams
            {
                InitialBalance = 10,
                Endorsement = Endorsement.None
            });
        }).ThrowsException();
        var aoe = ex as ArgumentOutOfRangeException;
        await Assert.That(aoe).IsNotNull();
        await Assert.That(aoe!.ParamName).IsEqualTo("Endorsement");
        await Assert.That(aoe.Message).StartsWith("The Endorsement for the account is missing, it is required.");
    }

    [Test]
    public async Task Can_Set_Memo()
    {
        var (publicKey, privateKey) = Generator.KeyPair();
        var memo = Generator.Memo(20);
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = 1,
            Endorsement = publicKey,
            Signatory = privateKey,
            Memo = memo
        });
        var record = (CreateAccountRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetAccountInfoAsync(record.Address);
        await Assert.That(info.Memo).IsEqualTo(memo);
    }

    [Test]
    public async Task Can_Set_Max_Token_Association()
    {
        var (publicKey, privateKey) = Generator.KeyPair();
        var limit = Generator.Integer(20, 200);
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = 1,
            Endorsement = publicKey,
            Signatory = privateKey,
            AutoAssociationLimit = limit
        });
        var record = (CreateAccountRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetAccountInfoAsync(record.Address);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(limit);
    }

    [Test]
    public async Task Can_Set_Staking_Node()
    {
        var (publicKey, privateKey) = Generator.KeyPair();
        await using var client = await TestNetwork.CreateClientAsync();
        var nodeId = (await client.GetAddressBookAsync()).Select(n => n.Id).Max();
        var receipt = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = 1,
            Endorsement = publicKey,
            Signatory = privateKey,
            StakedNode = nodeId,
        });
        var record = (CreateAccountRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetAccountInfoAsync(record.Address);
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo.Declined).IsFalse();
        await Assert.That(info.StakingInfo.Node).IsEqualTo(nodeId);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Set_Proxy_Address()
    {
        await using var fxProxied = await TestAccount.CreateAsync();
        var (publicKey, privateKey) = Generator.KeyPair();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = 1,
            Endorsement = publicKey,
            Signatory = privateKey,
            ProxyAccount = fxProxied.CreateReceipt!.Address
        });
        var record = (CreateAccountRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetAccountInfoAsync(record.Address);
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo.Declined).IsFalse();
        await Assert.That(info.StakingInfo.Node).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(fxProxied.CreateReceipt!.Address);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Decline_Staking_Reward()
    {
        var (publicKey, privateKey) = Generator.KeyPair();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = 1,
            Endorsement = publicKey,
            Signatory = privateKey,
            DeclineStakeReward = true
        });
        var record = (CreateAccountRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetAccountInfoAsync(record.Address);
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo.Declined).IsTrue();
        await Assert.That(info.StakingInfo.Node).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Schedule_Create_Account()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        var fxTemplate = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var scheduledReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = fxTemplate.CreateParams,
            Payer = fxPayer,
        });

        await using var payerClient = client.Clone(ctx =>
        {
            ctx.Payer = fxPayer;
            ctx.Signatory = fxPayer;
        });
        var transactionReceipt = await payerClient.SignScheduleAsync(scheduledReceipt.Schedule);
        var pendingReceipt = await payerClient.GetReceiptAsync(scheduledReceipt.ScheduledTransactionId);
        await Assert.That(pendingReceipt.Status).IsEqualTo(ResponseCode.Success);

        await Assert.That(pendingReceipt is CreateAccountReceipt).IsTrue();
        var createReceipt = (CreateAccountReceipt)pendingReceipt;
        var account = createReceipt.Address;

        var info = await client.GetAccountInfoAsync(account);
        await Assert.That(info.Address).IsEqualTo(account);
        await Assert.That(info.EvmAddress).IsNotNull();
        await Assert.That(info.Deleted).IsFalse();
        await Assert.That(info.Endorsement).IsEqualTo(fxTemplate.CreateParams.Endorsement);
        await Assert.That(info.Balance).IsEqualTo(fxTemplate.CreateParams.InitialBalance);
        await Assert.That(info.ReceiveSignatureRequired).IsEqualTo(fxTemplate.CreateParams.RequireReceiveSignature);
        await Assert.That(info.AutoRenewPeriod.TotalSeconds).IsEqualTo(fxTemplate.CreateParams.AutoRenewPeriod.TotalSeconds);
        await Assert.That(info.Expiration > ConsensusTimeStamp.MinValue).IsTrue();
    }

    [Test]
    public async Task Can_Create_Account_With_Duplicate_Keys_Async()
    {
        var initialBalance = (ulong)Generator.Integer(10, 200);
        var (publicKey, privateKey) = Generator.KeyPair();
        var list = Enumerable.Range(0, 5).Select(_ => new Endorsement(publicKey)).ToArray();
        var requiredCount = (uint)(list.Length - 1);
        var client = await TestNetwork.CreateClientAsync();
        var createResult = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = initialBalance,
            Endorsement = new Endorsement(requiredCount, list)
        });
        await Assert.That(createResult.Status).IsEqualTo(ResponseCode.Success);
        var createdAddress = createResult.Address;

        var info = await client.GetAccountInfoAsync(createResult.Address);
        await Assert.That(info.Balance).IsEqualTo(initialBalance);
        await Assert.That(info.Endorsement.Type).IsEqualTo(KeyType.List);
        await Assert.That(info.Endorsement.RequiredCount).IsEqualTo(requiredCount);
        await Assert.That(info.Endorsement.List.Length).IsEqualTo(list.Length);
        foreach (var key in info.Endorsement.List)
        {
            await Assert.That(key).IsEqualTo(list[0]);
        }

        var createdBalance = await client.GetAccountBalanceAsync(createdAddress);
        await Assert.That(createdBalance).IsEqualTo(initialBalance);

        await client.TransferAsync(createdAddress, TestNetwork.Payer, (long)initialBalance, ctx => ctx.Signatory = new Signatory(ctx.Signatory!, privateKey));

        var finalBalance = await client.GetAccountBalanceAsync(createdAddress);
        await Assert.That(finalBalance).IsEqualTo(0UL);

        var receipt = await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = createResult.Address,
            FundsReceiver = TestNetwork.Payer,
            Signatory = privateKey
        });
        await Assert.That(receipt).IsNotNull();
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Create_Accounts_With_Same_Key_Async()
    {
        var initialBalance = (ulong)Generator.Integer(10, 200);
        var (publicKey, privateKey) = Generator.Secp256k1KeyPair();
        var client = await TestNetwork.CreateClientAsync();
        var createResult1 = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = initialBalance,
            Endorsement = publicKey
        });
        var createResult2 = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = initialBalance,
            Endorsement = publicKey
        });
        await Assert.That(createResult1.Address).IsNotEqualTo(createResult2.Address);

        var info1 = await client.GetAccountInfoAsync(createResult1.Address);
        var info2 = await client.GetAccountInfoAsync(createResult2.Address);
        await Assert.That(info1.Balance).IsEqualTo(info2.Balance);
        await Assert.That(info1.Deleted).IsFalse();
        await Assert.That(info2.Deleted).IsFalse();
        await Assert.That(info1.EvmNonce).IsEqualTo(0);
        await Assert.That(info2.EvmNonce).IsEqualTo(0);
        await Assert.That(info1.AutoAssociationLimit).IsEqualTo(0);
        await Assert.That(info2.AutoAssociationLimit).IsEqualTo(0);
        await Assert.That(info1.KeyAlias).IsEqualTo(Endorsement.None);
        await Assert.That(info2.KeyAlias).IsEqualTo(Endorsement.None);
        await Assert.That(info1.KeyAlias).IsEqualTo(info2.KeyAlias);
        await Assert.That(info1.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(info2.Ledger != BigInteger.Zero).IsTrue();
        await Assert.That(info1.StakingInfo).IsNotNull();
        await Assert.That(info2.StakingInfo).IsNotNull();
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Create_Account()
    {
        var (publicKey, privateKey) = Generator.KeyPair();
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var client = await TestNetwork.CreateClientAsync();
        var scheduledReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new CreateAccountParams
            {
                Endorsement = publicKey,
                InitialBalance = 1
            },
            Payer = fxPayer,
        });
        await Assert.That(scheduledReceipt.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(scheduledReceipt.Schedule).IsNotEqualTo(EntityId.None);

        var signReceipt = await client.SignScheduleAsync(new SignScheduleParams
        {
            Schedule = scheduledReceipt.Schedule,
            Signatory = fxPayer.PrivateKey
        });
        await Assert.That(signReceipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Account_Can_Self_Destruct()
    {
        var (publicKey, privateKey) = Generator.KeyPair();
        await using var client = await TestNetwork.CreateClientAsync();
        var createResult = await client.CreateAccountAsync(new CreateAccountParams
        {
            InitialBalance = 22_00_000_000,
            Endorsement = publicKey
        });
        await Assert.That(createResult.Status).IsEqualTo(ResponseCode.Success);
        var deleteResult = await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = createResult.Address,
            FundsReceiver = TestNetwork.Payer,
            Signatory = privateKey
        }, ctx =>
        {
            ctx.Payer = createResult.Address;
            ctx.Signatory = privateKey;
        });
        await Assert.That(deleteResult.Status).IsEqualTo(ResponseCode.Success);

        var ex = await Assert.That(async () =>
        {
            await client.GetAccountInfoAsync(createResult.Address);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.AccountDeleted);
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Delete_Account()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var tex = await Assert.That(async () =>
        {
            await client.ScheduleAsync(new ScheduleParams
            {
                Transaction = new DeleteAccountParams
                {
                    Account = fxAccount.CreateReceipt!.Address,
                    FundsReceiver = TestNetwork.Payer,
                },
                Payer = fxPayer,
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<TransactionException>();
        await Assert.That(((TransactionException)tex!).Status).IsEqualTo(ResponseCode.ScheduledTransactionNotInWhitelist);
    }
}
