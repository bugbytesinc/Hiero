// SPDX-License-Identifier: Apache-2.0
using Grpc.Net.Client;

namespace Hiero.Test.Unit.Core;

/// <summary>
/// The channels a client opens are shared with its clones and released
/// when the last of them is disposed — once, however many times each is
/// disposed.
/// </summary>
public class ClientLifetimeTests
{
    private static readonly ConsensusNodeEndpoint Endpoint = new(new EntityId(0, 0, 3), new Uri("http://localhost:50211"));

    [Test]
    public async Task Disposing_The_Last_Client_Disposes_Its_Channels()
    {
        var handlers = new List<TrackingHandler>();
        var client = CreateClient(handlers);
        OpenChannel(client);
        await Assert.That(handlers.Count).IsEqualTo(1);
        await client.DisposeAsync();
        await Assert.That(handlers[0].Disposed).IsTrue();
    }

    [Test]
    public async Task A_Live_Clone_Keeps_The_Shared_Channels_Open()
    {
        var handlers = new List<TrackingHandler>();
        var client = CreateClient(handlers);
        var clone = client.Clone();
        OpenChannel(client);
        await client.DisposeAsync();
        await Assert.That(handlers[0].Disposed).IsFalse();
        await clone.DisposeAsync();
        await Assert.That(handlers[0].Disposed).IsTrue();
    }

    [Test]
    public async Task Disposing_A_Client_Twice_Leaves_Its_Clone_Working()
    {
        var handlers = new List<TrackingHandler>();
        var client = CreateClient(handlers);
        var clone = client.Clone();
        OpenChannel(client);
        await client.DisposeAsync();
        await client.DisposeAsync();
        await Assert.That(handlers[0].Disposed).IsFalse();
        await clone.DisposeAsync();
        await Assert.That(handlers[0].Disposed).IsTrue();
    }

    [Test]
    public async Task A_Configure_Callback_That_Throws_Does_Not_Pin_The_Channels()
    {
        var handlers = new List<TrackingHandler>();
        var client = CreateClient(handlers);
        OpenChannel(client);
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await using var clone = client.Clone(_ => throw new InvalidOperationException("configure failed"));
        });
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.PingAsync(default, _ => throw new InvalidOperationException("configure failed")));
        await client.DisposeAsync();
        await Assert.That(handlers[0].Disposed).IsTrue();
    }

    [Test]
    public async Task Disposing_A_Block_Node_Client_Twice_Leaves_Its_Clone_Working()
    {
        var node = new BlockNode.FakeBlockNodeHandler();
        node.Enqueue("/org.hiero.block.api.BlockNodeService/serverStatus", Grpc.Core.StatusCode.OK, new Org.Hiero.Block.Api.ServerStatusResponse { FirstAvailableBlock = 0, LastAvailableBlock = 10 });
        var tracking = new TrackingHandler(node);
        var client = new BlockNodeClient(uri => GrpcChannel.ForAddress(uri, new GrpcChannelOptions { HttpHandler = tracking, DisposeHttpClient = true }), ctx => ctx.Uri = new Uri("http://fake-block-node:40840"));
        var clone = client.Clone();
        await client.GetServerStatusAsync();
        await client.DisposeAsync();
        await client.DisposeAsync();
        await Assert.That(tracking.Disposed).IsFalse();
        await clone.GetServerStatusAsync();
        await clone.DisposeAsync();
        await Assert.That(tracking.Disposed).IsTrue();
    }

    private static ConsensusClient CreateClient(List<TrackingHandler> handlers)
    {
        return new ConsensusClient(endpoint =>
        {
            var handler = new TrackingHandler();
            handlers.Add(handler);
            return GrpcChannel.ForAddress(endpoint.Uri, new GrpcChannelOptions { HttpHandler = handler, DisposeHttpClient = true });
        }, ctx => ctx.Endpoint = Endpoint);
    }

    private static void OpenChannel(ConsensusClient client)
    {
        var context = client.BuildChildContext(null);
        context.GetChannel();
        _ = context.DisposeAsync();
    }

    /// <summary>
    /// Records whether the channel that owns it disposed it, passing any
    /// requests to an optional fake node.
    /// </summary>
    internal sealed class TrackingHandler(HttpMessageHandler? inner = null) : DelegatingHandler(inner ?? new NoNetworkHandler())
    {
        public bool Disposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class NoNetworkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("No network in unit tests.");
        }
    }
}
