using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Contract;

public class QueryContractTests
{
    [Test]
    public async Task Can_Create_A_Contract_Async()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var result = await client.QueryContractAsync(new QueryContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            ReturnedDataGasAllowance = 4000,
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3),
            MethodName = "greet"
        });
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Error.Data.IsEmpty).IsTrue();
        await Assert.That(result.Bloom.IsEmpty).IsFalse();
        await Assert.That(result.GasUsed <= ulong.MaxValue).IsTrue();
        // NETWORK DEFECT: NOT IMPLEMENTED
        await Assert.That(result.GasLimit).IsEqualTo(0);
        await Assert.That(result.PayableAmount).IsEqualTo(0);
        await Assert.That(result.MessageSender).IsEqualTo(EntityId.None);
        await Assert.That(result.Events).IsEmpty();
        await Assert.That(result.Result.As<string>()).IsEqualTo("Hello, world!");
        await Assert.That(result.Input.Size).IsEqualTo(0);
        await Assert.That(result.Nonces).IsEmpty();
        /**
         * HEDERA CHURN: THE FOLLOWING WILL BE ADDED BACK IF/WHEN HAPI SUPPORTS IT.
         *
         *  Assert.Empty(result.StateChanges);
         */
        await Assert.That(result.EvmAddress).IsEqualTo(EvmAddress.None);
    }

    [Test]
    public async Task Can_Create_A_Contract_With_State_Async()
    {
        await using var fx = await StatefulContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var result = await client.QueryContractAsync(new QueryContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            ReturnedDataGasAllowance = 6000,
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3),
            MethodName = "get_message"
        });
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Error.Data.IsEmpty).IsTrue();
        await Assert.That(result.Bloom.IsEmpty).IsFalse();
        await Assert.That(result.GasUsed <= ulong.MaxValue).IsTrue();
        // NETWORK DEFECT: NOT IMPLEMENTED
        await Assert.That(result.GasLimit).IsEqualTo(0);
        await Assert.That(result.PayableAmount).IsEqualTo(0);
        await Assert.That(result.MessageSender).IsEqualTo(EntityId.None);
        await Assert.That(result.Events).IsEmpty();
        await Assert.That(result.Result.As<string>()).IsEqualTo(fx.ContractParams.ConstructorArgs[0] as string);
        await Assert.That(result.Input.Size).IsEqualTo(0);
        await Assert.That(result.Nonces).IsEmpty();
        /**
         * HEDERA CHURN: THE FOLLOWING WILL BE ADDED BACK IF/WHEN HAPI SUPPORTS IT.
         *
         *  Assert.Empty(result.StateChanges);
         */
        await Assert.That(result.EvmAddress).IsEqualTo(EvmAddress.None);
    }

    [Test]
    public async Task Query_Contract_With_Insufficient_Funds_Throws_Error_By_Default()
    {
        await using var fx = await StatefulContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.QueryContractAsync(new QueryContractParams
            {
                Contract = fx.ContractReceipt!.Contract,
                Gas = 1,
                ReturnedDataGasAllowance = 4000,
                MethodName = "get_message"
            });
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InsufficientGas);
        await Assert.That(pex.TransactionId).IsNotNull();
        await Assert.That(pex.RequiredFee).IsEqualTo(0ul);
    }

    [Test]
    public async Task Query_Contract_With_Insufficient_Gas_And_Return_Flag_Set_Still_Returns_Precheck_Error()
    {
        await using var fx = await StatefulContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var ex = await Assert.That(async () =>
        {
            await client.QueryContractAsync(new QueryContractParams
            {
                Contract = fx.ContractReceipt!.Contract,
                Gas = 1,
                ReturnedDataGasAllowance = 4000,
                MethodName = "get_message",
                ThrowOnFail = false
            });
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InsufficientGas);
        await Assert.That(pex.TransactionId).IsNotNull();
        await Assert.That(pex.RequiredFee).IsEqualTo(0ul);
    }

    [Test]
    public async Task Can_Create_A_Contract_And_Set_State_Async()
    {
        await using var fx = await StatefulContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var newMessage = Generator.Code(50);
        var ex = await Assert.That(async () =>
        {
            await client.QueryContractAsync(new QueryContractParams
            {
                Contract = fx.ContractReceipt!.Contract,
                MethodName = "set_message",
                MethodArgs = new object[] { newMessage },
                Gas = await TestNetwork.EstimateGasFromCentsAsync(3),
                ReturnedDataGasAllowance = 10000
            });
        }).ThrowsException();
        var qex = ex as ContractException;
        await Assert.That(qex).IsNotNull();
        await Assert.That(qex!.Status).IsEqualTo(ResponseCode.LocalCallModificationException);
        await Assert.That(qex.Message).StartsWith("Contract Query Failed with Code: LocalCallModificationException");
    }

    [Test]
    public async Task Call_Query_Attempting_State_Change_Fails_Without_Error_When_Throw_On_Fail_False()
    {
        await using var fx = await StatefulContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();

        var newMessage = Generator.Code(50);
        var result = await client.QueryContractAsync(new QueryContractParams
        {
            Contract = fx.ContractReceipt!.Contract,
            Gas = await TestNetwork.EstimateGasFromCentsAsync(3),
            MethodName = "set_message",
            MethodArgs = [newMessage],
            ReturnedDataGasAllowance = 4000,
            ThrowOnFail = false
        });
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Error.As<string>()).IsEqualTo("ILLEGAL_STATE_CHANGE");
        await Assert.That(result.Bloom.IsEmpty).IsTrue();
        await Assert.That(result.GasUsed <= ulong.MaxValue).IsTrue();
        // NETWORK DEFECT: NOT IMPLEMENTED
        await Assert.That(result.GasLimit).IsEqualTo(0);
        await Assert.That(result.PayableAmount).IsEqualTo(0);
        await Assert.That(result.MessageSender).IsEqualTo(EntityId.None);
        await Assert.That(result.Events).IsEmpty();
        await Assert.That(result.Result.Size).IsEqualTo(0);
        // NETWORK DEFECT: NOT IMPLEMENTED
        await Assert.That(result.Input.Size).IsEqualTo(0);
        await Assert.That(result.Nonces).IsEmpty();
        //Assert.Equal(newMessage, result.Input.As<string>());
        /**
         * HEDERA CHURN: THE FOLLOWING WILL BE ADDED BACK IF/WHEN HAPI SUPPORTS IT.
         *
         *  Assert.Empty(result.StateChanges);
         */
        await Assert.That(result.EvmAddress).IsEqualTo(EvmAddress.None);
    }

    [Test]
    public async Task Invalid_Network_Call_Still_Raises_Pre_Check_Error_When_Throw_On_Fail_False()
    {
        await using var fx = await GreetingContract.CreateAsync();
        await using var client = await TestNetwork.CreateClientAsync();
        var (_, badPrivateKey) = Generator.KeyPair();

        var ex = await Assert.That(async () =>
        {
            await client.QueryContractAsync(new QueryContractParams
            {
                Contract = fx.ContractReceipt!.Contract,
                ReturnedDataGasAllowance = 4000,
                Gas = await TestNetwork.EstimateGasFromCentsAsync(3),
                MethodName = "greet",
                ThrowOnFail = false
            }, ctx => ctx.Signatory = badPrivateKey);
        }).ThrowsException();
        var pex = ex as PrecheckException;
        await Assert.That(pex).IsNotNull();
        await Assert.That(pex!.Status).IsEqualTo(ResponseCode.InvalidSignature);
    }
}
