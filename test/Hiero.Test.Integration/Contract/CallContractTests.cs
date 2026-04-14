using Hiero.Implementation;
using Hiero.Mirror;
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using Hiero.Extensions;

namespace Hiero.Test.Integration.Contract;

public class CallContractTests
{
    [Test]
    public async Task Can_Create_A_Contract_Async()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var callParams = new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "greet"
        };
        callParams.Gas = await mirror.EstimateGasAsync(TestNetwork.Payer.CastToEvmAddress(), callParams);
        var receipt = await client.CallContractAsync(callParams, ctx => ctx.Memo = "");
        var record = (CallContractRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Memo).IsEqualTo("");
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.Result!.Contract).IsEqualTo(fx.ContractReceipt.Contract);
        await Assert.That(record.Result.Error.Data.IsEmpty).IsTrue();
        await Assert.That(record.Result.Bloom.IsEmpty).IsTrue();
        await Assert.That(record.Result.GasUsed).IsBetween(0UL, 50_000UL);
        await Assert.That(record.Result.Events).IsEmpty();
        await Assert.That(record.Result.EvmAddress).IsEqualTo(EvmAddress.None);
        await Assert.That(record.Result.Result.As<string>()).IsEqualTo("Hello, world!");
    }

    [Test]
    public async Task Can_Create_A_Contract_With_State_Async()
    {
        await using var fx = await StatefulContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var callParams = new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "get_message"
        };
        callParams.Gas = await mirror.EstimateGasAsync(TestNetwork.Payer.CastToEvmAddress(), callParams) + 5700;
        var receipt = await client.CallContractAsync(callParams, ctx => ctx.Memo = "");
        var record = (CallContractRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Memo).IsEqualTo("");
        await Assert.That(record.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(record.Result!.Error.Data.IsEmpty).IsTrue();
        await Assert.That(record.Result.Bloom.IsEmpty).IsTrue();
        await Assert.That(record.Result.GasUsed).IsBetween(0UL, 50_000UL);
        // NETWORK DEFECT: NOT IMPLEMENTED
        await Assert.That(record.Result.GasLimit).IsEqualTo(0L);
        await Assert.That(record.Result.PayableAmount).IsEqualTo(0L);
        await Assert.That(record.Result.MessageSender).IsEqualTo(EntityId.None);
        await Assert.That(record.Result.Events).IsEmpty();
        await Assert.That(record.Result.Result.As<string>()).IsEqualTo(fx.ContractParams.ConstructorArgs![0] as string);
    }

    [Test]
    public async Task Can_Create_A_Contract_With_State_Alternate_Signatory_Async()
    {
        await using var fx = await StatefulContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var callParams = new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "get_message",
            Signatory = TestNetwork.PrivateKey
        };
        callParams.Gas = await mirror.EstimateGasAsync(TestNetwork.Payer.CastToEvmAddress(), callParams) + 5700;
        var receipt = await client.CallContractAsync(callParams, ctx => ctx.Signatory = null);
        await Assert.That(receipt).IsNotNull();
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Create_A_Contract_And_Set_State_Async()
    {
        await using var fx = await StatefulContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var mirror = await TestNetwork.GetMirrorRestClientAsync();

        var newMessage = Generator.Code(50);
        var setCallParams = new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "set_message",
            MethodArgs = [newMessage]
        };
        setCallParams.Gas = await mirror.EstimateGasAsync(TestNetwork.Payer.CastToEvmAddress(), setCallParams) + 17000;
        var setReceipt = await client.CallContractAsync(setCallParams, ctx => ctx.Memo = "");
        var setRecord = (CallContractRecord)await client.GetTransactionRecordAsync(setReceipt.TransactionId);
        await Assert.That(setRecord).IsNotNull();
        await Assert.That(setRecord.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(setRecord.Hash.IsEmpty).IsFalse();
        await Assert.That(setRecord.Consensus).IsNotNull();
        await Assert.That(setRecord.Memo).IsEqualTo("");
        await Assert.That(setRecord.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(setRecord.Result!.Contract).IsEqualTo(fx.ContractReceipt.Contract);
        await Assert.That(setRecord.Result.Error.Data.IsEmpty).IsTrue();
        await Assert.That(setRecord.Result.Bloom.IsEmpty).IsTrue();
        await Assert.That(setRecord.Result.GasUsed).IsBetween(0UL, 90_000UL);
        // NETWORK DEFECT: NOT IMPLEMENTED
        await Assert.That(setRecord.Result.GasLimit).IsEqualTo(0L);
        await Assert.That(setRecord.Result.PayableAmount).IsEqualTo(0L);
        await Assert.That(setRecord.Result.MessageSender).IsEqualTo(EntityId.None);
        await Assert.That(setRecord.Result.Events).IsEmpty();
        await Assert.That(setRecord.Result.EvmAddress).IsEqualTo(EvmAddress.None);

        var getCallParams = new CallContractParams
        {
            Contract = fx.ContractReceipt.Contract,
            MethodName = "get_message"
        };
        getCallParams.Gas = await mirror.EstimateGasAsync(TestNetwork.Payer.CastToEvmAddress(), getCallParams) + 1000;
        var getReceipt = await client.CallContractAsync(getCallParams);
        var getRecord = (CallContractRecord)await client.GetTransactionRecordAsync(getReceipt.TransactionId);
        await Assert.That(getRecord).IsNotNull();
        await Assert.That(getRecord.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(getRecord.Hash.IsEmpty).IsFalse();
        await Assert.That(getRecord.Consensus).IsNotNull();
        await Assert.That(getRecord.Memo).IsEmpty();
        await Assert.That(getRecord.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(getRecord.Result!.Contract).IsEqualTo(fx.ContractReceipt.Contract);
        await Assert.That(getRecord.Result.Error.Data.IsEmpty).IsTrue();
        await Assert.That(getRecord.Result.Bloom.IsEmpty).IsTrue();
        await Assert.That(getRecord.Result.Events).IsEmpty();
        await Assert.That(getRecord.Result.EvmAddress).IsEqualTo(EvmAddress.None);
        await Assert.That(getRecord.Result.Result.As<string>()).IsEqualTo(newMessage);
        await Assert.That(getRecord.Result.GasUsed).IsBetween(0UL, 50_000UL);
        // NETWORK DEFECT: NOT IMPLEMENTED
        await Assert.That(getRecord.Result.GasLimit).IsEqualTo(0L);
        await Assert.That(getRecord.Result.PayableAmount).IsEqualTo(0L);
        await Assert.That(getRecord.Result.MessageSender).IsEqualTo(EntityId.None);
    }

    [Test]
    public async Task Can_Create_A_Contract_And_Set_State_Without_Record_Async()
    {
        await using var fx = await StatefulContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var mirror = await TestNetwork.GetMirrorRestClientAsync();

        var newMessage = Generator.Code(50);
        var setCallParams = new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "set_message",
            MethodArgs = [newMessage]
        };
        setCallParams.Gas = await mirror.EstimateGasAsync(TestNetwork.Payer.CastToEvmAddress(), setCallParams) + 17000;
        var setReceipt = await client.CallContractAsync(setCallParams);
        await Assert.That(setReceipt).IsNotNull();
        await Assert.That(setReceipt.Status).IsEqualTo(ResponseCode.Success);

        var getCallParams = new CallContractParams
        {
            Contract = fx.ContractReceipt.Contract,
            MethodName = "get_message"
        };
        getCallParams.Gas = await mirror.EstimateGasAsync(TestNetwork.Payer.CastToEvmAddress(), getCallParams) + 5700;
        var getReceipt = await client.CallContractAsync(getCallParams, ctx => ctx.Memo = "");
        var getRecord = (CallContractRecord)await client.GetTransactionRecordAsync(getReceipt.TransactionId);
        await Assert.That(getRecord).IsNotNull();
        await Assert.That(getRecord.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(getRecord.Hash.IsEmpty).IsFalse();
        await Assert.That(getRecord.Consensus).IsNotNull();
        await Assert.That(getRecord.Memo).IsEqualTo("");
        await Assert.That(getRecord.Fee).IsBetween(0UL, ulong.MaxValue);
        await Assert.That(getRecord.Result!.Contract).IsEqualTo(fx.ContractReceipt.Contract);
        await Assert.That(getRecord.Result.Error.Data.IsEmpty).IsTrue();
        await Assert.That(getRecord.Result.Bloom.IsEmpty).IsTrue();
        await Assert.That(getRecord.Result.Events).IsEmpty();
        await Assert.That(getRecord.Result.EvmAddress).IsEqualTo(EvmAddress.None);
        await Assert.That(getRecord.Result.Result.As<string>()).IsEqualTo(newMessage);
        await Assert.That(getRecord.Result.GasUsed).IsBetween(0UL, 50_000UL);
        await Assert.That(getRecord.Result.PayableAmount).IsEqualTo(0L);
        await Assert.That(getRecord.Result.MessageSender).IsEqualTo(EntityId.None);
    }

    // Defect: 0.46.0 - There is a regression in the network that allows for calling of a contract after it has been deleted.
    [Test]
    public async Task Calling_Deleted_Contract_Does_Not_Raise_Error_Defect()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var deleteReceipt = await client.DeleteContractAsync(new DeleteContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fx.PrivateKey
        });
        await Assert.That(deleteReceipt.Status).IsEqualTo(ResponseCode.Success);

        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var callParams = new CallContractParams
        {
            Contract = fx.ContractReceipt.Contract,
            MethodName = "greet",
        };
        callParams.Gas = await mirror.EstimateGasAsync(TestNetwork.Payer.CastToEvmAddress(), callParams);
        var receipt = await client.CallContractAsync(callParams);
        var record = (CallContractRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Call_Contract()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxContract = await StatefulContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var newMessage = "Scheduled: " + Generator.Code(20);

        var callParams = new CallContractParams
        {
            Contract = fxContract.ContractReceipt!.Contract,
            MethodName = "set_message",
            MethodArgs = [newMessage],
        };
        callParams.Gas = await mirror.EstimateGasAsync(TestNetwork.Payer.CastToEvmAddress(), callParams);

        var receipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = callParams,
            Payer = TestNetwork.Payer,
        }, ctx =>
        {
            ctx.Payer = fxPayer;
            ctx.Signatory = fxPayer;
        });
        await Assert.That(receipt.Schedule).IsNotEqualTo(EntityId.None);

        var signReceipt = await client.SignScheduleAsync(new SignScheduleParams
        {
            Schedule = receipt.Schedule,
            Signatory = TestNetwork.PrivateKey,
        });
        await Assert.That(signReceipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Schedule_Call_Contract()
    {
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var fxContract = await StatefulContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var newMessage1 = "Updated: " + Generator.Code(20);
        var newMessage2 = "Updated: " + Generator.Code(20);

        var setCallParams1 = new CallContractParams
        {
            Contract = fxContract.ContractReceipt!.Contract,
            MethodName = "set_message",
            MethodArgs = [newMessage1]
        };
        setCallParams1.Gas = await mirror.EstimateGasAsync(TestNetwork.Payer.CastToEvmAddress(), setCallParams1);
        await client.CallContractAsync(setCallParams1);

        var queryBeforeScheduling = await client.QueryContractAsync(new QueryContractParams
        {
            Gas = 1_500_000,
            Contract = fxContract.ContractReceipt.Contract,
            MethodName = "get_message",
        });
        await Assert.That(queryBeforeScheduling.Result.As<string>()).IsEqualTo(newMessage1);

        var getCallParamsBefore = new CallContractParams
        {
            Contract = fxContract.ContractReceipt.Contract,
            MethodName = "get_message",
        };
        getCallParamsBefore.Gas = await mirror.EstimateGasAsync(TestNetwork.Payer.CastToEvmAddress(), getCallParamsBefore);
        var messageBeforeReceipt = await client.CallContractAsync(getCallParamsBefore);
        var messageBeforeScheduling = (CallContractRecord)await client.GetTransactionRecordAsync(messageBeforeReceipt.TransactionId);
        await Assert.That(messageBeforeScheduling.Result!.Result.As<string>()).IsEqualTo(newMessage1);

        // Note: network payer is owner of contract by default, so it is the only one
        // that can call set_message and see a change, so the fxPayer pays for this scheduling
        var scheduleCallParams = new CallContractParams
        {
            Contract = fxContract.ContractReceipt.Contract,
            MethodName = "set_message",
            MethodArgs = [newMessage2],
        };
        scheduleCallParams.Gas = await mirror.EstimateGasAsync(TestNetwork.Payer.CastToEvmAddress(), scheduleCallParams);
        var scheduledReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = scheduleCallParams,
            Payer = TestNetwork.Payer
        }, ctx =>
        {
            ctx.Payer = fxPayer;
            ctx.Signatory = fxPayer;
        });
        await Assert.That(scheduledReceipt.Status).IsEqualTo(ResponseCode.Success);

        var queryAfterScheduling = await client.QueryContractAsync(new QueryContractParams
        {
            Gas = 1_500_000,
            Contract = fxContract.ContractReceipt.Contract,
            MethodName = "get_message",
        });
        await Assert.That(queryAfterScheduling.Result.As<string>()).IsEqualTo(newMessage1);

        var getCallParamsAfterSchedule = new CallContractParams
        {
            Contract = fxContract.ContractReceipt.Contract,
            MethodName = "get_message",
        };
        getCallParamsAfterSchedule.Gas = await mirror.EstimateGasAsync(TestNetwork.Payer.CastToEvmAddress(), getCallParamsAfterSchedule);
        var messageAfterReceipt = await client.CallContractAsync(getCallParamsAfterSchedule);
        var messageAfterScheduling = (CallContractRecord)await client.GetTransactionRecordAsync(messageAfterReceipt.TransactionId);
        await Assert.That(messageAfterScheduling.Result!.Result.As<string>()).IsEqualTo(newMessage1);

        var tex = await Assert.That(async () =>
        {
            await client.SignScheduleAsync(scheduledReceipt.Schedule, ctx =>
            {
                ctx.Payer = fxPayer;
                ctx.Signatory = fxPayer;
            });
        }).ThrowsException();
        await Assert.That(tex).IsTypeOf<TransactionException>();
        await Assert.That(((TransactionException)tex!).Status).IsEqualTo(ResponseCode.NoNewValidSignatures);
    }
}
