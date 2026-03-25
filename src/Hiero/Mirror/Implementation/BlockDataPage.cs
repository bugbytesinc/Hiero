// SPDX-License-Identifier: Apache-2.0
using System.Text.Json.Serialization;

namespace Hiero.Mirror.Implementation;
/// <summary>
/// Paged list of block information.
/// </summary>
internal class BlockDataPage : Page<BlockData>
{
    /// <summary>
    /// List of block info objects.
    /// </summary>
    [JsonPropertyName("blocks")]
    public BlockData[]? Blocks { get; set; }
    /// <summary>
    /// Enumerates the list of block info objects.
    /// </summary>
    /// <returns>
    /// Enumerator of block info objects for this paged list.
    /// </returns>
    public override IEnumerable<BlockData> GetItems()
    {
        return Blocks ?? Array.Empty<BlockData>();
    }
}