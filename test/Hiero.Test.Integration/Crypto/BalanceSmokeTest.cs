// SPDX-License-Identifier: Apache-2.0
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Crypto;

public class BalanceSmokeTest
{
    [Test]
    public async Task Can_Query_Payer_Balance()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var balance = await client.GetAccountBalanceAsync(TestNetwork.Payer);

        await Assert.That(balance).IsGreaterThan(0UL);
    }
}
