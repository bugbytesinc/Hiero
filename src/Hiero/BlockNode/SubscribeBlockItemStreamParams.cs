// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace Hiero.BlockNode;

/// <summary>
/// The parameters for subscribing to a block node's block stream at
/// the item level: each item is delivered the moment its frame
/// arrives, before its block completes — the low-latency mode.
/// </summary>
/// <remarks>
/// This mode trades every safety the assembled mode provides for
/// latency: no buffering until block completion (an item can be
/// delivered a second or more before its block's boundary marker),
/// no duplicate dropping, and no structural verification.  A resent
/// block is delivered again under the same block number.  Use the
/// assembled <see cref="SubscribeBlockStreamParams"/> mode unless
/// milliseconds matter and the consumer can reconcile provisional
/// data — see <see cref="StreamedBlockItem"/> for the contract.
/// </remarks>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed class SubscribeBlockItemStreamParams
{
    /// <summary>
    /// The first block number to stream.  A <code>null</code> value
    /// starts at the node's latest available block (tailing mode).
    /// </summary>
    public ulong? StartingBlockNumber { get; set; } = null;
    /// <summary>
    /// The last block number to stream (inclusive).  A <code>null</code>
    /// value streams indefinitely.
    /// </summary>
    public ulong? EndingBlockNumber { get; set; } = null;
    /// <summary>
    /// The .NET Channel receiving each <see cref="StreamedBlockItem"/>
    /// as its frame arrives.
    /// </summary>
    public ChannelWriter<StreamedBlockItem> ItemWriter { get; set; } = default!;
    /// <summary>
    /// When <code>true</code> (the default), the
    /// <see cref="ItemWriter"/> channel is marked complete when the
    /// subscription finishes for any reason.
    /// </summary>
    public bool CompleteChannelWhenFinished { get; set; } = true;
    /// <summary>
    /// The reaction to block-number gaps and transport drops; defaults
    /// to <see cref="BlockStreamGapPolicy.Resubscribe"/>, resuming from
    /// the last <em>completed</em> block + 1.  Verification levels do
    /// not apply to item-level delivery.
    /// </summary>
    public BlockStreamGapPolicy GapPolicy { get; set; } = BlockStreamGapPolicy.Resubscribe;
    /// <summary>
    /// An optional token that can interrupt the subscription; firing
    /// it completes the subscription normally.
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
}
