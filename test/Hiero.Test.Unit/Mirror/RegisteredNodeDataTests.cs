// SPDX-License-Identifier: Apache-2.0
using System.Text.Json;
using Hiero.Mirror;
using Hiero.Mirror.Implementation;

namespace Hiero.Test.Unit.Mirror;

public class RegisteredNodeDataTests
{
    [Test]
    public async Task Deserializes_Registered_Block_Node_Page()
    {
        // Shape per the mirror node's OpenAPI schema for
        // /api/v1/network/registered-nodes (admin_key omitted — it is
        // optional and not needed for discovery).
        var json = """
        {
            "registered_nodes": [
                {
                    "registered_node_id": 7,
                    "description": "alpha block node",
                    "service_endpoints": [
                        {
                            "domain_name": "block1.example.com",
                            "ip_address": null,
                            "port": 40840,
                            "requires_tls": true,
                            "block_node": { "endpoint_apis": ["STATUS", "SUBSCRIBE_STREAM", "PUBLISH"] }
                        },
                        {
                            "domain_name": null,
                            "ip_address": "127.0.0.1",
                            "port": 8602,
                            "requires_tls": false,
                            "block_node": { "endpoint_apis": ["SUBSCRIBE_STREAM"] }
                        }
                    ]
                }
            ],
            "links": { "next": null }
        }
        """;
        var page = JsonSerializer.Deserialize<RegisteredNodeDataPage>(json);
        await Assert.That(page).IsNotNull();
        var nodes = page!.GetItems();
        await Assert.That(nodes.Length).IsEqualTo(1);
        var node = nodes[0];
        await Assert.That(node.RegisteredNodeId).IsEqualTo(7L);
        await Assert.That(node.Description).IsEqualTo("alpha block node");
        await Assert.That(node.Endpoints.Length).IsEqualTo(2);

        var tlsEndpoint = node.Endpoints[0];
        await Assert.That(tlsEndpoint.RequiresTls).IsTrue();
        await Assert.That(tlsEndpoint.BlockNode).IsNotNull();
        await Assert.That(tlsEndpoint.BlockNode!.EndpointApis).Contains("PUBLISH");
        await Assert.That(tlsEndpoint.ToUri()!.ToString()).IsEqualTo("https://block1.example.com:40840/");

        var ipEndpoint = node.Endpoints[1];
        await Assert.That(ipEndpoint.RequiresTls).IsFalse();
        await Assert.That(ipEndpoint.ToUri()!.ToString()).IsEqualTo("http://127.0.0.1:8602/");
    }

    [Test]
    public async Task Deserializes_Live_Mainnet_Registered_Block_Node()
    {
        // Mainnet registered node 40 as served by mirror node v0.164
        // (endpoints trimmed to the first).
        var json = $$"""
        {
            "registered_nodes": [
                {
                    "admin_key": { "_type": "ProtobufEncoded", "key": "{{ConsensusNodeDataTests.ProtobufEncodedThresholdKey}}" },
                    "created_timestamp": "1787154605.435466104",
                    "description": "Hosted by Swirlds",
                    "registered_node_id": 40,
                    "service_endpoints": [
                        {
                            "block_node": { "endpoint_apis": ["SUBSCRIBE_STREAM"] },
                            "domain_name": null,
                            "general_service": null,
                            "ip_address": "91.242.214.237",
                            "mirror_node": null,
                            "port": 40980,
                            "requires_tls": false,
                            "rpc_relay": null,
                            "type": "BLOCK_NODE"
                        }
                    ],
                    "timestamp": { "from": "1787154605.435466104", "to": null }
                }
            ],
            "links": { "next": null }
        }
        """;
        var node = JsonSerializer.Deserialize(json, MirrorJsonContext.Default.RegisteredNodeDataPage)!.GetItems()[0];
        await Assert.That(node.RegisteredNodeId).IsEqualTo(40L);
        await Assert.That(node.Administrator!.Type).IsEqualTo(KeyType.List);
        await Assert.That(node.Created).IsEqualTo(new ConsensusTimeStamp(1787154605, 435466104));
        await Assert.That(node.ValidRange!.Starting).IsEqualTo(new ConsensusTimeStamp(1787154605, 435466104));
        await Assert.That(node.ValidRange.Ending).IsNull();

        var endpoint = node.Endpoints[0];
        await Assert.That(endpoint.Type).IsEqualTo("BLOCK_NODE");
        await Assert.That(endpoint.GeneralService).IsNull();
        await Assert.That(endpoint.BlockNode!.EndpointApis).Contains("SUBSCRIBE_STREAM");
        await Assert.That(endpoint.ToUri()!.ToString()).IsEqualTo("http://91.242.214.237:40980/");
    }

    [Test]
    public async Task General_Service_Endpoint_Reads_Its_Description()
    {
        var json = """
        {
            "registered_nodes": [
                {
                    "registered_node_id": 9,
                    "service_endpoints": [
                        {
                            "block_node": null,
                            "domain_name": "svc.example.com",
                            "general_service": { "description": "Archive" },
                            "port": 443,
                            "requires_tls": true,
                            "type": "GENERAL_SERVICE"
                        }
                    ]
                }
            ]
        }
        """;
        var endpoint = JsonSerializer.Deserialize(json, MirrorJsonContext.Default.RegisteredNodeDataPage)!.GetItems()[0].Endpoints[0];
        await Assert.That(endpoint.Type).IsEqualTo("GENERAL_SERVICE");
        await Assert.That(endpoint.GeneralService!.Description).IsEqualTo("Archive");
        await Assert.That(endpoint.BlockNode).IsNull();
    }

    [Test]
    public async Task Endpoint_ToUri_Prefers_Domain_And_Returns_Null_Without_Host()
    {
        var domain = new RegisteredNodeEndpointData { DomainName = "bn.example.com", IpAddress = "10.0.0.1", Port = 40840, RequiresTls = true };
        await Assert.That(domain.ToUri()!.Host).IsEqualTo("bn.example.com");

        var neither = new RegisteredNodeEndpointData { Port = 40840 };
        await Assert.That(neither.ToUri()).IsNull();
    }
}
