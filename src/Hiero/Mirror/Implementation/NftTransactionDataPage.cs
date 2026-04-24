// SPDX-License-Identifier: Apache-2.0
using System.Text.Json.Serialization;

namespace Hiero.Mirror.Implementation;
/// <summary>
/// Paged list of NFT transaction-history records.
/// </summary>
internal class NftTransactionDataPage : Page<NftTransactionData>
{
    /// <summary>
    /// List of NFT transaction-history records.
    /// </summary>
    [JsonPropertyName("transactions")]
    public NftTransactionData[]? Transactions { get; set; }
    /// <summary>
    /// Enumerates the list of records.
    /// </summary>
    /// <returns>
    /// An enumerator listing the records in the list.
    /// </returns>
    public override IEnumerable<NftTransactionData> GetItems()
    {
        return Transactions ?? Array.Empty<NftTransactionData>();
    }
}
