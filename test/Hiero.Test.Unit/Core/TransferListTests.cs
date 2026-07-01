// SPDX-License-Identifier: Apache-2.0
using Proto;

namespace Hiero.Test.Unit.Core;

public class TransferListTests
{
    [Test]
    public async Task ToTransfers_Returns_Empty_Dictionary_For_Empty_List()
    {
        var list = new TransferList();

        var result = list.ToTransfers();

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task ToTransfers_Aggregates_Duplicate_Account_Amounts()
    {
        var accountOne = new EntityId(0, 0, 1001);
        var accountTwo = new EntityId(0, 0, 1002);
        var list = new TransferList();
        list.AccountAmounts.Add(new AccountAmount { AccountID = new AccountID(accountOne), Amount = 10 });
        list.AccountAmounts.Add(new AccountAmount { AccountID = new AccountID(accountOne), Amount = 15 });
        list.AccountAmounts.Add(new AccountAmount { AccountID = new AccountID(accountTwo), Amount = -5 });

        var result = list.ToTransfers();

        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[accountOne]).IsEqualTo(25);
        await Assert.That(result[accountTwo]).IsEqualTo(-5);
    }
}
