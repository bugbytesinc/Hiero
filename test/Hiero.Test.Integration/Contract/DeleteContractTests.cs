using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using Hiero.Extensions;

namespace Hiero.Test.Integration.Contract;

public class DeleteContractTests
{
    [Test]
    public async Task Can_Delete_Contract()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.DeleteContractAsync(new DeleteContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fx.PrivateKey
        }, ctx => ctx.Signatory = new Signatory(TestNetwork.PrivateKey, fx.PrivateKey));
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Can_Delete_Contract_Using_Signatory()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.DeleteContractAsync(new DeleteContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fx.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);
    }

    [Test]
    public async Task Deleting_Contract_Does_Not_Immediately_Remove_Contract_Info()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.DeleteContractAsync(new DeleteContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fx.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetContractInfoAsync(fx.ContractReceipt.Contract);
        await Assert.That(info.Deleted).IsTrue();
    }

    [Test]
    public async Task Delete_Twice_Raises_An_Error()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.DeleteContractAsync(new DeleteContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fx.PrivateKey
        });
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var ex = await Assert.That(async () =>
        {
            await client.DeleteContractAsync(new DeleteContractParams
            {
                Contract = fx.ContractReceipt.Contract,
                FundsReceiver = TestNetwork.Payer,
                Signatory = fx.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ContractDeleted);
        await Assert.That(tex.Message).StartsWith("Delete Contract failed with status: ContractDeleted");
    }

    [Test]
    public async Task Delete_Contract_Without_Admin_Key_Raises_Error()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.DeleteContractAsync(new DeleteContractParams
            {
                Contract = fxContract.ContractReceipt!.Contract,
                FundsReceiver = TestNetwork.Payer
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
        await Assert.That(tex.Message).StartsWith("Delete Contract failed with status: InvalidSignature");
    }

    [Test]
    public async Task Delete_Contract_With_Invalid_Contract_ID_Raises_Error()
    {
        await using var fx = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.DeleteContractAsync(new DeleteContractParams
            {
                Contract = fx.CreateReceipt!.Address,
                FundsReceiver = TestNetwork.Payer
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.InvalidContractId);
        await Assert.That(tex.Message).StartsWith("Delete Contract failed with status: InvalidContractId");
    }

    [Test]
    public async Task Delete_Contract_With_Missing_ID_Raises_Error()
    {
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.DeleteContractAsync(new DeleteContractParams
            {
                Contract = null!,
                FundsReceiver = TestNetwork.Payer
            });
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("Contract");
        await Assert.That(ane.Message).StartsWith("Contract to Delete is missing. Please check that it is not null.");
    }

    [Test]
    public async Task Delete_Contract_With_Missing_Return_To_Address_Raises_Error()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.DeleteContractAsync(new DeleteContractParams
            {
                Contract = fx.ContractReceipt!.Contract,
                FundsReceiver = null!
            });
        }).ThrowsException();
        var ane = ex as ArgumentNullException;
        await Assert.That(ane).IsNotNull();
        await Assert.That(ane!.ParamName).IsEqualTo("FundsReceiver");
        await Assert.That(ane.Message).StartsWith("Transfer address is missing. Please check that it is not null.");
    }

    [Test]
    public async Task Delete_Contract_With_Invalid_Address_Raises_Error()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var fx2 = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var deleteAccountReceipt = await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = fx2.CreateReceipt!.Address,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fx2.PrivateKey
        });
        await Assert.That(deleteAccountReceipt.Status).IsEqualTo(ResponseCode.Success);

        var ex = await Assert.That(async () =>
        {
            await client.DeleteContractAsync(new DeleteContractParams
            {
                Contract = fx.ContractReceipt!.Contract,
                FundsReceiver = fx2.CreateReceipt.Address,
                Signatory = fx.PrivateKey
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ObtainerDoesNotExist);
        await Assert.That(tex.Message).StartsWith("Delete Contract failed with status: ObtainerDoesNotExist");
    }

    [Test]
    public async Task Return_Remaining_Contract_Balance_Upon_Delete()
    {
        // Setup the Simple Event Emitting Contract and An account for "send to".
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var fxContract = await EventEmittingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        // Get the Info for the Address to receive funds before any changes happen.
        var infoBefore = await client.GetAccountInfoAsync(fxAccount.CreateReceipt!.Address);
        await Assert.That(infoBefore.Balance).IsEqualTo(fxAccount.CreateParams.InitialBalance);

        // Double check the balance on the contract, confirm it has hbars
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var callParams = new CallContractParams
        {
            Contract = fxContract.ContractReceipt!.Contract,
            MethodName = "get_balance"
        };
        callParams.Gas = await mirror.EstimateGasAsync(TestNetwork.Payer.CastToEvmAddress(), callParams) + 1000;
        var callReceipt = await client.CallContractAsync(callParams);
        var contractBalanceBefore = (CallContractRecord)await client.GetTransactionRecordAsync(callReceipt.TransactionId);
        await Assert.That(contractBalanceBefore).IsNotNull();
        await Assert.That(fxContract.ContractParams.InitialBalance).IsBetween(1L, (long)int.MaxValue);
        await Assert.That(contractBalanceBefore.Result!.Result.As<long>()).IsEqualTo(fxContract.ContractParams.InitialBalance);

        // Delete the Contract, returning contract balance to Address
        var deleteContractReceipt = await client.DeleteContractAsync(new DeleteContractParams
        {
            Contract = fxContract.ContractReceipt.Contract,
            FundsReceiver = fxAccount.CreateReceipt.Address,
            Signatory = fxContract.PrivateKey
        });
        await Assert.That(deleteContractReceipt.Status).IsEqualTo(ResponseCode.Success);

        // Check the balance of account to see if it went up by contract's balance.
        var infoAfter = await client.GetAccountInfoAsync(fxAccount.CreateReceipt.Address);
        await Assert.That(infoAfter.Balance).IsEqualTo(infoBefore.Balance + (ulong)fxContract.ContractParams.InitialBalance);
    }

    [Test]
    public async Task Delete_Imutable_Contract_Raises_Error()
    {
        await using var fxContract = await GreetingContract.CreateAsync(fx =>
        {
            fx.ContractParams.Administrator = null;
        });
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.DeleteContractAsync(new DeleteContractParams
            {
                Contract = fxContract.ContractReceipt!.Contract,
                FundsReceiver = TestNetwork.Payer
            });
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ModifyingImmutableContract);
        await Assert.That(tex.Message).StartsWith("Delete Contract failed with status: ModifyingImmutableContract");

        var info = await client.GetContractInfoAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(info).IsNotNull();
    }

    [Test]
    public async Task Can_Schedule_And_Sign_Delete_Contract()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new DeleteContractParams
            {
                Contract = fxContract.ContractReceipt!.Contract,
                FundsReceiver = TestNetwork.Payer,
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
    public async Task Can_Schedule_Delete()
    {
        await using var fxContract = await GreetingContract.CreateAsync();
        await using var fxPayer = await TestAccount.CreateAsync(fx => fx.CreateParams.InitialBalance = 20_00_000_000);
        await using var client = await TestNetwork.CreateClientAsync();

        var scheduledReceipt = await client.ScheduleAsync(new ScheduleParams
        {
            Transaction = new DeleteContractParams
            {
                Contract = fxContract.ContractReceipt!.Contract,
                FundsReceiver = TestNetwork.Payer,
                Signatory = fxContract.PrivateKey,
            },
            Payer = fxPayer,
        });
        await Assert.That(scheduledReceipt.Status).IsEqualTo(ResponseCode.Success);

        var infoBefore = await client.GetContractInfoAsync(fxContract.ContractReceipt.Contract);
        await Assert.That(infoBefore.Deleted).IsFalse();

        var executionReceipt = await client.SignScheduleAsync(scheduledReceipt.Schedule, ctx =>
        {
            ctx.Payer = fxPayer;
            ctx.Signatory = fxPayer;
        });
        var pendingReceipt = await client.GetReceiptAsync(scheduledReceipt.ScheduledTxId);
        await Assert.That(pendingReceipt.Status).IsEqualTo(ResponseCode.Success);

        var infoAfter = await client.GetContractInfoAsync(fxContract.ContractReceipt.Contract);
        await Assert.That(infoAfter.Deleted).IsTrue();
    }
}
