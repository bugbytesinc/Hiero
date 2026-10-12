// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// How a block stream subscription reacts when the unverified
/// stream skips a block number, delivers a malformed block, or
/// the transport drops.
/// </summary>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public enum BlockStreamGapPolicy
{
    /// <summary>
    /// Automatically resubscribe from the last delivered block + 1
    /// (the default).  Gaps, malformed blocks and transport drops
    /// are protocol-normal on the unverified stream; recovery uses
    /// the context's <see cref="IBlockNodeContext.RetryCount"/> and
    /// <see cref="IBlockNodeContext.RetryDelay"/> with linear
    /// backoff before surfacing the failure.
    /// </summary>
    Resubscribe = 0,
    /// <summary>
    /// Terminate the subscription with a <see cref="BlockNodeException"/>
    /// (<see cref="BlockNodeExceptionCode.StreamGapDetected"/> for
    /// gaps); the caller owns recovery.
    /// </summary>
    Throw = 1,
    /// <summary>
    /// Deliver blocks exactly as received — gaps, duplicates and
    /// all; the caller inspects <see cref="Block.Number"/>.  Blocks
    /// failing the configured verification level are still dropped.
    /// </summary>
    Deliver = 2,
}
