// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics.CodeAnalysis;

namespace Hiero.BlockNode;

/// <summary>
/// Identifies a block item for which a
/// <see cref="BlockContentsProof"/> is requested.
/// </summary>
/// <remarks>
/// The item may be identified by exactly one of three means: the
/// complete item (<see cref="Item"/>), the item's hash
/// (<see cref="ItemHash"/>), or the item's zero-based index within
/// the block (<see cref="ItemIndex"/>).  Setting none, or more than
/// one, is an error.
/// </remarks>
[Experimental("HIERO001", UrlFormat = "https://github.com/bugbytesinc/Hiero/blob/main/docs/blocknode-client-plan.md")]
public sealed class GetBlockContentsProofParams
{
    /// <summary>
    /// The number of the block containing the item.
    /// </summary>
    public ulong BlockNumber { get; set; }
    /// <summary>
    /// The complete block item to prove.  Exactly one of
    /// <see cref="Item"/>, <see cref="ItemHash"/> or
    /// <see cref="ItemIndex"/> must be set.
    /// </summary>
    public BlockItem? Item { get; set; } = null;
    /// <summary>
    /// The hash of the block item to prove.  Exactly one of
    /// <see cref="Item"/>, <see cref="ItemHash"/> or
    /// <see cref="ItemIndex"/> must be set.
    /// </summary>
    public ReadOnlyMemory<byte>? ItemHash { get; set; } = null;
    /// <summary>
    /// The zero-based index within the block of the item to prove.
    /// Exactly one of <see cref="Item"/>, <see cref="ItemHash"/> or
    /// <see cref="ItemIndex"/> must be set.
    /// </summary>
    public uint? ItemIndex { get; set; } = null;
}
