// SPDX-License-Identifier: Apache-2.0
using System.Net;
using Com.Hedera.Hapi.Node.Addressbook;

namespace Hiero.Test.Unit.BlockNode;

/// <summary>
/// Covers the write side of HIP-1137 endpoint conversion —
/// <see cref="RegisteredNodeEndpoint.ToProtobuf"/> — which must agree
/// with the read side (<c>RegisteredNodeEndpointData.ToUri</c>) about
/// how hosts, ports and TLS are represented on the wire.
/// </summary>
public class RegisteredNodeEndpointTests
{
    private static RegisteredServiceEndpoint Convert(string uri, params BlockNodeApi[] apis)
    {
        return new RegisteredNodeEndpoint
        {
            Uri = new Uri(uri),
            BlockNodeApis = apis.Length > 0 ? apis : [BlockNodeApi.Status]
        }.ToProtobuf();
    }

    [Test]
    public async Task IPv4_Host_Is_Sent_As_An_Ip_Address()
    {
        var endpoint = Convert("http://127.0.0.1:8602");
        await Assert.That(endpoint.AddressCase).IsEqualTo(RegisteredServiceEndpoint.AddressOneofCase.IpAddress);
        await Assert.That(endpoint.IpAddress.ToByteArray()).IsEquivalentTo(IPAddress.Parse("127.0.0.1").GetAddressBytes());
        await Assert.That(endpoint.IpAddress.Length).IsEqualTo(4);
        await Assert.That(endpoint.Port).IsEqualTo(8602u);
        await Assert.That(endpoint.RequiresTls).IsFalse();
    }

    [Test]
    public async Task IPv6_Host_Is_Sent_As_A_Sixteen_Byte_Ip_Address()
    {
        // The URI form brackets IPv6 literals; the wire form must carry
        // the 16 raw address bytes, not a bracketed domain name.
        var endpoint = Convert("http://[2001:db8::1]:40840");
        await Assert.That(endpoint.AddressCase).IsEqualTo(RegisteredServiceEndpoint.AddressOneofCase.IpAddress);
        await Assert.That(endpoint.IpAddress.Length).IsEqualTo(16);
        await Assert.That(endpoint.IpAddress.ToByteArray()).IsEquivalentTo(IPAddress.Parse("2001:db8::1").GetAddressBytes());
    }

    [Test]
    public async Task Domain_Host_Is_Sent_As_A_Domain_Name()
    {
        var endpoint = Convert("https://block1.example.com:40840");
        await Assert.That(endpoint.AddressCase).IsEqualTo(RegisteredServiceEndpoint.AddressOneofCase.DomainName);
        await Assert.That(endpoint.DomainName).IsEqualTo("block1.example.com");
        await Assert.That(endpoint.RequiresTls).IsTrue();
    }

    [Test]
    public async Task Https_Scheme_Sets_Requires_Tls()
    {
        await Assert.That(Convert("https://bn.example.com:40840").RequiresTls).IsTrue();
        await Assert.That(Convert("http://bn.example.com:40840").RequiresTls).IsFalse();
    }

    [Test]
    public async Task Block_Node_Apis_Map_To_The_Wire_Enum()
    {
        var endpoint = Convert("http://bn.example.com:40840",
            BlockNodeApi.Status, BlockNodeApi.Publish, BlockNodeApi.SubscribeStream, BlockNodeApi.StateProof, BlockNodeApi.Other);
        await Assert.That(endpoint.BlockNode).IsNotNull();
        await Assert.That(endpoint.BlockNode.EndpointApi).IsEquivalentTo(
        [
            RegisteredServiceEndpoint.Types.BlockNodeEndpoint.Types.BlockNodeApi.Status,
            RegisteredServiceEndpoint.Types.BlockNodeEndpoint.Types.BlockNodeApi.Publish,
            RegisteredServiceEndpoint.Types.BlockNodeEndpoint.Types.BlockNodeApi.SubscribeStream,
            RegisteredServiceEndpoint.Types.BlockNodeEndpoint.Types.BlockNodeApi.StateProof,
            RegisteredServiceEndpoint.Types.BlockNodeEndpoint.Types.BlockNodeApi.Other,
        ]);
    }

    /// <summary>
    /// Every public <see cref="BlockNodeApi"/> value must have the same
    /// numeric value as its wire counterpart, since the conversion is a
    /// cast; a divergence would silently register the wrong API.
    /// </summary>
    [Test]
    public async Task Public_Api_Enum_Values_Match_The_Wire_Enum()
    {
        foreach (var api in Enum.GetValues<BlockNodeApi>())
        {
            var wire = (RegisteredServiceEndpoint.Types.BlockNodeEndpoint.Types.BlockNodeApi)api;
            await Assert.That(Enum.IsDefined(wire)).IsTrue();
            await Assert.That(wire.ToString()).IsEqualTo(api.ToString());
        }
    }

    [Test]
    public async Task Missing_Uri_Throws()
    {
        var endpoint = new RegisteredNodeEndpoint { BlockNodeApis = [BlockNodeApi.Status] };
        var exception = Assert.Throws<ArgumentNullException>(() => endpoint.ToProtobuf());
        await Assert.That(exception!.ParamName).IsEqualTo("Uri");
    }

    [Test]
    public async Task Missing_Or_Empty_Apis_Throws()
    {
        var noApis = new RegisteredNodeEndpoint { Uri = new Uri("http://bn.example.com:40840"), BlockNodeApis = [] };
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => noApis.ToProtobuf());
        await Assert.That(exception!.ParamName).IsEqualTo("BlockNodeApis");
        var nullApis = new RegisteredNodeEndpoint { Uri = new Uri("http://bn.example.com:40840") };
        Assert.Throws<ArgumentOutOfRangeException>(() => nullApis.ToProtobuf());
    }

    /// <summary>
    /// The write and read sides must agree: an endpoint registered from
    /// a URI must come back as the same URI once the mirror echoes it.
    /// </summary>
    [Test]
    [Arguments("http://127.0.0.1:8602/")]
    [Arguments("http://[2001:db8::1]:40840/")]
    [Arguments("https://block1.example.com:40840/")]
    public async Task Endpoint_Round_Trips_Through_The_Wire_Form(string uri)
    {
        var wire = Convert(uri);
        // Mirror the REST projection the mirror node would publish.
        var data = new Hiero.Mirror.RegisteredNodeEndpointData
        {
            Port = (int)wire.Port,
            RequiresTls = wire.RequiresTls,
            DomainName = wire.AddressCase == RegisteredServiceEndpoint.AddressOneofCase.DomainName ? wire.DomainName : null,
            IpAddress = wire.AddressCase == RegisteredServiceEndpoint.AddressOneofCase.IpAddress
                ? new IPAddress(wire.IpAddress.ToByteArray()).ToString()
                : null
        };
        await Assert.That(data.ToUri()!.AbsoluteUri).IsEqualTo(uri);
    }

    [Test]
    public async Task A_Uri_Without_A_Port_Is_Rejected_Rather_Than_Sent_As_Port_4294967295()
    {
        await Assert.ThrowsAsync<ArgumentException>(async () => await Task.Run(() => Convert("grpc://block.example.com")));
    }
}
