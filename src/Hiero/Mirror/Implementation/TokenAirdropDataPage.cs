// SPDX-License-Identifier: Apache-2.0
using System.Text.Json.Serialization;

namespace Hiero.Mirror.Implementation;
/// <summary>
/// Paged list of token-airdrop records. Shared envelope for
/// the outstanding- and pending-airdrop endpoints, both of
/// which return the same OpenAPI <c>TokenAirdrops</c> shape.
/// </summary>
internal class TokenAirdropDataPage : Page<TokenAirdropData>
{
    /// <summary>
    /// List of airdrop records.
    /// </summary>
    [JsonPropertyName("airdrops")]
    public TokenAirdropData[]? Airdrops { get; set; }
    /// <summary>
    /// Enumerates the list of records.
    /// </summary>
    /// <returns>
    /// An enumerator listing the records in the list.
    /// </returns>
    public override IEnumerable<TokenAirdropData> GetItems()
    {
        return Airdrops ?? Array.Empty<TokenAirdropData>();
    }
}
