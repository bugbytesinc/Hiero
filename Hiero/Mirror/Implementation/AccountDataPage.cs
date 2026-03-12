using System.Text.Json.Serialization;

namespace Hiero.Mirror.Implementation;
/// <summary>
/// Paged list of account information.
/// </summary>
internal class AccountDataPage : Page<AccountData>
{
    /// <summary>
    /// List of account info objects.
    /// </summary>
    [JsonPropertyName("accounts")]
    public AccountData[]? Accounts { get; set; }
    /// <summary>
    /// Enumerates the list of account info objects.
    /// </summary>
    /// <returns>
    /// Enumerator of account info objects for this paged list.
    /// </returns>
    public override IEnumerable<AccountData> GetItems()
    {
        return Accounts ?? Array.Empty<AccountData>();
    }
}