// SPDX-License-Identifier: Apache-2.0
using Google.Protobuf;
using System.Net;

namespace Proto;

public sealed partial class ServiceEndpoint
{
    internal ServiceEndpoint(Uri uri) : this()
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri)
        {
            throw new ArgumentException("The URI must be absolute.", nameof(uri));
        }
        Port = uri.Port;
        // better than Host for internationalized names
        var host = uri.IdnHost;
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new ArgumentException("The URI host is missing.", nameof(uri));
        }
        var hostType = Uri.CheckHostName(host);
        if (hostType == UriHostNameType.IPv4 && IPAddress.TryParse(host, out var ip))
        {
            // IPv4 bytes are already returned in big-endian order
            IpAddressV4 = ByteString.CopyFrom(ip.GetAddressBytes());
            return;
        }
        if (hostType == UriHostNameType.Dns)
        {
            if (host.Length > 253)
            {
                throw new ArgumentException("The domain name must not exceed 253 characters.", nameof(uri));
            }
            DomainName = host;
            return;
        }
        throw new ArgumentException("Only IPv4 addresses or domain names are supported.", nameof(uri));
    }
}