using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Contract;

public class PayableContractTests
{
    [Test]
    public async Task Can_Get_Contract_Balance_From_Call()
    {
        await using var fx = await PayableContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.CallContractAsync(new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "get_balance",
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        }, ctx => ctx.Memo = "");
        var record = (CallContractRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Memo).IsEqualTo("");
        await Assert.That(record.Fee > 0UL).IsTrue();
        await Assert.That(record.Result!.Contract).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(record.Result.Error.Data.IsEmpty).IsTrue();
        await Assert.That(record.Result.Bloom.IsEmpty).IsTrue();
        await Assert.That(record.Result.GasUsed <= 50_000UL).IsTrue();
        // NETWORK DEFECT: NOT IMPLEMENTED
        await Assert.That(record.Result.GasLimit).IsEqualTo(0);
        await Assert.That(record.Result.PayableAmount).IsEqualTo(0);
        await Assert.That(record.Result.MessageSender).IsEqualTo(EntityId.None);
        await Assert.That(record.Result.Events).IsEmpty();
        /**
         * HEDERA CHURN: THE FOLLOWING WILL BE ADDED BACK IF/WHEN HAPI SUPPORTS IT.
         *
         *  Assert.Empty(record.Result.StateChanges);
         */
        await Assert.That(record.Result.EvmAddress).IsEqualTo(EvmAddress.None);
        await Assert.That(record.Result.Result.As<long>()).IsEqualTo(fx.ContractParams.InitialBalance);
        await Assert.That(record.Result.Input.Size).IsEqualTo(0);
        /// UM, is this correct?
        await Assert.That(record.Result.Nonces).IsEmpty();

