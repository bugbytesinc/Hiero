// SPDX-License-Identifier: Apache-2.0
using System.Text.Json.Serialization;

namespace Hiero.Mirror.Implementation;
/// <summary>
/// Paged list of NFT-allowance records.
/// </summary>
internal class NftAllowanceDataPage : Page<NftAllowanceData>
{
    /// <summary>
    /// List of NFT-allowance records.
    /// </summary>
    [JsonPropertyName("allowances")]
    public NftAllowanceData[]? Allowances { get; set; }
    /// <summary>
    /// Enumerates the list of records.
    /// </summary>
    /// <returns>
    /// An enumerator listing the records in the list.
    /// </returns>
    public override IEnumerable<NftAllowanceData> GetItems()
    {
        return Allowances ?? Array.Empty<NftAllowanceData>();
    }
}
