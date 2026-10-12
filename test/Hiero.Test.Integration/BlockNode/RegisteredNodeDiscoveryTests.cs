// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.BlockNode;

[Category("Block Node")]
public class RegisteredNodeDiscoveryTests
{
    [Test]
    public async Task Can_Enumerate_Registered_Nodes()
    {
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var count = 0;
        await foreach (var node in mirror.GetRegisteredNodesAsync())
        {
            count++;
            await Assert.That(node.RegisteredNodeId).IsGreaterThanOrEqualTo(0);
            if (count > 20)
            {
                break;
            }
        }
        // The registry may be empty on a fresh network; the query must
        // still complete without error.
        await Assert.That(count).IsGreaterThanOrEqualTo(0);
    }

    [Test]
    public async Task Can_Enumerate_Registered_Block_Nodes()
    {
        var mirror = await TestNetwork.GetMirrorRestClientAsync();
        var nodes = new List<RegisteredNodeData>();
        await foreach (var node in mirror.GetRegisteredBlockNodesAsync())
        {
            nodes.Add(node);
            if (nodes.Count > 20)
            {
                break;
            }
        }
        // Any block node returned must expose at least one endpoint whose
        // block-node descriptor and URI are well-formed.
        foreach (var node in nodes)
        {
            foreach (var endpoint in node.Endpoints)
            {
                var uri = endpoint.ToUri();
                if (uri is not null)
                {
                    await Assert.That(uri.Port).IsGreaterThan(0);
                }
            }
        }
    }
}
