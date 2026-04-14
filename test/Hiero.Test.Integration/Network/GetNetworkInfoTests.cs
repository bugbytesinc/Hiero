using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Network;

public class GetNetworkInfoTests
{
    [Test]
    public async Task Can_Get_Network_Version_Info()
    {
        await using var client = await TestNetwork.CreateClientAsync();

        var info = await client.GetVersionInfoAsync();
        await Assert.That(info).IsNotNull();
        AssertHg.SemanticVersionGreaterOrEqualThan(new SemanticVersion(0, 21, 2), info.ApiProtobufVersion);
        AssertHg.SemanticVersionGreaterOrEqualThan(new SemanticVersion(0, 21, 2), info.HederaServicesVersion);
    }
}
