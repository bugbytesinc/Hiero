// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;
using Com.Hedera.Hapi.Node.Addressbook;
using Google.Protobuf;
using System.Net;

namespace Hiero;
/// <summary>
/// A block-node service endpoint to publish when registering or
/// updating a node in the HIP-1137 registry.
/// </summary>
/// <remarks>
/// This SDK's registered-node support is block-node focused: each
/// endpoint advertises the Block Node APIs it serves.  The address,
/// port and TLS requirement are taken from <see cref="Uri"/> — an
/// <c>https</c> scheme sets the TLS-required flag, and a host that
/// parses as an IP address is sent as an IP endpoint, otherwise as a
/// domain name.
/// </remarks>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed class RegisteredNodeEndpoint
{
    /// <summary>
    /// The endpoint's address: host (domain name or IP), port, and —
    /// via the scheme (<c>https</c> vs <c>http</c>) — whether TLS is
    /// required. This field is REQUIRED.
    /// </summary>
    public Uri Uri { get; set; } = default!;
    /// <summary>
    /// The Block Node APIs this endpoint serves. This field is
    /// REQUIRED and must contain at least one value.
    /// </summary>
    public IReadOnlyList<BlockNodeApi> BlockNodeApis { get; set; } = default!;

    internal RegisteredServiceEndpoint ToProtobuf()
    {
        if (Uri is null)
        {
            throw new ArgumentNullException(nameof(Uri), "A registered node endpoint URI is required.");
        }
        if (BlockNodeApis is null || BlockNodeApis.Count == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(BlockNodeApis), "At least one Block Node API is required for a registered node endpoint.");
        }
        if (Uri.Port < 0)
        {
            // A scheme with no default port reports -1, which would be sent as 4294967295.
            throw new ArgumentException("A registered node endpoint URI must include a port.", nameof(Uri));
        }
        var endpoint = new RegisteredServiceEndpoint
        {
            Port = (uint)Uri.Port,
            RequiresTls = string.Equals(Uri.Scheme, "https", StringComparison.OrdinalIgnoreCase)
        };
        if (IPAddress.TryParse(Uri.Host, out var ip))
        {
            endpoint.IpAddress = ByteString.CopyFrom(ip.GetAddressBytes());
        }
        else
        {
            endpoint.DomainName = Uri.IdnHost;
        }
        var blockNode = new RegisteredServiceEndpoint.Types.BlockNodeEndpoint();
        foreach (var api in BlockNodeApis)
        {
            blockNode.EndpointApi.Add((RegisteredServiceEndpoint.Types.BlockNodeEndpoint.Types.BlockNodeApi)api);
        }
        endpoint.BlockNode = blockNode;
        return endpoint;
    }
}
