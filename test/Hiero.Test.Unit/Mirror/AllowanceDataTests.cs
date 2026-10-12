// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;
using Hiero.Mirror.Implementation;

namespace Hiero.Test.Unit.Mirror;

public class AllowanceDataTests
{
    [Test]
    public async Task Crypto_Allowance_Amount_Is_What_Remains_Of_The_Grant()
    {
        // Matches TokenAllowanceData: Amount is the remaining allowance,
        // AmountGranted the original grant. (Before 3.0.0, Amount held the
        // original grant.)
        var json = """
        {
            "allowances": [
                {
                    "amount": 75,
                    "amount_granted": 100,
                    "owner": "0.0.1001",
                    "spender": "0.0.2002",
                    "timestamp": { "from": "1791480886.510917639", "to": null }
                }
            ],
            "links": { "next": null }
        }
        """;
        var allowance = JsonSerializer.Deserialize(json, MirrorJsonContext.Default.CryptoAllowanceDataPage)!.GetItems()[0];
        await Assert.That(allowance.Amount).IsEqualTo(75L);
        await Assert.That(allowance.AmountGranted).IsEqualTo(100L);
        await Assert.That(allowance.Owner).IsEqualTo(new EntityId(0, 0, 1001));
        await Assert.That(allowance.Spender).IsEqualTo(new EntityId(0, 0, 2002));
        await Assert.That(allowance.Timestamp!.Starting).IsEqualTo(new ConsensusTimeStamp(1791480886, 510917639));
        await Assert.That(allowance.Timestamp.Ending).IsNull();
    }

    [Test]
    public async Task Token_Allowance_Reads_Its_Timestamp()
    {
        var json = """
        {
            "allowances": [
                {
                    "amount": 3,
                    "amount_granted": 10,
                    "owner": "0.0.1001",
                    "spender": "0.0.2002",
                    "timestamp": { "from": "1791480886.510917639", "to": "1791480999.000000000" },
                    "token_id": "0.0.3003"
                }
            ],
            "links": { "next": null }
        }
        """;
        var allowance = JsonSerializer.Deserialize(json, MirrorJsonContext.Default.TokenAllowanceDataPage)!.GetItems()[0];
        await Assert.That(allowance.Amount).IsEqualTo(3L);
        await Assert.That(allowance.AmountGranted).IsEqualTo(10L);
        await Assert.That(allowance.Timestamp!.Ending).IsEqualTo(new ConsensusTimeStamp(1791480999, 0));
    }
}
