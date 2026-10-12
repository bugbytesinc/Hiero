// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// The reason a publisher gives the block node when gracefully
/// ending a publish stream via
/// <see cref="BlockStreamPublisher.EndStreamAsync"/>.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public enum PublishStreamEndCode
{
    /// <summary>
    /// An orderly end; the publisher intends to reconnect.
    /// </summary>
    Reset = 1,
    /// <summary>
    /// The publisher timed out awaiting node responses.
    /// </summary>
    Timeout = 2,
    /// <summary>
    /// The publisher encountered an internal error.
    /// </summary>
    Error = 3,
    /// <summary>
    /// The node is too far behind this publisher to catch up over
    /// this stream; it should backfill from another source.
    /// </summary>
    TooFarBehind = 4,
}
