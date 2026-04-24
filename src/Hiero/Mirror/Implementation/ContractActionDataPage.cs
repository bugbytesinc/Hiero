// SPDX-License-Identifier: Apache-2.0
using System.Text.Json.Serialization;

namespace Hiero.Mirror.Implementation;
/// <summary>
/// Paged list of contract-action data.
/// </summary>
internal class ContractActionDataPage : Page<ContractActionData>
{
    /// <summary>
    /// List of contract actions.
    /// </summary>
    [JsonPropertyName("actions")]
    public ContractActionData[]? Actions { get; set; }
    /// <summary>
    /// Enumerates the list of contract action objects.
    /// </summary>
    /// <returns>
    /// Enumerator of contract action objects for this paged list.
    /// </returns>
    public override IEnumerable<ContractActionData> GetItems()
    {
        return Actions ?? Array.Empty<ContractActionData>();
    }
}
