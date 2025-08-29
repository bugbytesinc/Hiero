using System.Text.Json.Serialization;

namespace Hiero.Mirror.Implementation;
/// <summary>
/// Paged list of account transaction details.
/// </summary>
internal class ExtendedContractLogDataPage : Page<ExtendedContractLogData>
{
    /// <summary>
    /// List of logs.
    /// </summary>
    [JsonPropertyName("logs")]
    public ExtendedContractLogData[]? Logs { get; set; }
    /// <summary>
    /// Enumerates the list of log objects.
    /// </summary>
    /// <returns>
    /// Enumerator of account log objects for this
    /// paged list.
    /// </returns>
    public override IEnumerable<ExtendedContractLogData> GetItems()
    {
        return Logs ?? Array.Empty<ExtendedContractLogData>();
    }
}
