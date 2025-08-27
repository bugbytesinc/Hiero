using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hiero.Mirror.Implementation;
/// <summary>
/// Paged list of account transaction details.
/// </summary>
internal class ContractResultDataPage : Page<ContractResultData>
{
    /// <summary>
    /// List of account transaction details.
    /// </summary>
    [JsonPropertyName("results")]
    public ContractResultData[]? Results { get; set; }
    /// <summary>
    /// Enumerates the list of account transaction detail objects.
    /// </summary>
    /// <returns>
    /// Enumerator of account transaction detail objects for this
    /// paged list.
    /// </returns>
    public override IEnumerable<ContractResultData> GetItems()
    {
        return Results ?? Array.Empty<ContractResultData>();
    }
}
