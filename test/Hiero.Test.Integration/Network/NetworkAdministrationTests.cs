using System.Security.Cryptography;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.Network;

// NOTE: All tests in this class require the configured Payer key to have admin
// rights over the System Freeze Administrator account (Hedera 0.0.58). Without
// those rights, the network returns AUTHORIZATION_FAILED/NOT_SUPPORTED and the
// tests will fail at runtime. Tests are marked [Skip] until such a configuration
// is available. When running against a privileged environment, remove the [Skip]
// attribute.

public class NetworkAdministrationTests
{
    [Test]
    [Skip("Requires System Freeze Admin account which we do not have access to.")]
    public async Task Can_Schedule_And_Cancel_Suspend_Network()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var systemAddress = TestNetwork.SystemFreezeAdminAddress;

        var receipt = await client.SuspendNetworkAsync(
            new SuspendNetworkParams
            {
                Consensus = new ConsensusTimeStamp(DateTime.UtcNow.AddSeconds(20))
            },
            ctx => ctx.Payer = systemAddress);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        receipt = await client.AbortNetworkUpgradeAsync(
            new AbortNetworkUpgradeParams(),
            ctx => ctx.Payer = systemAddress);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await Task.Delay(TimeSpan.FromSeconds(30));

        var info = await client.GetAccountInfoAsync(TestNetwork.Payer);
        await Assert.That(info.Address).IsEqualTo(TestNetwork.Payer);
    }

    [Test]
    [Skip("Requires System Freeze Admin account which we do not have access to.")]
    public async Task Can_Suspend_Network_With_Update_File()
    {
        await using var client = await TestNetwork.CreateClientAsync();
        var systemAddress = TestNetwork.SystemFreezeAdminAddress;

        var specialFileAddress = new EntityId(0, 0, 150);
        var contents = await client.GetFileContentAsync(specialFileAddress);
        var contentHash = SHA384.HashData(contents.ToArray());

        var receipt = await client.PrepareNetworkUpgradeAsync(
            new PrepareNetworkUpgradeParams
            {
                File = specialFileAddress,
                FileHash = contentHash
            },
            ctx => ctx.Payer = systemAddress);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        await Task.Delay(TimeSpan.FromSeconds(10));

        receipt = await client.AbortNetworkUpgradeAsync(
            new AbortNetworkUpgradeParams(),
            ctx => ctx.Payer = systemAddress);
        await Assert.That(receipt.Status).IsEqualTo(ResponseCode.Success);

        var info = await client.GetAccountInfoAsync(TestNetwork.Payer);
        await Assert.That(info.Address).IsEqualTo(TestNetwork.Payer);
    }
}
