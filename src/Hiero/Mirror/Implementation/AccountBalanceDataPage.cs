// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using System.Text.Json.Serialization;

namespace Hiero.Mirror.Implementation;
/// <summary>
/// Paged list of balance objects returned from the mirror node.
/// </summary>
internal class AccountBalanceDataPage : Page<AccountBalanceData>
{
    /// <summary>
    /// The timestamp at which this information was valid.
    /// </summary>
    [JsonPropertyName("timestamp")]
    [JsonConverter(typeof(ConsensusTimeStampConverter))]
    public ConsensusTimeStamp TimeStamp { get; set; }
    /// <summary>
    /// List of balances returned by the mirror node query.
    /// </summary>
    [JsonPropertyName("balances")]
    public AccountBalanceData[]? Balances { get; set; }
    /// <summary>
    /// Enumerates the list of balances.
    /// </summary>
    /// <returns>
    /// Enumerator for the TokenBalance objects in the list.
    /// </returns>
    public override IEnumerable<AccountBalanceData> GetItems()
    {
        return Balances ?? Array.Empty<AccountBalanceData>();
    }
}