// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// A directive or acknowledgement received from the block node over
/// an open publish stream.
/// </summary>
/// <remarks>
/// Delivered in arrival order via
/// <see cref="BlockStreamPublisher.Events"/>.  The caller MUST consume
/// the event stream: <see cref="PublishStreamEventKind.SkipBlock"/>,
/// <see cref="PublishStreamEventKind.ResendBlock"/> and
/// <see cref="PublishStreamEventKind.BehindPublisher"/> require action
/// before or during subsequent writes.
/// </remarks>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed record PublishStreamEvent
{
    /// <summary>
    /// The kind of directive or acknowledgement.
    /// </summary>
    public PublishStreamEventKind Kind { get; internal init; }
    /// <summary>
    /// The block number the event refers to (for acknowledgements,
    /// cumulative: all blocks up to and including this number).
    /// </summary>
    public ulong BlockNumber { get; internal init; }
    /// <summary>
    /// The node's termination reason; populated only when
    /// <see cref="Kind"/> is
    /// <see cref="PublishStreamEventKind.StreamEnded"/>.
    /// </summary>
    public PublishStreamEndReason? EndReason { get; internal init; }
}
