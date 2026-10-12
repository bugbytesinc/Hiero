// SPDX-License-Identifier: Apache-2.0
using Grpc.Core;
using Hiero.BlockNode;
using Hiero.Implementation;
using Org.Hiero.Block.Api;

namespace Hiero;

public sealed partial class BlockNodeClient
{
    /// <summary>
    /// Opens a block stream publish session with the block node — the
    /// advanced, producer-side surface used by block-producing tooling,
    /// simulators and repeaters.
    /// </summary>
    /// <remarks>
    /// The returned <see cref="BlockStreamPublisher"/> exposes the
    /// publish protocol's state machine honestly: the caller writes
    /// blocks (whole or item-by-item), MUST consume the
    /// <see cref="BlockStreamPublisher.Events"/> stream to react to
    /// skip/resend/behind directives, and awaits cumulative
    /// acknowledgements.  End the session gracefully with
    /// <see cref="BlockStreamPublisher.EndStreamAsync"/>; disposing
    /// without it aborts the call.
    /// </remarks>
    /// <param name="cancellationToken">
    /// An optional token that can interrupt the session; firing it
    /// aborts the underlying stream.
    /// </param>
    /// <param name="configure">
    /// Optional callback method providing an opportunity to modify the
    /// execution configuration for just this session.
    /// </param>
    /// <returns>
    /// An open publish session.
    /// </returns>
    /// <exception cref="InvalidOperationException">If the block node URL configuration is missing.</exception>
    public Task<BlockStreamPublisher> CreateBlockStreamPublisherAsync(CancellationToken cancellationToken = default, Action<IBlockNodeContext>? configure = null)
    {
        var context = CreateChildContext(configure);
        if (context.Uri is null)
        {
            _ = context.DisposeAsync();
            throw new InvalidOperationException("The Block Node URL has not been configured. Please check that 'Uri' is set in the Block Node context.");
        }
        try
        {
            var client = new BlockStreamPublishService.BlockStreamPublishServiceClient(context.GetChannel());
            var call = client.publishBlockStream(new CallOptions(cancellationToken: cancellationToken));
            // The publisher owns the child context (and its channel
            // reference) for the life of the session.
            return Task.FromResult(new BlockStreamPublisher(new GrpcPublishConnection(call), context));
        }
        catch
        {
            // No publisher took ownership, so release the reference here.
            _ = context.DisposeAsync();
            throw;
        }
    }
}
