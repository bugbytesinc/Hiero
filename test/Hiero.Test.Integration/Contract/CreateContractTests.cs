using Hiero.Implementation;
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Contract;

public class CreateContractTests
{
    [Test]
    public async Task Can_Create_A_Contract_Async()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await Assert.That(fx.ContractReceipt).IsNotNull();
        await Assert.That(fx.ContractReceipt!.Contract).IsNotNull();
        await Assert.That(fx.ContractReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var record = (CreateContractRecord)await client.GetTransactionRecordAsync(fx.ContractReceipt.TransactionId);
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Memo).IsNotNull();
        await Assert.That(record.Fee >= 0UL).IsTrue();
    }

    [Test]
    public async Task Can_Create_A_Contract_Using_Byte_Code()
    {
        await using var fx = await InitCodeContract.CreateAsync();
        await Assert.That(fx.ContractReceipt).IsNotNull();
        await Assert.That(fx.ContractReceipt!.Contract).IsNotNull();
        await Assert.That(fx.ContractReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var record = (CreateContractRecord)await client.GetTransactionRecordAsync(fx.ContractReceipt.TransactionId);
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Memo).IsNotNull();
        await Assert.That(record.Fee >= 0UL).IsTrue();
    }

    [Test]
    public async Task Can_Create_A_Contract_With_Signature_Async()
    {
        await using var fxContract = await GreetingContract.CreateAsync(fx =>
        {
            fx.ContractParams.Administrator = TestNetwork.Endorsement;
            fx.ContractParams.Signatory = TestNetwork.PrivateKey;
        });
        await Assert.That(fxContract.ContractReceipt).IsNotNull();
        await Assert.That(fxContract.ContractReceipt!.Contract).IsNotNull();
        await Assert.That(fxContract.ContractReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var record = (CreateContractRecord)await client.GetTransactionRecordAsync(fxContract.ContractReceipt.TransactionId);
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Memo).IsNotNull();
        await Assert.That(record.Fee >= 0UL).IsTrue();
        await Assert.That(record.ParentTransactionConsensus).IsNull();
    }

    [Test]
    public async Task Create_A_Contract_Without_Signatory_Raises_Error_Async()
    {
        var (publicKey, privateKey) = Generator.KeyPair();
        var ex = await Assert.That(async () =>
        {
            await GreetingContract.CreateAsync(fx =>
            {
                fx.ContractParams.Administrator = publicKey;
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Message).StartsWith("Create Contract failed with status: InvalidSignature");
        await Assert.That(tex.Status).IsEqualTo(ResponseCode.InvalidSignature);
    }

    [Test]
    public async Task Missing_File_Address_Raises_Error()
    {
        var ex = await Assert.That(async () =>
        {
            await GreetingContract.CreateAsync(fx =>
            {
                fx.ContractParams.File = null!;
            });
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.Message).StartsWith("Both the File address and ByteCode properties missing, one must be specified");
        await Assert.That(ane.ParamName).IsEqualTo("File");
    }

    [Test]
    public async Task File_And_Init_Code_Raises_Error()
    {
        var ex = await Assert.That(async () =>
        {
            await GreetingContract.CreateAsync(fx =>
            {
                fx.ContractParams.ByteCode = Hex.ToBytes(InitCodeContract.CONTRACT_BYTECODE);
            });
        }).ThrowsException();
        var ae = ex as ArgumentException;
        await Assert.That(ae).IsNotNull();
        await Assert.That(ae!.Message).StartsWith("Both the File address and ByteCode properties are specified, only one can be set.");
        await Assert.That(ae.ParamName).IsEqualTo("File");
    }

    [Test]
    public async Task Missing_Gas_Raises_Error()
    {
        var ex = await Assert.That(async () =>
        {
            await GreetingContract.CreateAsync(fx =>
            {
                fx.ContractParams.Gas = 0;
            });
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InsufficientGas);
        await Assert.That(pex.Message).StartsWith("Transaction Failed Pre-Check: InsufficientGas");
    }

    [Test]
    public async Task Sending_Crypto_To_Non_Payable_Contract_Raises_Error()
    {
        var ex = await Assert.That(async () =>
        {
            await GreetingContract.CreateAsync(fx =>
            {
                fx.ContractParams.InitialBalance = 10;
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Message).StartsWith("Create Contract failed with status: ContractRevertExecuted");
        await Assert.That(tex.Status).IsEqualTo(ResponseCode.ContractRevertExecuted);
    }

    [Test]
    public async Task Invalid_Renew_Period_Raises_Error()
    {
        var ex = await Assert.That(async () =>
        {
            await GreetingContract.CreateAsync(fx =>
            {
                fx.ContractParams.RenewPeriod = TimeSpan.FromTicks(1);
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidRenewalPeriod);
        await Assert.That(tex.Message).StartsWith("Create Contract failed with status: InvalidRenewalPeriod");
    }

    [Test]
    public async Task Can_Create_Contract_Without_Admin_Key()
    {
        await using var fxContract = await GreetingContract.CreateAsync(fx =>
        {
            fx.ContractParams.Administrator = null;
        });
        await Assert.That(fxContract.ContractReceipt).IsNotNull();
        await Assert.That(fxContract.ContractReceipt!.Contract).IsNotNull();
        await Assert.That(fxContract.ContractReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var record = (CreateContractRecord)await client.GetTransactionRecordAsync(fxContract.ContractReceipt.TransactionId);
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Memo).IsNotNull();
        await Assert.That(record.Fee >= 0UL).IsTrue();
        await Assert.That(record.Contract).IsEqualTo(record.Result!.Contract);
        await Assert.That(record.Result.Error.Data.IsEmpty).IsTrue();
        await Assert.That(record.Result.Bloom.IsEmpty).IsTrue();
        await Assert.That(record.Result.GasUsed <= (ulong)fxContract.ContractParams.Gas).IsTrue();
        await Assert.That(record.Result.GasLimit).IsEqualTo(0);
        await Assert.That(record.Result.PayableAmount).IsEqualTo(0);
        await Assert.That(record.Result.MessageSender).IsEqualTo(EntityId.None);
        await Assert.That(record.Result.Events).IsEmpty();
        await Assert.That(record.Result.EvmAddress).IsEqualTo(new EvmAddress(Abi.EncodeArguments(new[] { record.Contract })[12..]));
        await Assert.That(record.Result.Result.Size != 0).IsTrue();
        await Assert.That(record.Result.Result.Data.IsEmpty).IsFalse();
    }

    [Test]
    public async Task Can_Create_Contract_With_Unneeded_Constructor_Data()
    {
        await using var fxContract = await GreetingContract.CreateAsync(fx =>
        {
            fx.ContractParams.ConstructorArgs = new object[] { "Random Data that Should Be Ignored." };
        });
        await Assert.That(fxContract.ContractReceipt).IsNotNull();
        await Assert.That(fxContract.ContractReceipt!.Contract).IsNotNull();
        await Assert.That(fxContract.ContractReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var record = (CreateContractRecord)await client.GetTransactionRecordAsync(fxContract.ContractReceipt.TransactionId);
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Memo).IsNotNull();
        await Assert.That(record.Fee >= 0UL).IsTrue();
        await Assert.That(record.Contract).IsEqualTo(record.Result!.Contract);
        await Assert.That(record.Result.Error.Data.IsEmpty).IsTrue();
        await Assert.That(record.Result.Bloom.IsEmpty).IsTrue();
        await Assert.That(record.Result.GasUsed <= (ulong)fxContract.ContractParams.Gas).IsTrue();
        await Assert.That(record.Result.GasLimit).IsEqualTo(0);
        await Assert.That(record.Result.PayableAmount).IsEqualTo(0);
        await Assert.That(record.Result.MessageSender).IsEqualTo(EntityId.None);
        await Assert.That(record.Result.Events).IsEmpty();
        await Assert.That(record.Result.EvmAddress).IsEqualTo(new EvmAddress(Abi.EncodeArguments(new[] { record.Contract })[12..]));
        await Assert.That(record.Result.Result.Size != 0).IsTrue();
        await Assert.That(record.Result.Result.Data.IsEmpty).IsFalse();
    }

    [Test]
    public async Task Can_Create_Contract_Without_Returning_Record_Data()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var receipt = await client.CreateContractAsync(fx.ContractParams);
        await Assert.That(receipt).IsNotNull();
        await Assert.That(receipt.Contract).IsNotNull();
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Create_A_Contract_With_Parameters()
    {
        await using var fx = await StatefulContract.CreateAsync();
        await Assert.That(fx.ContractReceipt).IsNotNull();
        await Assert.That(fx.ContractReceipt!.Contract).IsNotNull();
        await Assert.That(fx.ContractReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var record = (CreateContractRecord)await client.GetTransactionRecordAsync(fx.ContractReceipt.TransactionId);
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Memo).IsNotNull();
        await Assert.That(record.Fee >= 0UL).IsTrue();
        await Assert.That(record.Contract).IsEqualTo(record.Result!.Contract);
        await Assert.That(record.Result.Error.Data.IsEmpty).IsTrue();
        await Assert.That(record.Result.Bloom.IsEmpty).IsTrue();
        await Assert.That(record.Result.GasUsed <= (ulong)fx.ContractParams.Gas).IsTrue();
        await Assert.That(record.Result.GasLimit).IsEqualTo(0);
        await Assert.That(record.Result.PayableAmount).IsEqualTo(0);
        await Assert.That(record.Result.MessageSender).IsEqualTo(EntityId.None);
        await Assert.That(record.Result.Events).IsEmpty();
        await Assert.That(record.Result.EvmAddress).IsEqualTo(new EvmAddress(Abi.EncodeArguments(new[] { record.Contract })[12..]));
        await Assert.That(record.Result.Result.Size != 0).IsTrue();
        await Assert.That(record.Result.Result.Data.IsEmpty).IsFalse();
        // NETWORK DEFECT: NOT IMPLEMENTED
        await Assert.That(record.Result.Input.Size).IsEqualTo(0);
        await Assert.That(record.Result.Nonces).IsNotEmpty();
    }

    [Test]
    public async Task Create_Without_Required_Contract_Params_Throws_Error()
    {
        var ex = await Assert.That(async () =>
        {
            await StatefulContract.CreateAsync(fx =>
            {
                fx.ContractParams.ConstructorArgs = null!;
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Message).StartsWith("Create Contract failed with status: ContractRevertExecuted");
        await Assert.That(tex.Status).IsEqualTo(ResponseCode.ContractRevertExecuted);
    }

    [Test]
    public async Task Can_Create_A_Payable_Contract()
    {
        await using var fx = await PayableContract.CreateAsync();
        await Assert.That(fx.ContractReceipt).IsNotNull();
        await Assert.That(fx.ContractReceipt!.Contract).IsNotNull();
        await Assert.That(fx.ContractReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var record = (CreateContractRecord)await client.GetTransactionRecordAsync(fx.ContractReceipt.TransactionId);
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Memo).IsNotNull();
        await Assert.That(record.Fee >= 0UL).IsTrue();
    }

    [Test]
    public async Task Can_Not_Schedule_Create()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        var fxTemplate = await GreetingContract.CreateAsync();

        await using var client = await TestNetwork.CreateClientAsync();
        var schedulingReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = fxTemplate.ContractParams,
            Payer = fxPayer,
        });
        await using var payerClient = client.Clone(ctx =>
        {
            ctx.Payer = fxPayer;
            ctx.Signatory = fxPayer;
        });
        var transactionReceipt = await payerClient.SignScheduleAsync(schedulingReceipt.Schedule, ctx =>
        {
            ctx.Payer = fxPayer;
            ctx.Signatory = fxPayer;
        });
        var pendingReceipt = await payerClient.GetReceiptAsync(schedulingReceipt.ScheduledTxId);
        await Assert.That(pendingReceipt.Status).IsEqualTo(ResponseCode.Success);

        var createReceipt = pendingReceipt as CreateContractReceipt;
        await Assert.That(createReceipt).IsNotNull();
        await Assert.That(createReceipt!.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetContractInfoAsync(createReceipt.Contract);
        await Assert.That(info.Memo).IsEqualTo(fxTemplate.ContractParams.Memo);
    }

    [Test]
    public async Task Can_Create_Token_Transfer_Contract()
    {
        await using var fx = await TransferTokenContract.CreateAsync();
        await Assert.That(fx.ContractReceipt).IsNotNull();
        await Assert.That(fx.ContractReceipt!.Contract).IsNotNull();
        await Assert.That(fx.ContractReceipt.Status).IsEqualTo(ResponseCode.Success);

        await using var client = await TestNetwork.CreateClientAsync();
        var record = (CreateContractRecord)await client.GetTransactionRecordAsync(fx.ContractReceipt.TransactionId);
        await Assert.That(record.Hash.ToArray()).IsNotEmpty();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Memo).IsNotNull();
        await Assert.That(record.Fee >= 0UL).IsTrue();
    }

    [Test]
    public async Task Can_Create_With_Auto_Association()
    {
        var limit = Generator.Integer(20, 400);
        var fxContract = await TransferTokenContract.CreateAsync(fx =>
        {
            fx.ContractParams.AutoAssociationLimit = limit;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetContractInfoAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(limit);
    }

    [Test]
    public async Task Can_Create_With_Airdop_Association()
    {
        var fxContract = await TransferTokenContract.CreateAsync(fx =>
        {
            fx.ContractParams.AutoAssociationLimit = -1;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetContractInfoAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(info.AutoAssociationLimit).IsEqualTo(-1);
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Create_Contract()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        var fxTemplate = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new CreateContractParams
            {
                File = fxTemplate.FileReceipt.File,
                Gas = await TestNetwork.EstimateGasFromCentsAsync(3),
                RenewPeriod = TimeSpan.FromSeconds(7890000),
            },
            Payer = fxPayer,
        });
        await Assert.That(receipt.Schedule).IsNotEqualTo(EntityId.None);

        var signReceipt = await client.SignScheduleAsync(new SignScheduleParams
        {
            Schedule = receipt.Schedule,
            Signatory = fxPayer,
        });
        await Assert.That(signReceipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Create_Token_Transfer_Contract_With_Max_Auto_Associations()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var nodeId = (await client.GetAddressBookAsync()).Select(n => n.Id).Max();
        await using var fxContract = await TransferTokenContract.CreateAsync(fx =>
        {
            fx.ContractParams.StakedNode = nodeId;
        });

        var info = await client.GetContractInfoAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo!.Declined).IsFalse();
        await Assert.That(info.StakingInfo.Node).IsEqualTo(nodeId);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Set_Proxy_Address()
    {
        await using var fxProxied = await TestAccount.CreateAsync();
        await using var fxContract = await TransferTokenContract.CreateAsync(fx =>
        {
            fx.ContractParams.StakingProxy = fxProxied.CreateReceipt!.Address;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetContractInfoAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo!.Declined).IsFalse();
        await Assert.That(info.StakingInfo.Node).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(fxProxied.CreateReceipt!.Address);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Decline_Staking_Reward()
    {
        await using var fxContract = await TransferTokenContract.CreateAsync(fx =>
        {
            fx.ContractParams.DeclineStakeReward = true;
        });

        await using var client = await TestNetwork.CreateClientAsync();
        var info = await client.GetContractInfoAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(info.StakingInfo).IsNotNull();
        await Assert.That(info.StakingInfo!.Declined).IsTrue();
        await Assert.That(info.StakingInfo.Node).IsEqualTo(0);
        await Assert.That(info.StakingInfo.Proxy).IsEqualTo(EntityId.None);
        await Assert.That(info.StakingInfo.Proxied).IsEqualTo(0);
    }
}
