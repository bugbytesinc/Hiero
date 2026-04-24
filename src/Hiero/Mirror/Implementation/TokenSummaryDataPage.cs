// SPDX-License-Identifier: Apache-2.0
using System.Text.Json.Serialization;

namespace Hiero.Mirror.Implementation;
/// <summary>
/// Paged list of token-summary records.
/// </summary>
internal class TokenSummaryDataPage : Page<TokenSummaryData>
{
    /// <summary>
    /// List of token-summary records.
    /// </summary>
    [JsonPropertyName("tokens")]
    public TokenSummaryData[]? Tokens { get; set; }
    /// <summary>
    /// Enumerates the list of records.
    /// </summary>
    /// <returns>
    /// An enumerator listing the records in the list.
    /// </returns>
    public override IEnumerable<TokenSummaryData> GetItems()
    {
        return Tokens ?? Array.Empty<TokenSummaryData>();
    }
}
