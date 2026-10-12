// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Hiero.Mirror;
using Proto;

namespace Hiero.Test.Unit.Contract;

/// <summary>
/// An empty method name calls the contract with no call data, which is what
/// runs a Solidity receive() (or fallback()) function.
/// </summary>
public class CallContractParamsTests
{
    private static readonly EntityId Contract = new(0, 0, 3001);

    [Test]
    [Arguments(null)]
    [Arguments("")]
    public async Task No_Method_Name_Sends_No_Call_Data(string? methodName)
    {
        var body = (ContractCallTransactionBody)((INetworkParams<TransactionReceipt>)new CallContractParams
        {
            Contract = Contract,
            Gas = 30_000,
            PayableAmount = 1_000,
            MethodName = methodName!
        }).CreateNetworkTransaction();
        await Assert.That(body.FunctionParameters.Length).IsEqualTo(0);
        await Assert.That(body.Amount).IsEqualTo(1_000);
    }

    [Test]
    public async Task A_Method_Name_Still_Sends_Its_Selector()
    {
        var body = (ContractCallTransactionBody)((INetworkParams<TransactionReceipt>)new CallContractParams
        {
            Contract = Contract,
            Gas = 30_000,
            MethodName = "set_message",
            MethodArgs = ["hi"]
        }).CreateNetworkTransaction();
        await Assert.That(body.FunctionParameters.Length).IsGreaterThan(4);
    }

    [Test]
    public async Task Arguments_Without_A_Method_Name_Are_Rejected()
    {
        var call = new CallContractParams { Contract = Contract, Gas = 30_000, MethodArgs = ["hi"] };
        var ex = await Assert.ThrowsAsync<ArgumentException>(async () => await Task.Run(() => ((INetworkParams<TransactionReceipt>)call).CreateNetworkTransaction()));
        await Assert.That(ex!.ParamName).IsEqualTo("MethodName");
    }

    [Test]
    public async Task A_Simulated_Call_With_No_Method_Name_Has_No_Call_Data()
    {
        var callData = new EvmCallData(new EvmAddress(new byte[20]), "");
        await Assert.That(callData.Data!.Value.Length).IsEqualTo(0);
    }
}
