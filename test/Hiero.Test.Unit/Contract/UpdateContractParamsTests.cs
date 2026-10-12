// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Proto;

namespace Hiero.Test.Unit.Contract;

public class UpdateContractParamsTests
{
    private static readonly EntityId Contract = new(0, 0, 3001);

    [Test]
    public async Task An_Empty_Memo_Is_Sent_So_The_Memo_Is_Cleared()
    {
        var body = CreateBody(new UpdateContractParams { Contract = Contract, Memo = "" });
        await Assert.That(body.MemoWrapper).IsEqualTo("");
    }

    [Test]
    public async Task A_Null_Memo_Leaves_The_Memo_Unchanged()
    {
        var body = CreateBody(new UpdateContractParams { Contract = Contract, RenewPeriod = TimeSpan.FromDays(90) });
        await Assert.That(body.MemoWrapper).IsNull();
    }

    private static ContractUpdateTransactionBody CreateBody(INetworkParams<TransactionReceipt> parameters)
    {
        return (ContractUpdateTransactionBody)parameters.CreateNetworkTransaction();
    }
}
