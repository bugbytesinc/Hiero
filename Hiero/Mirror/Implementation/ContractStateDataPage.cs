using System.Text.Json.Serialization;

namespace Hiero.Mirror.Implementation;
/// <summary>
/// Paged list of slot data
/// </summary>
internal class ContractStateDataPage : Page<ContractStateData>
{
    /// <summary>
    /// List of slot details.
    /// </summary>
    [JsonPropertyName("state")]
    public ContractStateData[]? States { get; set; }
    /// <summary>
    /// Enumerates the list of account slot detail objects.
    /// </summary>
    /// <returns>
    /// Enumerator of slot detail objects for this
    /// paged list.
    /// </returns>
    public override IEnumerable<ContractStateData> GetItems()
    {
        return States ?? Array.Empty<ContractStateData>();
    }
}
