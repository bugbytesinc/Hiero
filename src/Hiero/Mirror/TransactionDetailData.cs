// SPDX-License-Identifier: Apache-2.0
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using Hiero.Mirror.Paging;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// Represents a transaction detail from a mirror node.
/// Similar to the TransactionData object but includes
/// custom fees and Nft (NFT) transfer data.
/// </summary>
public class TransactionDetailData : TransactionData
{
    /// <summary>
    /// Assessed custom fees for transferring tokens.
    /// </summary>
    [JsonPropertyName("assessed_custom_fees")]
    public AssessedFeeData[]? AssessedFees { get; set; }
    /// <summary>
    /// List of Assets transferred as a part of this
    /// transaction.
    /// </summary>
    [JsonPropertyName("nft_transfers")]
    public AssetTransferData[]? AssetTransfers { get; set; }
}
/// <summary>
/// Extension methods for querying transaction detail data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TransactionDetailDataExtensions
{
    /// <summary>
    /// Retrieves the parent/child transaction group for the given
    /// root transaction id via <c>/api/v1/transactions/{id}</c>. A
    /// single HAPI transaction can fan out into a parent record plus
    /// zero or more child records (from nested contract calls,
    /// scheduled transactions, etc.); this method returns the whole
    /// group in one call.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="transactionId">
    /// The transaction ID to search by.
    /// </param>
    /// <returns>
    /// A list of transactions (including child transactions with nonces)
    /// matching the given transaction ID, or an empty list if none are found.
    /// </returns>
    /// <remarks>
    /// Passing a <see cref="TransactionId"/> that already carries a
    /// <c>ChildNonce</c> or <c>Scheduled</c> flag narrows the response
    /// to that specific record — the server adds the corresponding
    /// query-parameter filter automatically. In that case the "group"
    /// is usually a single record, and the method's plural return
    /// shape (<see cref="TransactionDetailData"/><c>[]</c>) degrades
    /// gracefully to length 1.
    /// </remarks>
    public static async Task<TransactionDetailData[]> GetTransactionGroupAsync(this MirrorRestClient client, TransactionId transactionId)
    {
        var (txId, txFilters) = MirrorFormat(transactionId);
        var path = GenerateInitialPath($"transactions/{txId}", txFilters);
        var list = await client.GetSingleItemAsync(path, MirrorJsonContext.Default.TransactionDetailByIdResponse).ConfigureAwait(false);
        if (list?.Transactions?.Length > 0)
        {
            return list.Transactions;
        }
        return [];
    }
    /// <summary>
    /// Retrieves the single transaction record at a given consensus
    /// timestamp via
    /// <c>/api/v1/transactions?timestamp={consensus}&amp;limit=100</c>
    /// — consensus timestamps are globally unique across the network,
    /// so equality narrows the list to at most one record. The method
    /// unwraps the first item.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="consensus">
    /// The consensus timestamp for the transaction.
    /// </param>
    /// <returns>
    /// The transaction details, or null if not found.
    /// </returns>
    public static async Task<TransactionDetailData?> GetTransactionAsync(this MirrorRestClient client, ConsensusTimeStamp consensus)
    {
        var path = GenerateInitialPath($"transactions", [new PageLimit(100), TimestampFilter.Is(consensus)]);
        var list = await client.GetSingleItemAsync(path, MirrorJsonContext.Default.TransactionDetailByIdResponse).ConfigureAwait(false);
        return list?.Transactions?.FirstOrDefault();
    }
    /// <summary>
    /// Enumerates transactions across the network. Pair with
    /// <see cref="AccountFilter"/> to narrow to a single account
    /// (credit or debit), <see cref="ResultFilter"/> to restrict to
    /// success or failure, <see cref="TransferDirectionFilter"/> for
    /// credit/debit, <see cref="TransactionTypeFilter"/> for a
    /// specific HAPI transaction kind, or <see cref="TimestampFilter"/>
    /// to bracket a time window.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="filters">
    /// Additional query parameters. The endpoint supports
    /// <see cref="AccountFilter"/>, <see cref="ResultFilter"/>,
    /// <see cref="TransferDirectionFilter"/>,
    /// <see cref="TransactionTypeFilter"/>, <see cref="TimestampFilter"/>,
    /// <see cref="PageLimit"/>, and <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of transactions (which may include child
    /// transactions) matching the supplied filters. To list
    /// transactions that involve a specific account (regardless of
    /// payer status), pass <c>AccountFilter.Is(account)</c>.
    /// </returns>
    public static IAsyncEnumerable<TransactionDetailData> GetTransactionsAsync(this MirrorRestClient client, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"transactions", [new PageLimit(100), .. filters]);
        return client.GetPagedItemsAsync<TransactionDetailDataPage, TransactionDetailData>(path, MirrorJsonContext.Default.TransactionDetailDataPage);
    }
}