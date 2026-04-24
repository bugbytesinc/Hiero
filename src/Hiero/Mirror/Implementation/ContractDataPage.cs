// SPDX-License-Identifier: Apache-2.0
using System.Text.Json.Serialization;

namespace Hiero.Mirror.Implementation;
/// <summary>
/// Paged list of contract records.
/// </summary>
internal class ContractDataPage : Page<ContractData>
{
    /// <summary>
    /// List of contract records.
    /// </summary>
    [JsonPropertyName("contracts")]
    public ContractData[]? Contracts { get; set; }
    /// <summary>
    /// Enumerates the list of records.
    /// </summary>
    /// <returns>
    /// An enumerator listing the records in the list.
    /// </returns>
    public override IEnumerable<ContractData> GetItems()
    {
        return Contracts ?? Array.Empty<ContractData>();
    }
}
