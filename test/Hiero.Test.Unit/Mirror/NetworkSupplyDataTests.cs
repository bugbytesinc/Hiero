// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;

namespace Hiero.Test.Unit.Mirror;

public class NetworkSupplyDataTests
{
    [Test]
    public async Task Deserializes_Number_String_Supply_Fields()
    {
        var json = """
        {
            "released_supply":"3999999999999999949",
            "timestamp":"1700000000.123456789",
            "total_supply":"5000000000000000000"
        }
        """;
        var data = JsonSerializer.Deserialize<NetworkSupplyData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.ReleasedSupply).IsEqualTo(3_999_999_999_999_999_949L);
        await Assert.That(data.TotalSupply).IsEqualTo(5_000_000_000_000_000_000L);
        await Assert.That(data.Timestamp).IsEqualTo(new ConsensusTimeStamp(1_700_000_000L, 123_456_789));
    }

    [Test]
    public async Task Tolerates_Numeric_Supply_Fields()
    {
        var json = """
        {
            "released_supply":1234567890,
            "timestamp":"1700000000.000000000",
            "total_supply":5000000000000000000
        }
        """;
        var data = JsonSerializer.Deserialize<NetworkSupplyData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.ReleasedSupply).IsEqualTo(1_234_567_890L);
        await Assert.That(data.TotalSupply).IsEqualTo(5_000_000_000_000_000_000L);
    }

    [Test]
    public async Task Defaults_Are_Zero_When_Fields_Absent()
    {
        var json = """{}""";
        var data = JsonSerializer.Deserialize<NetworkSupplyData>(json);
        await Assert.That(data).IsNotNull();
        await Assert.That(data!.ReleasedSupply).IsEqualTo(0L);
        await Assert.That(data.TotalSupply).IsEqualTo(0L);
    }
}
