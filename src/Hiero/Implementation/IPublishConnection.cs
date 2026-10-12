// SPDX-License-Identifier: Apache-2.0
using Grpc.Core;
using Org.Hiero.Block.Api;

namespace Hiero.Implementation;

/// <summary>
/// The minimal duplex transport the <see cref="Hiero.BlockNode.BlockStreamPublisher"/>
/// drives: write requests, signal end-of-send, read responses until the
/// node closes its side.  Abstracting the bidi gRPC call behind this
/// seam keeps the publisher's protocol logic (event pump, cumulative
/// acknowledgements, fault mapping) unit-testable with an in-memory
/// fake, while the thin gRPC-backed implementation is exercised by the
/// live integration tests.
/// </summary>
internal interface IPublishConnection : IAsyncDisposable
{
    /// <summary>
    /// Writes one request to the node's receive stream.
    /// </summary>
    ValueTask WriteAsync(PublishStreamRequest request, CancellationToken cancellationToken);
    /// <summary>
    /// Completes the send side of the stream (no more requests).
    /// </summary>
    ValueTask CompleteRequestsAsync();
    /// <summary>
    /// Reads the next response from the node, or returns
    /// <code>null</code> when the node has closed its side of the
    /// stream with an OK status.  A close with any other status, a
    /// cancelled call or a dropped connection throws an
    /// <see cref="RpcException"/> instead, which the publisher's pump
    /// maps to a fault; an implementation (including a test fake) must
    /// throw rather than return <code>null</code> for those.
    /// </summary>
    ValueTask<PublishStreamResponse?> ReadAsync();
}

/// <summary>
/// The gRPC-backed <see cref="IPublishConnection"/> wrapping a bidi
/// <c>publishBlockStream</c> call.
/// </summary>
internal sealed class GrpcPublishConnection : IPublishConnection
{
    private readonly AsyncDuplexStreamingCall<PublishStreamRequest, PublishStreamResponse> _call;

    public GrpcPublishConnection(AsyncDuplexStreamingCall<PublishStreamRequest, PublishStreamResponse> call)
    {
        _call = call;
    }
    public async ValueTask WriteAsync(PublishStreamRequest request, CancellationToken cancellationToken)
    {
        await _call.RequestStream.WriteAsync(request, cancellationToken).ConfigureAwait(false);
    }
    public async ValueTask CompleteRequestsAsync()
    {
        await _call.RequestStream.CompleteAsync().ConfigureAwait(false);
    }
    public async ValueTask<PublishStreamResponse?> ReadAsync()
    {
        if (await _call.ResponseStream.MoveNext().ConfigureAwait(false))
        {
            return _call.ResponseStream.Current;
        }
        return null;
    }
    public ValueTask DisposeAsync()
    {
        _call.Dispose();
        return ValueTask.CompletedTask;
    }
}
