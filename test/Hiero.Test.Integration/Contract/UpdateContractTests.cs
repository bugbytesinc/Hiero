using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Numerics;

namespace Hiero.Test.Integration.Contract;

public class UpdateContractTests
{
    [Test]
    public async Task Can_Update_Multiple_Properties_In_One_Call()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        var newEndorsement = new Endorsement(newPublicKey);
        var updatedSignatory = new Signatory(TestNetwork.PrivateKey, newPrivateKey);
        var newMemo = Generator.Memo(50);
        client.Configure(ctx => ctx.Signatory = updatedSignatory);
        var receipt = await client.UpdateContractAsync(new UpdateContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            Administrator = newEndorsement,
            Memo = newMemo,
            Signatory = fx.PrivateKey
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        var info = await client.GetContractInfoAsync(fx.ContractReceipt!.Contract);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Contract).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Account).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Administrator).IsEqualTo(newEndorsement);
        await Assert.That(info.RenewPeriod).IsEqualTo(fx.ContractParams.RenewPeriod);
        await Assert.That(info.Memo).IsEqualTo(newMemo);
        await Assert.That(info.Balance).IsEqualTo((ulong)fx.ContractParams.InitialBalance);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Update_Multiple_Properties_In_One_Call_But_Not_Renewal_Period()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var originalInfo = await client.GetContractInfoAsync(fx.ContractReceipt!.Contract);
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        var newExpiration = Generator.TruncatedFutureDate(2400, 4800);
        var newEndorsement = new Endorsement(newPublicKey);
        var updatedSignatory = new Signatory(TestNetwork.PrivateKey, newPrivateKey);
        var newRenewPeriod = TimeSpan.FromDays(Generator.Integer(32, 90));
        var newMemo = Generator.Memo(50);
        client.Configure(ctx => ctx.Signatory = updatedSignatory);
        var ex = await Assert.That(async () =>
        {
            var receipt = await client.UpdateContractAsync(new UpdateContractParams
            {
                Contract = fx.ContractReceipt!.Contract,
                Expiration = newExpiration,
                Administrator = newEndorsement,
                RenewPeriod = newRenewPeriod,
                Memo = newMemo,
                Signatory = new Signatory(fx.PrivateKey, newPrivateKey),
            });
            await client.GetTransactionRecordAsync(receipt.TransactionId);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidExpirationTime);
        await Assert.That(tex.Message).StartsWith("Contract Update failed with status: InvalidExpirationTime");

        var info = await client.GetContractInfoAsync(fx.ContractReceipt!.Contract);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Contract).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Account).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Expiration == originalInfo.Expiration).IsTrue();
        await Assert.That(info.Administrator).IsEqualTo(originalInfo.Administrator);
        await Assert.That(info.RenewPeriod).IsEqualTo(originalInfo.RenewPeriod);
        await Assert.That(info.Memo).IsEqualTo(originalInfo.Memo);
        await Assert.That(info.Balance).IsEqualTo((ulong)fx.ContractParams.InitialBalance);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Update_Contract_Expiration_Date()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var oldExpiration = (await client.GetContractInfoAsync(fxContract)).Expiration;
        var newExpiration = new ConsensusTimeStamp(oldExpiration.Seconds + 60 * 60);
        var receipt = await client.UpdateContractAsync(new UpdateContractParams
        {
            Contract = fxContract,
            Expiration = newExpiration,
            Signatory = fxContract
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        var info = await client.GetContractInfoAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Contract).IsEqualTo(fxContract.ContractReceipt!.Contract);
        await Assert.That(info.Account).IsEqualTo(fxContract.ContractReceipt!.Contract);
        await Assert.That(info.Expiration).IsEqualTo(newExpiration);
        await Assert.That(info.Administrator).IsEqualTo(fxContract.PublicKey);
        await Assert.That(info.RenewPeriod).IsEqualTo(fxContract.ContractParams.RenewPeriod);
        await Assert.That(info.Memo).IsEqualTo(fxContract.ContractParams.Memo);
        await Assert.That(info.Balance).IsEqualTo((ulong)fxContract.ContractParams.InitialBalance);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Update_Admin_Key()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var (newPublicKey, newPrivateKey) = Generator.KeyPair();
        var newEndorsement = new Endorsement(newPublicKey);
        var updatedSignatory = new Signatory(TestNetwork.PrivateKey, fx.PrivateKey, newPrivateKey);
        client.Configure(ctx => ctx.Signatory = updatedSignatory);
        var receipt = await client.UpdateContractAsync(new UpdateContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            Administrator = newEndorsement,
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        var info = await client.GetContractInfoAsync(fx.ContractReceipt!.Contract);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Contract).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Account).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Administrator).IsEqualTo(newEndorsement);
        await Assert.That(info.RenewPeriod).IsEqualTo(fx.ContractParams.RenewPeriod);
        await Assert.That(info.Memo).IsEqualTo(fx.ContractParams.Memo);
        await Assert.That(info.Balance).IsEqualTo((ulong)fx.ContractParams.InitialBalance);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Update_Renew_Period()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newRenewal = TimeSpan.FromDays(Generator.Integer(180, 365));
        var ex = await Assert.That(async () =>
        {
            var receipt = await client.UpdateContractAsync(new UpdateContractParams
            {
                Contract = fx.ContractReceipt!.Contract,
                RenewPeriod = newRenewal,
                Signatory = fx.PrivateKey
            });
            await client.GetTransactionRecordAsync(receipt.TransactionId);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AutorenewDurationNotInRange);
        await Assert.That(tex.Message).StartsWith("Contract Update failed with status: AutorenewDurationNotInRange");

        var info = await client.GetContractInfoAsync(fx.ContractReceipt!.Contract);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Contract).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Account).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Administrator).IsEqualTo(fx.ContractParams.Administrator);
        await Assert.That(info.RenewPeriod).IsEqualTo(fx.ContractParams.RenewPeriod);
        await Assert.That(info.Memo).IsEqualTo(fx.ContractParams.Memo);
        await Assert.That(info.Balance).IsEqualTo((ulong)fx.ContractParams.InitialBalance);
        await Assert.That(info.Ledger != BigInteger.Zero).IsTrue();
    }

    [Test]
    public async Task Can_Update_Memo()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newMemo = Generator.Memo(50);
        var receipt = await client.UpdateContractAsync(new UpdateContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            Memo = newMemo,
            Signatory = fx.PrivateKey
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        var info = await client.GetContractInfoAsync(fx.ContractReceipt!.Contract);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Contract).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Account).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Administrator).IsEqualTo(fx.ContractParams.Administrator);
        await Assert.That(info.RenewPeriod).IsEqualTo(fx.ContractParams.RenewPeriod);
        await Assert.That(info.Memo).IsEqualTo(newMemo);
        await Assert.That(info.Balance).IsEqualTo((ulong)fx.ContractParams.InitialBalance);
    }

    [Test]
    public async Task Can_Update_Association_Limit()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newLimit = Generator.Integer(fx.ContractParams.AutoAssociationLimit + 1, fx.ContractParams.AutoAssociationLimit + 100);
        var receipt = await client.UpdateContractAsync(new UpdateContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            AutoAssociationLimit = newLimit,
            Signatory = fx.PrivateKey
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetContractInfoAsync(fx.ContractReceipt!.Contract);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Contract).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Account).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Administrator).IsEqualTo(fx.ContractParams.Administrator);
        await Assert.That(info.RenewPeriod).IsEqualTo(fx.ContractParams.RenewPeriod);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(newLimit);
        await Assert.That(info.Balance).IsEqualTo((ulong)fx.ContractParams.InitialBalance);
    }

    [Test]
    public async Task Can_Update_To_Airdrop_Association()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.UpdateContractAsync(new UpdateContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            AutoAssociationLimit = -1,
            Signatory = fx.PrivateKey
        });
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetContractInfoAsync(fx.ContractReceipt!.Contract);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Contract).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Account).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Administrator).IsEqualTo(fx.ContractParams.Administrator);
        await Assert.That(info.RenewPeriod).IsEqualTo(fx.ContractParams.RenewPeriod);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(-1);
        await Assert.That(info.Balance).IsEqualTo((ulong)fx.ContractParams.InitialBalance);
    }

    [Test]
    public async Task Can_Update_Memo_No_Record()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newMemo = Generator.Memo(50);
        var receipt = await client.UpdateContractAsync(new UpdateContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            Memo = newMemo,
            Signatory = fx.PrivateKey
        });
        var info = await client.GetContractInfoAsync(fx.ContractReceipt!.Contract);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Contract).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Account).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(info.Administrator).IsEqualTo(fx.ContractParams.Administrator);
        await Assert.That(info.RenewPeriod).IsEqualTo(fx.ContractParams.RenewPeriod);
        await Assert.That(info.Memo).IsEqualTo(newMemo);
        await Assert.That(info.Balance).IsEqualTo((ulong)fx.ContractParams.InitialBalance);
    }

    [Test]
    public async Task Updating_Immutable_Contract_Raises_Error()
    {
        await using var fxContract = await GreetingContract.CreateAsync(fx =>
        {
            fx.ContractParams.Administrator = null;
        });
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            var receipt = await client.UpdateContractAsync(new UpdateContractParams
            {
                Contract = fxContract.ContractReceipt!.Contract,
                Memo = Generator.Memo(50)
            });
            await client.GetTransactionRecordAsync(receipt.TransactionId);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ModifyingImmutableContract);
        await Assert.That(tex.Message).StartsWith("Contract Update failed with status: ModifyingImmutableContract");
    }

    [Test]
    public async Task Updating_Contract_Without_Admin_Key_Raises_Error()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        // Note Client does not have admin key in context
        var ex = await Assert.That(async () =>
        {
            await using var client = await TestNetwork.CreateClientAsync();
            await client.UpdateContractAsync(new UpdateContractParams
            {
                Contract = fxContract,
                Memo = Generator.Memo(50)
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Contract Update failed with status: InvalidSignature");

        // Try again with the admin key to prove we have the keys right
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.UpdateContractAsync(new UpdateContractParams
        {
            Signatory = fxContract,
            Contract = fxContract,
            Memo = Generator.Memo(50)
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
        var record = await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Update_With_Missing_Contract_Raises_Error()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newMemo = Generator.Memo(50);
        var ex = await Assert.That(async () =>
        {
            var receipt = await client.UpdateContractAsync(new UpdateContractParams
            {
                Memo = newMemo
            });
            await client.GetTransactionRecordAsync(receipt.TransactionId);
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("Contract");
        await Assert.That(ane.Message).StartsWith("Contract address is missing. Please check that it is not null.");
    }

    [Test]
    public async Task Update_With_No_Changes_Raises_Error()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var ex = await Assert.That(async () =>
        {
            var receipt = await client.UpdateContractAsync(new UpdateContractParams
            {
                Contract = fx.ContractReceipt!.Contract
            });
            await client.GetTransactionRecordAsync(receipt.TransactionId);
        }).ThrowsException();
        var ae = ex as ArgumentException;
        await Assert.That(ae).IsNotNull();
        await Assert.That(ae!.ParamName).IsEqualTo("UpdateContractParams");
        await Assert.That(ae.Message).StartsWith("The Contract Updates contains no update properties, it is blank.");
    }

    [Test]
    public async Task Update_With_Non_Existant_Contract_Raises_Error()
    {
        await using var fx = await TestFile.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var invalidContractAddress = fx.CreateReceipt!.File;
        await client.DeleteFileAsync(new DeleteFileParams
        {
            File = invalidContractAddress,
            Signatory = fx.CreateParams.Signatory
        });

        var newMemo = Generator.Memo(50);
        var ex = await Assert.That(async () =>
        {
            var receipt = await client.UpdateContractAsync(new UpdateContractParams
            {
                Contract = invalidContractAddress,
                Memo = newMemo,
                Signatory = fx.PrivateKey
            });
            await client.GetTransactionRecordAsync(receipt.TransactionId);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidContractId);
        await Assert.That(tex.Message).StartsWith("Contract Update failed with status: InvalidContractId");
    }

    [Test]
    public async Task Update_With_Negative_Duration_Raises_Error()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newMemo = Generator.Memo(50);
        var ex = await Assert.That(async () =>
        {
            var receipt = await client.UpdateContractAsync(new UpdateContractParams
            {
                Contract = fx.ContractReceipt!.Contract,
                RenewPeriod = TimeSpan.FromDays(Generator.Integer(-90, -60)),
                Signatory = fx.PrivateKey
            });
            await client.GetTransactionRecordAsync(receipt.TransactionId);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidRenewalPeriod);
        await Assert.That(tex.Message).StartsWith("Contract Update failed with status: InvalidRenewalPeriod");
    }

    [Test]
    public async Task Update_With_Invalid_Duration_Raises_Error()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var newMemo = Generator.Memo(50);
        var ex = await Assert.That(async () =>
        {
            var receipt = await client.UpdateContractAsync(new UpdateContractParams
            {
                Contract = fx.ContractReceipt!.Contract,
                RenewPeriod = TimeSpan.FromMinutes(Generator.Integer(90, 120)),
                Signatory = fx.PrivateKey
            });
            await client.GetTransactionRecordAsync(receipt.TransactionId);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.AutorenewDurationNotInRange);
        await Assert.That(tex.Message).StartsWith("Contract Update failed with status: AutorenewDurationNotInRange");
    }

    [Test]
    public async Task Can_Make_Mutable_Contract_Imutable()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var memo = Generator.Memo(50);

        await client.UpdateContractAsync(new UpdateContractParams
        {
            Contract = fxContract.ContractReceipt!.Contract,
            Memo = memo,
            Signatory = fxContract.PrivateKey
        });
        var info = await client.GetContractInfoAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(info.Memo).IsEqualTo(memo);

        await client.UpdateContractAsync(new UpdateContractParams
        {
            Contract = fxContract.ContractReceipt!.Contract,
            Administrator = Endorsement.None,
            Signatory = fxContract.PrivateKey
        });

        var ex = await Assert.That(async () =>
        {
            await client.UpdateContractAsync(new UpdateContractParams
            {
                Contract = fxContract.ContractReceipt!.Contract,
                Memo = Generator.Code(50),
                Signatory = fxContract.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ModifyingImmutableContract);
        await Assert.That(tex.Message).StartsWith("Contract Update failed with status: ModifyingImmutableContract");

        info = await client.GetContractInfoAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(info).IsNotNull();
        await Assert.That(info.Memo).IsEqualTo(memo);
        // Immutable Contracts list their "contract" key as the administrator Key.
        await Assert.That(info.Administrator!.Type).IsEqualTo(KeyType.Contract);
        await Assert.That(info.Administrator.Contract).IsEqualTo(fxContract.ContractReceipt!.Contract);
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Update_Contract()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var client = await TestNetwork.CreateClientAsync();
        var newMemo = Generator.Memo(10, 20);

        var receipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new UpdateContractParams
            {
                Contract = fxContract.ContractReceipt!.Contract,
                Memo = newMemo,
            },
            Payer = fxPayer,
        });
        await Assert.That(receipt.Schedule).IsNotEqualTo(EntityId.None);

        var signReceipt = await client.SignScheduleAsync(new SignScheduleParams
        {
            Schedule = receipt.Schedule,
            Signatory = new Signatory(fxContract.PrivateKey, fxPayer.PrivateKey),
        });
        await Assert.That(signReceipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Schedule_Update()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var client = await TestNetwork.CreateClientAsync();
        var newMemo = Generator.Memo(50);
        var scheduledReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new UpdateContractParams
            {
                Contract = fxContract.ContractReceipt!.Contract,
                Memo = newMemo,
                Signatory = fxContract.PrivateKey,
            },
            Payer = fxPayer
        });
        await Assert.That(scheduledReceipt.Status).IsEqualTo(ResponseCode.Success);

        var infoBefore = await client.GetContractInfoAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(infoBefore.Memo).IsEqualTo(fxContract.ContractParams.Memo);

        var executionReceipt = await client.SignScheduleAsync(scheduledReceipt.Schedule, ctx =>
        {
            ctx.Payer = fxPayer;
            ctx.Signatory = fxPayer;
        });
        var pendingReceipt = await client.GetReceiptAsync(scheduledReceipt.ScheduledTransactionId);
        await Assert.That(pendingReceipt.Status).IsEqualTo(ResponseCode.Success);

        var infoAfter = await client.GetContractInfoAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(infoAfter.Memo).IsEqualTo(newMemo);
    }

    [Test]
    public async Task Can_Update_Staking_Node()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var nodeId = (await client.GetAddressBookAsync()).Select(n => n.Id).Max();
        var receipt = await client.UpdateContractAsync(new UpdateContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            StakedNode = nodeId,
            Signatory = fx.PrivateKey
        });
        var info = await client.GetContractInfoAsync(fx.ContractReceipt!.Contract);
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo.Declined).IsFalse();
        await Assert.That(info.StakingInfo.Node).IsEqualTo(nodeId);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Update_Proxied_Address()
    {
        await using var fxProxied = await TestAccount.CreateAsync();
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.UpdateContractAsync(new UpdateContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            ProxyAccount = fxProxied.CreateReceipt!.Address,
            Signatory = fx.PrivateKey
        });
        var info = await client.GetContractInfoAsync(fx.ContractReceipt!.Contract);
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo.Declined).IsFalse();
        await Assert.That(info.StakingInfo.Node).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(fxProxied.CreateReceipt!.Address);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Decline_Staking_Reward()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.UpdateContractAsync(new UpdateContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            DeclineStakeReward = true,
            Signatory = fx.PrivateKey
        });
        var info = await client.GetContractInfoAsync(fx.ContractReceipt!.Contract);
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo.Declined).IsTrue();
        await Assert.That(info.StakingInfo.Node).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
    }
}
