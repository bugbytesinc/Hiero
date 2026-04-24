// SPDX-License-Identifier: Apache-2.0
using System.Text.Json.Serialization;

namespace Hiero.Mirror.Implementation;
/// <summary>
/// Paged list of NFT records.
/// </summary>
internal class NftDataPage : Page<NftData>
{
    /// <summary>
    /// List of NFT records.
    /// </summary>
    [JsonPropertyName("nfts")]
    public NftData[]? Nfts { get; set; }
    /// <summary>
    /// Enumerates the list of NFT records.
    /// </summary>
    /// <returns>
    /// An enumerator listing the NFT records in the list.
    /// </returns>
    public override IEnumerable<NftData> GetItems()
    {
        return Nfts ?? Array.Empty<NftData>();
    }
}
