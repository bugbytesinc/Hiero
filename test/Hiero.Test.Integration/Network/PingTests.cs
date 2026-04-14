using Hiero.Test.Integration.Fixtures;
using System.Diagnostics;

namespace Hiero.Test.Integration.Network;

public class PingTests
{
    [Test]
    public async Task Can_Ping_The_Gossip_Node()
    {
        await using var client = await TestNetwork.CreateClientAsync();

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var elapsed = await client.PingAsync();
        stopwatch.Stop();

        await Assert.That(stopwatch.ElapsedMilliseconds >= elapsed).IsTrue();
    }
}
