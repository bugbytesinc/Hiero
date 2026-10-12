// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror;
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;

namespace Hiero.Test.Integration.BlockNode;

[Category("Block Node")]
public class RegisteredNodeRoundTripTests
{
    /// <summary>
    /// The full HIP-1137 circle: register the block node on-chain,
    /// discover it through the mirror, connect to the discovered
    /// endpoint, and query its status — then clean up.
    /// </summary>
    /// <remarks>
    /// Registered-node creation is restricted to privileged system
    /// payers, so this test requires [PrivilegedPayerPrivateKey] in
    /// configuration (for Solo, the well-known genesis account 0.0.2
    /// key) and skips without it.
    /// </remarks>
    [Test]
    public async Task Register_Discover_Connect_RoundTrip()
    {
        if (TestNetwork.BlockNodeUri is null)
        {
            Skip.Test("Block Node Endpoint [BlockNodeEndpoint] is not configured.");
        }
        if (!TestNetwork.HasPrivilegedPayer)
        {
            Skip.Test("Registered-node creation requires a privileged payer [PrivilegedPayerPrivateKey], which is not configured.");
        }
        var client = await TestNetwork.CreatePrivilegedClientAsync();
        var description = $"hiero-sdk-test-{Generator.Integer(100000, 999999)}";

        RegisteredNodeReceipt receipt;
        try
        {
            receipt = await client.CreateRegisteredNodeAsync(new CreateRegisteredNodeParams
            {
                AdminKey = TestNetwork.PrivilegedEndorsement,
                Description = description,
                Endpoints =
                [
                    new RegisteredNodeEndpoint
                    {
                        Uri = TestNetwork.BlockNodeUri!,
                        BlockNodeApis = [BlockNodeApi.Status, BlockNodeApi.SubscribeStream]
                    }
                ]
            });
        }
        catch (PrecheckException ex) when (ex.Status is ResponseCode.NotSupported)
        {
            Skip.Test($"Registered-node creation is not supported on this network ({ex.Status}).");
            return;
        }
        catch (TransactionException ex) when (ex.Status is ResponseCode.NotSupported)
        {
            Skip.Test($"Registered-node creation is not supported on this network ({ex.Status}).");
            return;
        }

        await Assert.That(receipt.RegisteredNodeId).IsGreaterThan(0ul);
        var registeredNodeId = receipt.RegisteredNodeId;

        // The direct call above types its own receipt, so it never exercised
        // the type sniffer — which is how this shipped with no
        // RegisteredNodeReceipt branch at all, leaving the id unreachable
        // through the "get all" path.  Assert that path explicitly.
        var allReceipts = await client.GetAllReceiptsAsync(receipt.TransactionId);
        var refetched = allReceipts.Parent as RegisteredNodeReceipt;
        await Assert.That(refetched).IsNotNull();
        await Assert.That(refetched!.RegisteredNodeId).IsEqualTo(registeredNodeId);

        try
        {
            var mirror = await TestNetwork.GetMirrorRestClientAsync();
            RegisteredNodeData? discovered = null;
            await foreach (var node in mirror.GetRegisteredBlockNodesAsync())
            {
                if ((ulong)node.RegisteredNodeId == registeredNodeId)
                {
                    discovered = node;
                    break;
                }
            }
            await Assert.That(discovered).IsNotNull();
            await Assert.That(discovered!.Description).IsEqualTo(description);
            var endpoint = discovered.Endpoints.FirstOrDefault(e => e.BlockNode is not null);
            await Assert.That(endpoint).IsNotNull();
            await Assert.That(endpoint!.BlockNode!.EndpointApis).Contains("SUBSCRIBE_STREAM");

            // Connect to the discovered endpoint and confirm it answers.
            // This deliberately opens a fresh channel (as a real application
            // discovering a node would) rather than reusing the fixture's
            // warm one, so it is given a more patient retry budget: a
            // newly-opened connection to a tunneled test node is
            // occasionally reset for a few seconds, which is not what this
            // test is asserting.
            var uri = endpoint.ToUri();
            await Assert.That(uri).IsNotNull();
            await using var blockNode = new BlockNodeClient(ctx =>
            {
                ctx.Uri = uri!;
                ctx.RetryCount = 8;
                ctx.RetryDelay = TimeSpan.FromMilliseconds(250);
            });
            var status = await blockNode.GetServerStatusAsync();
            await Assert.That(status.LastAvailableBlockNumber).IsGreaterThanOrEqualTo(status.FirstAvailableBlockNumber);
        }
        finally
        {
            await client.DeleteRegisteredNodeAsync(registeredNodeId);
        }
    }
}
