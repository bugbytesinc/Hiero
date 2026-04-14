using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Contract;

public class EventEmittingContractTests
{
    [Test]
    public async Task Can_Get_Contract_Balance_From_Call()
    {
        await using var fx = await EventEmittingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var receipt = await client.CallContractAsync(new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "get_balance",
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        });
        var record = (CallContractRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee > 0UL).IsTrue();
        await Assert.That(record.Result!.Contract).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(record.Result.Error.Data.IsEmpty).IsTrue();
        await Assert.That(record.Result.Bloom.IsEmpty).IsTrue();
        await Assert.That(record.Result.GasUsed <= 40_000UL).IsTrue();
        // NETWORK DEFECT: NOT IMPLEMENTED
        await Assert.That(record.Result.GasLimit).IsEqualTo(0);
        await Assert.That(record.Result.PayableAmount).IsEqualTo(0);
        await Assert.That(record.Result.MessageSender).IsEqualTo(EntityId.None);
        await Assert.That(record.Result.Events).IsEmpty();
        var createRecord = (CreateContractRecord)await client.GetTransactionRecordAsync(fx.ContractReceipt!.TransactionId);
        await Assert.That(createRecord.Result!.Nonces).IsNotEmpty();
        /**
         * This looks like a bug in the hedera EVM implementation?
         */
        await Assert.That(record.Result.Nonces).IsEmpty();
        /**
         * HEDERA CHURN: THE FOLLOWING WILL BE ADDED BACK IF/WHEN HAPI SUPPORTS IT.
         *
         *  Assert.Empty(record.Result.StateChanges);
         */
        await Assert.That(record.Result.EvmAddress).IsEqualTo(EvmAddress.None);
        await Assert.That(record.Result.Result.As<long>()).IsEqualTo(fx.ContractParams.InitialBalance);
        await Assert.That(record.Result.Input.Size).IsEqualTo(0);
    }

    [Test]
    public async Task Can_Get_Contract_Balance_From_Local_Call()
    {
        await using var fx = await EventEmittingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var result = await client.QueryContractAsync(new QueryContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "get_balance",
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3),
            ReturnedDataGasAllowance = 1000
        });

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Error.Data.IsEmpty).IsTrue();
        await Assert.That(result.Bloom.IsEmpty).IsFalse();
        await Assert.That(result.GasUsed <= 40000UL).IsTrue();
        // NETWORK DEFECT: NOT IMPLEMENTED
        await Assert.That(result.GasLimit).IsEqualTo(0);
        await Assert.That(result.PayableAmount).IsEqualTo(0);
        await Assert.That(result.MessageSender).IsEqualTo(EntityId.None);
        await Assert.That(result.Events).IsEmpty();
        /**
         * HEDERA CHURN: THE FOLLOWING WILL BE ADDED BACK IF/WHEN HAPI SUPPORTS IT.
         *
         *  Assert.Empty(result.StateChanges);
         */
        await Assert.That(result.EvmAddress).IsEqualTo(EvmAddress.None);
        await Assert.That(result.Result.As<long>()).IsEqualTo(fx.ContractParams.InitialBalance);
        await Assert.That(result.Input.Size).IsEqualTo(0);
        await Assert.That(result.Nonces).IsEmpty();
    }

    [Test]
    public async Task Can_Call_Contract_Method_Sending_Funds()
    {
        await using var fx = await EventEmittingContract.CreateAsync();
        await using var fx2 = await TestAccount.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var infoBefore = await client.GetAccountInfoAsync(fx2.CreateReceipt!.Address);
        var receipt = await client.CallContractAsync(new CallContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            MethodName = "send_to",
            MethodArgs = [fx2.CreateReceipt!.Address],
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        });
        var record = (CallContractRecord)await client.GetTransactionRecordAsync(receipt.TransactionId);
        await Assert.That(record).IsNotNull();
        await Assert.That(record.Status).IsEqualTo(ResponseCode.Success);
        await Assert.That(record.Hash.IsEmpty).IsFalse();
        await Assert.That(record.Consensus).IsNotNull();
        await Assert.That(record.Memo).IsEmpty();
        await Assert.That(record.Fee > 0UL).IsTrue();
        await Assert.That(record.Result!.Error.Data.IsEmpty).IsTrue();
        await Assert.That(record.Result.Bloom.IsEmpty).IsFalse();
        await Assert.That(record.Result.GasUsed <= 300_000UL).IsTrue();
        // NETWORK DEFECT: NOT IMPLEMENTED
        await Assert.That(record.Result.GasLimit).IsEqualTo(0);
        await Assert.That(record.Result.PayableAmount).IsEqualTo(0);
        await Assert.That(record.Result.MessageSender).IsEqualTo(EntityId.None);
        await Assert.That(record.Result.Events).HasSingleItem();
        /**
         * HEDERA CHURN: THE FOLLOWING WILL BE ADDED BACK IF/WHEN HAPI SUPPORTS IT.
         *
         *  Assert.Empty(record.Result.StateChanges);
         */
        await Assert.That(record.Result.EvmAddress).IsEqualTo(EvmAddress.None);

        // Now check the emitted Event
        var result = record.Result.Events[0];
        await Assert.That(result.Contract).IsEqualTo(fx.ContractReceipt!.Contract);
        await Assert.That(result.Bloom.IsEmpty).IsFalse();
        await Assert.That(result.Topics).HasSingleItem();
        await Assert.That(Hex.FromBytes(result.Topics[0])).IsEqualTo("9277a4302be4a765ae8585e09a9306bd55da10e20e59ed4f611a04ba606fece8");

        /**
         * HEDERA CHURN: THE FOLLOWING WILL BE ADDED BACK IF/WHEN HAPI SUPPORTS IT.
         *
         *  Assert.Empty(record.Result.StateChanges);
         */
        await Assert.That(record.Result.EvmAddress).IsEqualTo(EvmAddress.None);

        var (address, amount) = result.Data.As<EntityId, long>();
        await Assert.That(address).IsEqualTo(fx2.CreateReceipt!.Address);
        await Assert.That(amount).IsEqualTo(fx.ContractParams.InitialBalance);

        // Alternate Way
        var objects = result.Data.GetAll(typeof(EntityId), typeof(long));
        await Assert.That(objects[0]).IsEqualTo(fx2.CreateReceipt!.Address);
        await Assert.That(objects[1]).IsEqualTo(fx.ContractParams.InitialBalance);

        var infoAfter = await client.GetAccountInfoAsync(fx2.CreateReceipt!.Address);
        await Assert.That(infoAfter.Balance - infoBefore.Balance).IsEqualTo((ulong)fx.ContractParams.InitialBalance);
    }

    [Test]
    public async Task Attempt_To_Send_Hbars_To_Deleted_Account_Fails()
    {
        // Setup the Simple Event Emitting Contract and An account for "send to".
        await using var fxAccount1 = await TestAccount.CreateAsync();
        await using var fxAccount2 = await TestAccount.CreateAsync();
        await using var fxContract = await EventEmittingContract.CreateAsync();
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

        // Confirm deleted account by trying to get info on the deleted account,
        // this will throw an exception.
        var ex = await Assert.That(async () =>
        {
            await client.GetAccountInfoAsync(fxAccount1.CreateReceipt!.Address);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.AccountDeleted);

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

        // Call the contract, sending to the address of the now deleted account
        ex = await Assert.That(async () =>
        {
            var callReceipt = await client.CallContractAsync(new CallContractParams
            {
                Contract = fxContract.ContractReceipt!.Contract,
                Gas = await TestNetwork.EstimateGasFromCentsAsync(8),
                MethodName = "send_to",
                MethodArgs = [fxAccount1.CreateReceipt!.Address]
            });
            await client.GetTransactionRecordAsync(callReceipt.TransactionId);
        }).ThrowsException();
        var tex = ex as TransactionException;
        await Assert.That(tex).IsNotNull();
        await Assert.That(tex!.Status).IsEqualTo(ResponseCode.ContractRevertExecuted);
        await Assert.That(tex.Message).StartsWith("Contract Call failed with status: ContractRevertExecuted");

        // Confirm that the balance on the contract has not changed.
        var contractBalanceAfterReceipt = await client.CallContractAsync(new CallContractParams
        {
            Contract = fxContract.ContractReceipt!.Contract,
            MethodName = "get_balance",
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3)
        });
        var contractBalanceAfter = (CallContractRecord)await client.GetTransactionRecordAsync(contractBalanceAfterReceipt.TransactionId);
        await Assert.That(contractBalanceAfter).IsNotNull();
        await Assert.That(contractBalanceAfter.Result!.Result.As<long>()).IsEqualTo(fxContract.ContractParams.InitialBalance);

        // Double Check: try to get info on the deleted account,
        // but this will fail because the account is already deleted.
        ex = await Assert.That(async () =>
        {
            // So if this throws an error, why did the above transfer not fail?
            await client.GetAccountInfoAsync(fxAccount1.CreateReceipt!.Address);
        }).ThrowsException();
        pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.AccountDeleted);

        // Delete the Contract, returning any hidden hbars to account number 2
        var deleteContractRecord = await client.DeleteContractAsync(new DeleteContractParams
        {
            Contract = fxContract.ContractReceipt!.Contract,
            FundsReceiver = fxAccount2.CreateReceipt!.Address,
            Signatory = fxContract.PrivateKey
        });
        await Assert.That(deleteContractRecord.Status).IsEqualTo(ResponseCode.Success);

        // Check the balance of account number 2, the hBars should be there.
        var info2After = await client.GetAccountInfoAsync(fxAccount2.CreateReceipt!.Address);
        await Assert.That(info2After.Balance).IsEqualTo((ulong)fxContract.ContractParams.InitialBalance + info2Before.Balance);
    }
}
