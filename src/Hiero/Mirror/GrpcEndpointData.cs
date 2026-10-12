// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;
/// <summary>
/// Represents a gossip node’s gRPC endpoint.
/// </summary>
public class GrpcEndpointData
{
    /// <summary>
    /// The DNS domain name of the endpoint, when the address book
    /// publishes one; empty or null otherwise.
    /// </summary>
    [JsonPropertyName("domain_name")]
    public string? DomainName { get; set; }
    /// <summary>
    /// IPV4 address of the endpoint.
    /// </summary>
    [JsonPropertyName("ip_address_v4")]
    public string? Address { get; set; }
    /// <summary>
    /// Port number to connect with.
    /// </summary>
    [JsonPropertyName("port")]
    [JsonConverter(typeof(IntMirrorConverter))]
    public int Port { get; set; }
}