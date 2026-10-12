// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using Hiero.Mirror.Paging;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// A node registered on-chain via the HIP-1137 registered-node registry
/// (block nodes, mirror nodes, JSON-RPC relays and general services),
/// as returned from the mirror node's
/// <c>/api/v1/network/registered-nodes</c> endpoint.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public class RegisteredNodeData
{
    /// <summary>
    /// The network-assigned identifier of the registered node.
    /// </summary>
    [JsonPropertyName("registered_node_id")]
    public long RegisteredNodeId { get; set; }
    /// <summary>
    /// A short human-readable description of the node, if provided.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    /// <summary>
    /// The administrative key authorized to update or delete the
    /// registration.
    /// </summary>
    [JsonPropertyName("admin_key")]
    public Endorsement? Administrator { get; set; }
    /// <summary>
    /// The consensus timestamp at which the node was registered.
    /// </summary>
    [JsonPropertyName("created_timestamp")]
    public ConsensusTimeStamp? Created { get; set; }
    /// <summary>
    /// The service endpoints this node publishes; a block node's
    /// endpoints carry the list of Block Node APIs they serve.
    /// </summary>
    [JsonPropertyName("service_endpoints")]
    public RegisteredNodeEndpointData[] Endpoints { get; set; } = [];
    /// <summary>
    /// The range of time this version of the registration is valid
    /// for; an open-ended range is the current version.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public TimestampRangeData? ValidRange { get; set; }
}
/// <summary>
/// A single service endpoint published by a
/// <see cref="RegisteredNodeData"/>.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public class RegisteredNodeEndpointData
{
    /// <summary>
    /// The DNS domain name of the service, if published by name.
    /// Exactly one of <see cref="DomainName"/> or
    /// <see cref="IpAddress"/> is populated.
    /// </summary>
    [JsonPropertyName("domain_name")]
    public string? DomainName { get; set; }
    /// <summary>
    /// The IP address of the service, if published by address.
    /// </summary>
    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }
    /// <summary>
    /// The port the service listens on.
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; }
    /// <summary>
    /// Whether connecting to this endpoint requires TLS.
    /// </summary>
    [JsonPropertyName("requires_tls")]
    public bool RequiresTls { get; set; }
    /// <summary>
    /// The kind of service this endpoint provides — one of
    /// <c>BLOCK_NODE</c>, <c>MIRROR_NODE</c>, <c>RPC_RELAY</c> or
    /// <c>GENERAL_SERVICE</c>, the values
    /// <see cref="RegisteredNodeTypeFilter"/> filters on.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    /// <summary>
    /// The block-node service descriptor when this endpoint serves the
    /// Block Node APIs; <code>null</code> for other endpoint kinds.
    /// </summary>
    [JsonPropertyName("block_node")]
    public RegisteredBlockNodeEndpointData? BlockNode { get; set; }
    /// <summary>
    /// The general-service descriptor when this endpoint is a
    /// <c>GENERAL_SERVICE</c>; <code>null</code> for other endpoint
    /// kinds.
    /// </summary>
    [JsonPropertyName("general_service")]
    public RegisteredGeneralServiceEndpointData? GeneralService { get; set; }
    /// <summary>
    /// Builds the gRPC URI for this endpoint, using <c>https</c> when
    /// TLS is required and <c>http</c> otherwise.
    /// </summary>
    /// <remarks>
    /// IPv6 addresses are bracketed as the URI syntax requires, so an
    /// endpoint published as <c>::1</c> yields <c>http://[::1]:40840</c>.
    /// </remarks>
    /// <returns>
    /// The endpoint's URI, or <code>null</code> when the endpoint
    /// publishes neither a domain name nor an IP address.
    /// </returns>
    public Uri? ToUri()
    {
        var host = !string.IsNullOrWhiteSpace(DomainName) ? DomainName : IpAddress;
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }
        // UriBuilder brackets IPv6 literals; string interpolation does not,
        // and an unbracketed IPv6 host makes the Uri constructor throw.
        return new UriBuilder(RequiresTls ? "https" : "http", host.Trim('[', ']'), Port).Uri;
    }
}
/// <summary>
/// The block-node descriptor on a registered service endpoint: the set
/// of Block Node APIs the endpoint serves.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public class RegisteredBlockNodeEndpointData
{
    /// <summary>
    /// The Block Node APIs this endpoint serves — values from the set
    /// <c>STATUS</c>, <c>PUBLISH</c>, <c>SUBSCRIBE_STREAM</c>,
    /// <c>STATE_PROOF</c>, <c>OTHER</c>.
    /// </summary>
    [JsonPropertyName("endpoint_apis")]
    public string[] EndpointApis { get; set; } = [];
}
/// <summary>
/// The general-service descriptor on a registered service endpoint.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public class RegisteredGeneralServiceEndpointData
{
    /// <summary>
    /// A description of the service the endpoint provides.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
/// <summary>
/// Extension methods for querying the HIP-1137 registered-node registry
/// from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public static class RegisteredNodeDataExtensions
{
    /// <summary>
    /// Enumerates registered nodes from
    /// <c>/api/v1/network/registered-nodes</c>. Narrow by kind with
    /// <see cref="RegisteredNodeTypeFilter"/> or by id with
    /// <see cref="RegisteredNodeIdFilter"/>.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="cancellationToken">
    /// Token that cancels the enumeration, including a page request in
    /// flight. <c>WithCancellation</c> on the result works as well.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters. The endpoint supports
    /// <see cref="RegisteredNodeTypeFilter"/>,
    /// <see cref="RegisteredNodeIdFilter"/>,
    /// <see cref="PageLimit"/>, and <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of registered-node records.
    /// </returns>
    public static IAsyncEnumerable<RegisteredNodeData> GetRegisteredNodesAsync(this MirrorRestClient client, CancellationToken cancellationToken, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath("network/registered-nodes", filters);
        return client.GetPagedItemsAsync<RegisteredNodeDataPage, RegisteredNodeData>(path, MirrorJsonContext.Default.RegisteredNodeDataPage, cancellationToken);
    }
    /// <inheritdoc cref="GetRegisteredNodesAsync(MirrorRestClient, CancellationToken, IMirrorQueryParameter[])"/>
    public static IAsyncEnumerable<RegisteredNodeData> GetRegisteredNodesAsync(this MirrorRestClient client, params IMirrorQueryParameter[] filters)
    {
        return client.GetRegisteredNodesAsync(CancellationToken.None, filters);
    }
    /// <summary>
    /// Enumerates registered <b>block</b> nodes — a convenience over
    /// <see cref="GetRegisteredNodesAsync(MirrorRestClient, CancellationToken, IMirrorQueryParameter[])"/> that applies
    /// <see cref="RegisteredNodeTypeFilter.BlockNode"/>.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="cancellationToken">
    /// Token that cancels the enumeration, including a page request in
    /// flight. <c>WithCancellation</c> on the result works as well.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters (the block-node type filter is
    /// applied automatically).
    /// </param>
    /// <returns>
    /// An async enumerable of registered block-node records.
    /// </returns>
    public static IAsyncEnumerable<RegisteredNodeData> GetRegisteredBlockNodesAsync(this MirrorRestClient client, CancellationToken cancellationToken, params IMirrorQueryParameter[] filters)
    {
        var combined = new IMirrorQueryParameter[filters.Length + 1];
        combined[0] = RegisteredNodeTypeFilter.BlockNode;
        Array.Copy(filters, 0, combined, 1, filters.Length);
        return client.GetRegisteredNodesAsync(cancellationToken, combined);
    }
    /// <inheritdoc cref="GetRegisteredBlockNodesAsync(MirrorRestClient, CancellationToken, IMirrorQueryParameter[])"/>
    public static IAsyncEnumerable<RegisteredNodeData> GetRegisteredBlockNodesAsync(this MirrorRestClient client, params IMirrorQueryParameter[] filters)
    {
        return client.GetRegisteredBlockNodesAsync(CancellationToken.None, filters);
    }
}
