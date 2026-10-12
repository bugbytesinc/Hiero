using Grpc.Net.Client;
using Hiero.Mirror;
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Net.Http;
using System.Net.Security;

namespace Hiero.Test.Integration.Network;

public class ChannelFactoryTests
{
    [Test]
    public async Task Can_Create_Client_With_Custom_Channel_Factory()
    {
        ConsensusNodeEndpoint calledGateway = default!;
        // A plain channel only reaches a plaintext endpoint: a TLS one
        // presents a self-signed certificate that standard validation
        // rejects (see the TLS test below).
        var gateway = await TestNetwork.GetConsensusNodeEndpointAsync(ConsensusNodeTransport.Plaintext);

        await using var client = new ConsensusClient(channelFactory, ctx =>
        {
            ctx.Payer = TestNetwork.Payer;
            ctx.Endpoint = gateway;
        });
        await client.PingAsync();
        await Assert.That(calledGateway).IsEqualTo(gateway);

        GrpcChannel channelFactory(ConsensusNodeEndpoint endpoint)
        {
            calledGateway = endpoint;
            var options = new GrpcChannelOptions()
            {
                HttpHandler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true,
                },
                DisposeHttpClient = true,
            };
            return GrpcChannel.ForAddress(endpoint.Uri, options);
        }
    }

    [Test]
    public async Task Can_Create_Client_With_Custom_Channel_Factory_Over_Tls()
    {
        ConsensusNodeEndpoint calledGateway = default!;
        var certificateChecked = false;
        var gateway = (await TlsPinningTests.DiscoverTlsEndpointsAsync())[0];

        await using var client = new ConsensusClient(channelFactory, ctx =>
        {
            ctx.Payer = TestNetwork.Payer;
            ctx.Endpoint = gateway;
        });
        await client.PingAsync();
        await Assert.That(calledGateway).IsEqualTo(gateway);
        await Assert.That(certificateChecked).IsTrue();

        // A custom factory replaces the default one and its certificate
        // pinning, so it pins to the address book's hash itself.
        GrpcChannel channelFactory(ConsensusNodeEndpoint endpoint)
        {
            calledGateway = endpoint;
            var options = new GrpcChannelOptions()
            {
                HttpHandler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true,
                    SslOptions = new SslClientAuthenticationOptions
                    {
                        RemoteCertificateValidationCallback = (_, cert, _, _) =>
                        {
                            certificateChecked = true;
                            return endpoint.MatchesCertificate(cert);
                        }
                    },
                },
                DisposeHttpClient = true,
            };
            return GrpcChannel.ForAddress(endpoint.Uri, options);
        }
    }

    [Test]
    public async Task Channel_Creation_Error_Propigates()
    {
        var gateway = await TestNetwork.GetConsensusNodeEndpointAsync();

        await using var client = new ConsensusClient(channelFactory, ctx =>
        {
            ctx.Payer = TestNetwork.Payer;
            ctx.Endpoint = gateway;
        });
        var ex = await Assert.That(async () =>
        {
            await client.PingAsync();
        }).ThrowsException();
        var nie = ex as NotImplementedException;
        await Assert.That(nie).IsNotNull();
        await Assert.That(nie!.Message).IsEqualTo("Channel Creation Factory Test");

        GrpcChannel channelFactory(ConsensusNodeEndpoint endpoint)
        {
            throw new NotImplementedException("Channel Creation Factory Test");
        }
    }
}
