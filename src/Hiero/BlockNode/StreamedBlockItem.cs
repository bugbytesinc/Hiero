// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// A block item observed live on the block stream, delivered before
/// its block is complete or verified.
/// </summary>
/// <remarks>
/// Items arrive unverified and mid-block.  If the publisher resends
/// a block, a second run of items is delivered under the same
/// <see cref="BlockNumber"/>; the later run supersedes the earlier.
/// Consumers acting on items before block completion must treat them
/// as provisional and reconcile when <see cref="IsBlockComplete"/>
/// signals (or against the assembled block stream or mirror data).
/// </remarks>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed record StreamedBlockItem
{
    /// <summary>
    /// The block this item claims membership of, taken from the most
    /// recent block header observed on the stream (zero when items
    /// arrive before any header, which can happen when joining a
    /// stream mid-block).
    /// </summary>
    public ulong BlockNumber { get; internal init; }
    /// <summary>
    /// The block item.
    /// </summary>
    public BlockItem Item { get; internal init; } = default!;
    /// <summary>
    /// True when this item is the block's proof — the signal that
    /// <see cref="BlockNumber"/> is now complete on the wire and safe to
    /// reconcile.  The proof itself is not verified.
    /// </summary>
    public bool IsBlockComplete { get; internal init; }
}