        // Ensure matches API version.
        var apiBalance = await client.GetContractBalanceAsync(fx.ContractReceipt!.Contract);
        await Assert.That(apiBalance).IsEqualTo((ulong)fx.ContractParams.InitialBalance);
    }

    [Test]
    public async Task Can_Get_Contract_Balance_From_Local_Call()
    {
        await using var fx = await PayableContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var result = await client.QueryContractAsync(new QueryContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "get_balance",
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3),
            ReturnedDataGasAllowance = 5000
        });
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Error.Data.IsEmpty).IsTrue();
        await Assert.That(result.Bloom.IsEmpty).IsFalse();
        await Assert.That(result.GasUsed <= (ulong)fx.ContractParams.Gas).IsTrue();
        // NETWORK DEFECT: NOT IMPLEMENTED
        await Assert.That(result.GasLimit).IsEqualTo(0);
        await Assert.That(result.PayableAmount).IsEqualTo(0);
        await Assert.That(result.MessageSender).IsEqualTo(EntityId.None);
        await Assert.That(result.Events).IsEmpty();
        await Assert.That(result.Result.As<long>()).IsEqualTo(fx.ContractParams.InitialBalance);
        await Assert.That(result.Input.Size).IsEqualTo(0);
        await Assert.That(result.Nonces).IsEmpty();
        /**
         * HEDERA CHURN: THE FOLLOWING WILL BE ADDED BACK IF/WHEN HAPI SUPPORTS IT.
         *
         *  Assert.Empty(result.StateChanges);
         */
        await Assert.That(result.EvmAddress).IsEqualTo(EvmAddress.None);

        // Ensure matches API version.
        var apiBalance = await client.GetContractBalanceAsync(fx.ContractReceipt!.Contract);
        await Assert.That(apiBalance).IsEqualTo((ulong)fx.ContractParams.InitialBalance);

        // Ensure matches Info version
        var info = await client.GetContractInfoAsync(fx.ContractReceipt!.Contract);
        await Assert.That(info.Balance).IsEqualTo((ulong)fx.ContractParams.InitialBalance);
    }

    [Test]
    public async Task Can_Call_Contract_Method_Sending_Funds()
    {
        await using var fx = await PayableContract.CreateAsync();
        await using var fx2 = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var infoBefore = await client.GetAccountInfoAsync(fx2.CreateReceipt!.Address);
        var callParameters = new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "send_to",
            MethodArgs = [fx2.CreateReceipt!.Address],
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        };
        var receipt = await client.CallContractAsync(callParameters);
        var record = (CallContractRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee > 0UL).IsTrue();
        await Assert.That(record.Result!.Error.Data.IsEmpty).IsTrue();
        await Assert.That(record.Result.Bloom.IsEmpty).IsTrue();
        await Assert.That(record.Result.GasUsed <= (ulong)callParameters.Gas).IsTrue();
        // NETWORK DEFECT: NOT IMPLEMENTED
        await Assert.That(record.Result.GasLimit).IsEqualTo(0);
        await Assert.That(record.Result.Events).IsEmpty();
        /**
         * HEDERA CHURN: THE FOLLOWING WILL BE ADDED BACK IF/WHEN HAPI SUPPORTS IT.
         *
         *  Assert.Empty(record.Result.StateChanges);
         */
        await Assert.That(record.Result.EvmAddress).IsEqualTo(EvmAddress.None);

        var infoAfter = await client.GetAccountInfoAsync(fx2.CreateReceipt!.Address);
        await Assert.That(infoAfter.Balance - infoBefore.Balance).IsEqualTo((ulong)fx.ContractParams.InitialBalance);
    }

    [Test]
    public async Task Can_Send_Funds_To_Payable_Contract_With_External_Payable()
    {
        await using var fx = await PayableContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var extraFunds = Generator.Integer(500, 1000);
        var receipt = await client.CallContractAsync(new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            PayableAmount = extraFunds,
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        });
        var record = (CallContractRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        await using var fx2 = await TestAccount.CreateAsync();
        var infoBefore = await client.GetAccountInfoAsync(fx2.CreateReceipt!.Address);
        receipt = await client.CallContractAsync(new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "send_to",
            MethodArgs = new[] { fx2.CreateReceipt!.Address },
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        }, ctx => ctx.Memo = "");
        record = (CallContractRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Memo).IsEqualTo("");
        await Assert.That(record.Fee > 0UL).IsTrue();
        await Assert.That(record.Result!.Error.Data.IsEmpty).IsTrue();
        await Assert.That(record.Result.Bloom.IsEmpty).IsTrue();
        await Assert.That(record.Result.GasUsed <= 50_000UL).IsTrue();
        // NETWORK DEFECT: NOT IMPLEMENTED
        await Assert.That(record.Result.GasLimit).IsEqualTo(0);
        await Assert.That(record.Result.PayableAmount).IsEqualTo(0);
        await Assert.That(record.Result.MessageSender).IsEqualTo(EntityId.None);
        await Assert.That(record.Result.Events).IsEmpty();
        /**
         * HEDERA CHURN: THE FOLLOWING WILL BE ADDED BACK IF/WHEN HAPI SUPPORTS IT.
         *
         *  Assert.Empty(record.Result.StateChanges);
         */
        await Assert.That(record.Result.EvmAddress).IsEqualTo(EvmAddress.None);

        var infoAfter = await client.GetAccountInfoAsync(fx2.CreateReceipt!.Address);
        await Assert.That(infoAfter.Balance - infoBefore.Balance).IsEqualTo((ulong)(fx.ContractParams.InitialBalance + extraFunds));
    }

    [Test]
    public async Task Send_Funds_To_Deleted_Account_Raises_Error()
    {
        // Setup the Simple Payable Contract and An account for "send to".
        await using var fxContract = await PayableContract.CreateAsync();
        await using var fxAccount = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        // Get the Info for the account state and then delete the account.
        var infoBefore = await client.GetAccountInfoAsync(fxAccount.CreateReceipt!.Address);
        var deleteReceipt = await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = fxAccount.CreateReceipt!.Address,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fxAccount.PrivateKey
        });
        await Assert.That(deleteReceipt.Status).IsEqualTo(ResponseCode.Success);

        // Double check the balance on the contract, confirm it has hbars
        var contractBalanceBeforeReceipt = await client.CallContractAsync(new CallContractParams
        {
            Contract = fxContract.ContractReceipt!.Contract,
            MethodName = "get_balance",
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        });
        var contractBalanceBefore = (CallContractRecord)await client.GetTransactionRecordAsync(contractBalanceBeforeReceipt.TransactionId);
        await Assert.That(contractBalanceBefore).IsNotNull();
        await Assert.That(fxContract.ContractParams.InitialBalance >= 1 && fxContract.ContractParams.InitialBalance <= int.MaxValue).IsTrue();
        await Assert.That(contractBalanceBefore.Result!.Result.As<long>()).IsEqualTo(fxContract.ContractParams.InitialBalance);

        // Ensure matches API version.
        var apiBalance = await client.GetContractBalanceAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(apiBalance).IsEqualTo((ulong)fxContract.ContractParams.InitialBalance);

        // Ensure matches Info version
        var info = await client.GetContractInfoAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(info.Balance).IsEqualTo((ulong)fxContract.ContractParams.InitialBalance);

        // Call the contract, sending to the address of the now deleted account
        var ex = await Assert.That(async () =>
        {
            var callReceipt = await client.CallContractAsync(new CallContractParams
            {
                Contract = fxContract.ContractReceipt!.Contract,
                Gas = await TestNetwork.EstimateGasFromCentsAsync(7),
                MethodName = "send_to",
                MethodArgs = [fxAccount.CreateReceipt!.Address]
            });
            await client.GetTransactionRecordAsync(callReceipt.TransactionId);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ContractRevertExecuted);
        await Assert.That(tex.Message).StartsWith("Contract Call failed with status: ContractRevertExecuted");

        // Confirm that the balance on the contract remained unchanged.
        var contractBalanceAfterReceipt = await client.CallContractAsync(new CallContractParams
        {
            Contract = fxContract.ContractReceipt!.Contract,
            MethodName = "get_balance",
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        });
        var contractBalanceAfter = (CallContractRecord)await client.GetTransactionRecordAsync(contractBalanceAfterReceipt.TransactionId);
        await Assert.That(contractBalanceAfter).IsNotNull();
        await Assert.That(contractBalanceAfter.Result!.Result.As<long>()).IsEqualTo(fxContract.ContractParams.InitialBalance);

        // Ensure matches API version.
        apiBalance = await client.GetContractBalanceAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(apiBalance).IsEqualTo((ulong)fxContract.ContractParams.InitialBalance);

        // Ensure matches Info version
        info = await client.GetContractInfoAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(info.Balance).IsEqualTo((ulong)fxContract.ContractParams.InitialBalance);
    }

    [Test]
    public async Task Send_Funds_To_Invalid_Account_Raises_Error()
    {
        await using var fx = await PayableContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            var callReceipt = await client.CallContractAsync(new CallContractParams
            {
                Contract = fx.ContractReceipt!.Contract,
                MethodName = "send_to",
                MethodArgs = [new EntityId(0, 0, long.MaxValue)],
                Gas = await TestNetwork.EstimateGasFromCentsAsync(7)
            });
            await client.GetTransactionRecordAsync(callReceipt.TransactionId);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ContractRevertExecuted);
        await Assert.That(tex.Message).StartsWith("Contract Call failed with status: ContractRevertExecuted");
    }

    [Test]
    public async Task Attempts_To_Misplace_Hbars_Thru_Payable_Contract_Should_Fail()
    {
        // Setup the Simple Payable Contract and An account for "send to".
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxContract = await PayableContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        // Get the Info for the account state and then delete the account.
        var info1Before = await client.GetAccountInfoAsync(fxAccount1.CreateReceipt!.Address);
        var info2Before = await client.GetAccountInfoAsync(fxAccount2.CreateReceipt!.Address);
        var delete1Receipt = await client.DeleteAccountAsync(new DeleteAccountParams
        {
            Account = fxAccount1.CreateReceipt!.Address,
            FundsReceiver = TestNetwork.Payer,
            Signatory = fxAccount1.PrivateKey
        });
        await Assert.That(delete1Receipt.Status).IsEqualTo(ResponseCode.Success);

        // Double check the balance on the contract, confirm it has hbars
        var contractBalanceBeforeReceipt = await client.CallContractAsync(new CallContractParams
        {
            Contract = fxContract.ContractReceipt!.Contract,
            MethodName = "get_balance",
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        });
        var contractBalanceBefore = (CallContractRecord)await client.GetTransactionRecordAsync(contractBalanceBeforeReceipt.TransactionId);
        await Assert.That(contractBalanceBefore).IsNotNull();
        await Assert.That(fxContract.ContractParams.InitialBalance >= 1 && fxContract.ContractParams.InitialBalance <= int.MaxValue).IsTrue();
        await Assert.That(contractBalanceBefore.Result!.Result.As<long>()).IsEqualTo(fxContract.ContractParams.InitialBalance);

        // Ensure matches API version.
        var apiBalance = await client.GetContractBalanceAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(apiBalance).IsEqualTo((ulong)fxContract.ContractParams.InitialBalance);

        // Call the contract, sending to the address of the now deleted account
        var ex = await Assert.That(async () =>
        {
            var callReceipt = await client.CallContractAsync(new CallContractParams
            {
                Contract = fxContract.ContractReceipt!.Contract,
                MethodName = "send_to",
                MethodArgs = [fxAccount1.CreateReceipt!.Address],
                Gas = await TestNetwork.EstimateGasFromCentsAsync(5)
            });
            await client.GetTransactionRecordAsync(callReceipt.TransactionId);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ContractRevertExecuted);
        await Assert.That(tex.Message).StartsWith("Contract Call failed with status: ContractRevertExecuted");

        // Confirm that the balance on the contract did not change.
        var contractBalanceAfterReceipt = await client.CallContractAsync(new CallContractParams
        {
            Contract = fxContract.ContractReceipt!.Contract,
            MethodName = "get_balance",
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        });
        var contractBalanceAfter = (CallContractRecord)await client.GetTransactionRecordAsync(contractBalanceAfterReceipt.TransactionId);
        await Assert.That(contractBalanceAfter).IsNotNull();
        await Assert.That(contractBalanceAfter.Result!.Result.As<long>()).IsEqualTo(fxContract.ContractParams.InitialBalance);

        // Ensure matches API version.
        apiBalance = await client.GetContractBalanceAsync(fxContract.ContractReceipt!.Contract);
        await Assert.That(apiBalance).IsEqualTo((ulong)fxContract.ContractParams.InitialBalance);

        // Try to get info on the deleted account, but this will fail because the
        // account is already deleted.
        ex = await Assert.That(async () =>
        {
            // So if this throws an error, why did the above call not fail?
            await client.GetAccountInfoAsync(fxAccount1.CreateReceipt!.Address);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();

        // Delete the Contract, returning any hidden hbars to account number 2
        var deleteContractRecord = await client.DeleteContractAsync(new DeleteContractParams
        {
            Contract = fxContract.ContractReceipt!.Contract,
            FundsReceiver = fxAccount2.CreateReceipt!.Address,
            Signatory = fxContract.PrivateKey
        });
        await Assert.That(deleteContractRecord.Status).IsEqualTo(ResponseCode.Success);

        // Check the balance of account number 2, the hBars should be there?
        var info2After = await client.GetAccountInfoAsync(fxAccount2.CreateReceipt!.Address);
        await Assert.That(info2After.Balance).IsEqualTo((ulong)fxContract.ContractParams.InitialBalance + info2Before.Balance);
    }

    [Test]
    public async Task Send_Funds_To_Payable_Contract_With_External_Payable_Raises_Contract_Balance()
    {
        await using var fx = await PayableContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        ulong initialBalance = (ulong)fx.ContractParams.InitialBalance;
        var apiBalanceBefore = await client.GetContractBalanceAsync(fx.ContractReceipt!.Contract);
        var infoBalanceBefore = (await client.GetContractInfoAsync(fx.ContractReceipt!.Contract)).Balance;
        var callBalanceBeforeReceipt = await client.CallContractAsync(new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "get_balance",
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        });
        var callBalanceBeforeRecord = (CallContractRecord)await client.GetTransactionRecordAsync(callBalanceBeforeReceipt.TransactionId);
        var callBalanceBefore = (ulong)callBalanceBeforeRecord.Result!.Result.As<long>();
        await Assert.That(apiBalanceBefore).IsEqualTo(initialBalance);
        await Assert.That(infoBalanceBefore).IsEqualTo(initialBalance);
        await Assert.That(callBalanceBefore).IsEqualTo(initialBalance);

        var extraFunds = Generator.Integer(500, 1000);
        var receipt = await client.CallContractAsync(new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            PayableAmount = extraFunds,
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        });
        var record = (CallContractRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);

        ulong finalBalance = (ulong)fx.ContractParams.InitialBalance + (ulong)extraFunds;
        var apiBalanceAfter = await client.GetContractBalanceAsync(fx.ContractReceipt!.Contract);
        var infoBalanceAfter = (await client.GetContractInfoAsync(fx.ContractReceipt!.Contract)).Balance;
        var callBalanceAfterReceipt = await client.CallContractAsync(new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "get_balance",
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        });
        var callBalanceAfterRecord = (CallContractRecord)await client.GetTransactionRecordAsync(callBalanceAfterReceipt.TransactionId);
        var callBalanceAfter = (ulong)callBalanceAfterRecord.Result!.Result.As<long>();
        await Assert.That(apiBalanceAfter).IsEqualTo(finalBalance);
        await Assert.That(infoBalanceAfter).IsEqualTo(finalBalance);
        await Assert.That(callBalanceAfter).IsEqualTo(finalBalance);
    }

    [Test]
    public async Task Transfer_Funds_To_Payable_Contract_With_External_Payable_Raises_Contract_Balance()
    {
        await using var fx = await PayableContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        ulong initialBalance = (ulong)fx.ContractParams.InitialBalance;
        var apiBalanceBefore = await client.GetContractBalanceAsync(fx.ContractReceipt!.Contract);
        var infoBalanceBefore = (await client.GetContractInfoAsync(fx.ContractReceipt!.Contract)).Balance;
        var callBalanceBeforeReceipt = await client.CallContractAsync(new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "get_balance",
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        });
        var callBalanceBeforeRecord = (CallContractRecord)await client.GetTransactionRecordAsync(callBalanceBeforeReceipt.TransactionId);
        var callBalanceBefore = (ulong)callBalanceBeforeRecord.Result!.Result.As<long>();
        await Assert.That(apiBalanceBefore).IsEqualTo(initialBalance);
        await Assert.That(infoBalanceBefore).IsEqualTo(initialBalance);
        await Assert.That(callBalanceBefore).IsEqualTo(initialBalance);

        var extraFunds = Generator.Integer(500, 1000);
        var transferReceipt = await client.TransferAsync(TestNetwork.Payer, fx.ContractReceipt!.Contract, extraFunds);
        await Assert.That(transferReceipt.Status).IsEqualTo(ResponseCode.Success);

        ulong finalBalance = (ulong)fx.ContractParams.InitialBalance + (ulong)extraFunds;
        var apiBalanceAfter = await client.GetContractBalanceAsync(fx.ContractReceipt!.Contract);
        var infoBalanceAfter = (await client.GetContractInfoAsync(fx.ContractReceipt!.Contract)).Balance;
        var callBalanceAfterReceipt = await client.CallContractAsync(new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "get_balance",
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        });
        var callBalanceAfterRecord = (CallContractRecord)await client.GetTransactionRecordAsync(callBalanceAfterReceipt.TransactionId);
        var callBalanceAfter = (ulong)callBalanceAfterRecord.Result!.Result.As<long>();
        await Assert.That(apiBalanceAfter).IsEqualTo(finalBalance);
        await Assert.That(infoBalanceAfter).IsEqualTo(finalBalance);
        await Assert.That(callBalanceAfter).IsEqualTo(finalBalance);
    }
}
