using Grpc.Net.Client;
using Hiero.Test.Helpers;
using Hiero.Test.Integration.Fixtures;
using System.Net.Http;

namespace Hiero.Test.Integration.Network;

public class ChannelFactoryTests
{
    [Test]
    public async Task Can_Create_Client_With_Custom_Channel_Factory()
    {
        ConsensusNodeEndpoint calledGateway = default!;
        var gateway = await TestNetwork.GetConsensusNodeEndpointAsync();

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
