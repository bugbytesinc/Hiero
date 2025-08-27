using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror;
/// <summary>
/// Represents a gossip node’s gRPC endpoint.
/// </summary>
public class GrpcEndpointData
{
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