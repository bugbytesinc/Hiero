// SPDX-License-Identifier: Apache-2.0
using Hiero.Implementation;
using Org.Hiero.Block.Api;
using System.Threading.Channels;

namespace Hiero.Test.Unit.BlockNode;

/// <summary>
/// An in-memory <see cref="IPublishConnection"/> for unit-testing the
/// <see cref="BlockStreamPublisher"/> without a gRPC transport: the
/// test pushes node responses and inspects the publisher's requests
/// through two channels.
/// </summary>
internal sealed class FakePublishConnection : IPublishConnection
{
    private readonly Channel<PublishStreamRequest> _requests = Channel.CreateUnbounded<PublishStreamRequest>();
    private readonly Channel<PublishStreamResponse> _responses = Channel.CreateUnbounded<PublishStreamResponse>();
    public bool RequestsCompleted { get; private set; }
    public bool Disposed { get; private set; }

    /// <summary>The publisher's outbound requests, in write order.</summary>
    public ChannelReader<PublishStreamRequest> Requests => _requests.Reader;

    public async Task<PublishStreamRequest> NextRequestAsync(TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));
        return await _requests.Reader.ReadAsync(cts.Token);
    }

    /// <summary>Pushes a node response to the publisher's pump.</summary>
    public void Send(PublishStreamResponse response)
    {
        _responses.Writer.TryWrite(response);
    }

    /// <summary>Closes the node's response stream (server side done).</summary>
    public void CloseResponses()
    {
        _responses.Writer.TryComplete();
    }

    public ValueTask WriteAsync(PublishStreamRequest request, CancellationToken cancellationToken)
    {
        _requests.Writer.TryWrite(request);
        return ValueTask.CompletedTask;
    }
    public ValueTask CompleteRequestsAsync()
    {
        RequestsCompleted = true;
        _requests.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }
    public async ValueTask<PublishStreamResponse?> ReadAsync()
    {
        try
        {
            return await _responses.Reader.ReadAsync();
        }
        catch (ChannelClosedException)
        {
            return null;
        }
    }
    public ValueTask DisposeAsync()
    {
        Disposed = true;
        _responses.Writer.TryComplete();
        _requests.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }
}
