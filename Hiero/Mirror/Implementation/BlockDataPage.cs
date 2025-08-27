using System;
using System.Collections.Generic;

namespace Hiero.Mirror.Implementation;
/// <summary>
/// Paged list of block information.
/// </summary>
internal class BlockDataPage : Page<BlockData>
{
    /// <summary>
    /// List of block info objects.
    /// </summary>
    public BlockData[]? Blocks { get; set; }
    /// <summary>
    /// Enumerates the list of block info objects.
    /// </summary>
    /// <returns>
    /// Enumerator of account block objects for this paged list.
    /// </returns>
    public override IEnumerable<BlockData> GetItems()
    {
        return Blocks ?? Array.Empty<BlockData>();
    }
}