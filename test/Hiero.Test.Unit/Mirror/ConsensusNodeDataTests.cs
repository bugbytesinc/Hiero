// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;
using Hiero.Mirror.Implementation;

namespace Hiero.Test.Unit.Mirror;

public class ConsensusNodeDataTests
{
    // A real threshold key (2 of 2) as the mirror node serves it, protobuf
    // encoded: the admin key of mainnet registered node 40.
    internal const string ProtobufEncodedThresholdKey = "2af001080212eb010a4e2a4c080112480a221220ef2d877b88b7464d9253560b8851316f5c2f6ddf935eb4eec0761a3262b0a48c0a221220a95d54cf49c1d08cd16d8908f37dfad95637134ffaf528a1d96da7f28d45f1390a98012a950108021290010a221220c44c911fa45166e356b498463184459dd9ee760bacc083de348691d6357e06340a221220ef2d877b88b7464d9253560b8851316f5c2f6ddf935eb4eec0761a3262b0a48c0a221220a95d54cf49c1d08cd16d8908f37dfad95637134ffaf528a1d96da7f28d45f1390a221220daa5da866bf4e990c14eff4336f5ab4b416c85a31289c8cb8ae1b4a54ce8c111";

    [Test]
    public async Task Node_Reads_Admin_Key_Proxy_Staking_Period_And_Registered_Nodes()
    {
        var json = $$"""
        {
            "nodes": [
                {
                    "admin_key": { "_type": "ProtobufEncoded", "key": "{{ProtobufEncodedThresholdKey}}" },
                    "associated_registered_nodes": [40, 41],
                    "decline_reward": true,
                    "description": "Hosted by Example",
                    "file_id": "0.0.102",
                    "grpc_proxy_endpoint": { "domain_name": "grpc-web.example.com", "ip_address_v4": "", "port": 443 },
                    "node_account_id": "0.0.3",
                    "node_id": 0,
                    "service_endpoints": [
                        { "domain_name": "", "ip_address_v4": "34.64.136.190", "port": 50211 }
                    ],
                    "staking_period": { "from": "1791417600.000000000", "to": "1791504000.000000000" },
                    "timestamp": { "from": "1791417600.000000001", "to": null }
                }
            ],
            "links": { "next": null }
        }
        """;
        var node = JsonSerializer.Deserialize(json, MirrorJsonContext.Default.ConsensusNodeDataPage)!.GetItems()[0];

        await Assert.That(node.Administrator).IsNotNull();
        await Assert.That(node.Administrator!.Type).IsEqualTo(KeyType.List);
        await Assert.That(node.Administrator.RequiredCount).IsEqualTo(2U);
        await Assert.That(node.AssociatedRegisteredNodes).IsEquivalentTo(new long[] { 40, 41 });
        await Assert.That(node.DeclineReward).IsTrue();
        await Assert.That(node.GrpcProxyEndpoint!.DomainName).IsEqualTo("grpc-web.example.com");
        await Assert.That(node.GrpcProxyEndpoint.Port).IsEqualTo(443);
        await Assert.That(node.StakingPeriod!.Starting).IsEqualTo(new ConsensusTimeStamp(1791417600, 0));
        await Assert.That(node.StakingPeriod.Ending).IsEqualTo(new ConsensusTimeStamp(1791504000, 0));
        await Assert.That(node.Endpoints[0].DomainName).IsEqualTo("");
        await Assert.That(node.Endpoints[0].Address).IsEqualTo("34.64.136.190");
    }

    [Test]
    public async Task Node_Without_Proxy_Or_Registered_Nodes()
    {
        var json = """
        {
            "nodes": [
                { "admin_key": null, "associated_registered_nodes": [], "decline_reward": false, "grpc_proxy_endpoint": null, "node_account_id": "0.0.4", "service_endpoints": [] }
            ],
            "links": { "next": null }
        }
        """;
        var node = JsonSerializer.Deserialize(json, MirrorJsonContext.Default.ConsensusNodeDataPage)!.GetItems()[0];
        await Assert.That(node.Administrator).IsNull();
        await Assert.That(node.AssociatedRegisteredNodes.Length).IsEqualTo(0);
        await Assert.That(node.DeclineReward).IsFalse();
        await Assert.That(node.GrpcProxyEndpoint).IsNull();
        await Assert.That(node.StakingPeriod).IsNull();
    }
}
