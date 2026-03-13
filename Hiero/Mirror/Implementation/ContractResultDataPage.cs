using System.Text.Json.Serialization;

namespace Hiero.Mirror.Implementation;
/// <summary>
/// Paged list of contract result data.
/// </summary>
internal class ContractResultDataPage : Page<ContractResultData>
{
    /// <summary>
    /// List of contract results.
    /// </summary>
    [JsonPropertyName("results")]
    public ContractResultData[]? Results { get; set; }
    /// <summary>
    /// Enumerates the list of contract result objects.
    /// </summary>
    /// <returns>
    /// Enumerator of contract result objects for this
    /// paged list.
    /// </returns>
    public override IEnumerable<ContractResultData> GetItems()
    {
        return Results ?? Array.Empty<ContractResultData>();
    }
}
