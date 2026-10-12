// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace Hiero.BlockNode;

/// <summary>
/// The parameters for subscribing to a block node's block stream,
/// receiving whole assembled blocks.
/// </summary>
/// <remarks>
/// The client owns the sharp edges of the unverified stream: it
/// assembles item frames into whole blocks, discards partial blocks
/// superseded by a resend, drops duplicate blocks, validates each
/// block per <see cref="VerificationLevel"/>, and recovers from
/// gaps and transport drops per <see cref="GapPolicy"/>.  Only
/// completed blocks are ever written to <see cref="BlockWriter"/>.
/// </remarks>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed class SubscribeBlockStreamParams
{
    /// <summary>
    /// The first block number to stream.  A <code>null</code> value
    /// starts at the node's latest available block (tailing mode).
    /// Mutually exclusive with <see cref="ResumeFromCheckpoint"/>.
    /// </summary>
    public ulong? StartingBlockNumber { get; set; } = null;
    /// <summary>
    /// A previously persisted point of verified progress to resume
    /// from: streaming starts at the checkpoint's block number + 1
    /// and the first block must chain from the checkpoint's root
    /// hash — cryptographic continuity across process restarts.
    /// Requires <see cref="VerificationLevel"/> of
    /// <see cref="BlockVerificationLevel.HashChain"/>; mutually
    /// exclusive with <see cref="StartingBlockNumber"/>.
    /// </summary>
    public BlockStreamCheckpoint? ResumeFromCheckpoint { get; set; } = null;
    /// <summary>
    /// The last block number to stream (inclusive).  A <code>null</code>
    /// value streams indefinitely.
    /// </summary>
    public ulong? EndingBlockNumber { get; set; } = null;
    /// <summary>
    /// The maximum number of blocks to deliver before returning;
    /// zero (the default) imposes no limit.
    /// </summary>
    public ulong MaxCount { get; set; } = 0;
    /// <summary>
    /// The .NET Channel receiving each assembled <see cref="Block"/>.
    /// </summary>
    public ChannelWriter<Block> BlockWriter { get; set; } = default!;
    /// <summary>
    /// When <code>true</code> (the default), the
    /// <see cref="BlockWriter"/> channel is marked complete when the
    /// subscription finishes for any reason.
    /// </summary>
    public bool CompleteChannelWhenFinished { get; set; } = true;
    /// <summary>
    /// The validation applied to each assembled block before
    /// delivery; defaults to
    /// <see cref="BlockVerificationLevel.Structural"/>.
    /// </summary>
    public BlockVerificationLevel VerificationLevel { get; set; } = BlockVerificationLevel.Structural;
    /// <summary>
    /// The reaction to stream gaps, malformed blocks and transport
    /// drops; defaults to
    /// <see cref="BlockStreamGapPolicy.Resubscribe"/>.
    /// </summary>
    public BlockStreamGapPolicy GapPolicy { get; set; } = BlockStreamGapPolicy.Resubscribe;
    /// <summary>
    /// An optional token that can interrupt the subscription; firing
    /// it completes the subscription normally.
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
}
