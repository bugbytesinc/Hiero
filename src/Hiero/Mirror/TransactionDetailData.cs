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
/// the assessed custom fees.
/// </summary>
public class TransactionDetailData : TransactionData
{
    /// <summary>
    /// The custom (royalty) fees the network assessed against this
    /// transaction. Null when no custom fees applied.
    /// </summary>
    [JsonPropertyName("assessed_custom_fees")]
    public AssessedFeeData[]? AssessedFees { get; set; }
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
    /// <param name="cancellationToken">
    /// Optional token that cancels the request.
    /// </param>
    /// <returns>
    /// A list of transactions (including child transactions with nonces)
    /// matching the given transaction ID, or an empty list if none are found.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The mirror's "not found" (HTTP 404) is returned as an empty array,
    /// not an exception.  For a recent transaction that may only mean the
    /// mirror has not caught up: absence proves a transaction did not
    /// execute only once the mirror's latest consensus time
    /// (<see cref="TransactionTimestampDataExtensions.GetLatestConsensusTimestampAsync"/>),
    /// read before this lookup, is past its valid start plus its duration
    /// plus a margin for lagging mirror replicas.  To settle a transaction
    /// whose outcome is unknown, see the example on
    /// <see cref="ConsensusException"/>.
    /// </para>
    /// <para>
    /// <b>The array is not ordered parent-first.</b> The mirror node returns
    /// the group in its own order, and child records are commonly listed
    /// ahead of the transaction you asked about — so <c>group[0]</c> is not
    /// reliably the submitted transaction. Select by content instead of by
    /// position:
    /// </para>
    /// <code>
    /// var group     = await mirror.GetTransactionGroupAsync(txId);
    /// var submitted = group.Where(t =&gt; t.Nonce == 0 &amp;&amp; !t.IsScheduled &amp;&amp; t.TransactionId == txId).ToArray();
    /// var children  = group.Where(t =&gt; t.Nonce &gt; 0);
    /// </code>
    /// <para>
    /// <c>submitted</c> is an array on purpose: it is empty while the mirror
    /// has not caught up, and holds more than one record when the
    /// transaction was submitted to more than one node, as described below.
    /// A <see cref="ResponseCode.Success"/> among them means it executed.
    /// </para>
    /// <para>
    /// The group holds every record filed under the transaction id.
    /// <c>Nonce &gt; 0</c> marks a child record the network generated on the
    /// transaction's behalf. A schedule that executed adds the executed
    /// transaction, with <c>Nonce == 0</c> and
    /// <see cref="TransactionData.IsScheduled"/> set. An atomic batch adds its
    /// inner transactions, each with <c>Nonce == 0</c> but its own
    /// <see cref="TransactionData.TransactionId"/> and a
    /// <see cref="TransactionData.BatchEndorsement"/>. Where a transaction was
    /// submitted to more than one node, the extra submissions also pass the
    /// filter above; they are told apart by
    /// <see cref="TransactionData.Status"/> and
    /// <see cref="TransactionData.GossipNode"/>. Do <i>not</i> use
    /// <see cref="TransactionData.ParentConsensus"/> to identify children: it
    /// is null for a child recorded ahead of its parent, such as an account
    /// auto-created for an alias.
    /// </para>
    /// <para>
    /// This method deliberately returns the mirror's flat array rather than
    /// pre-sorted groups, unlike the consensus-node counterpart
    /// <see cref="TransactionReceiptExtensions.GetAllReceiptsAsync"/>. The
    /// difference is not an oversight: the consensus node sends parent, child
    /// and duplicate receipts as three separate fields, so grouping there
    /// restores structure the wire supplied, whereas the mirror sends a single
    /// array and grouping here would invent structure by applying an
    /// interpretive rule. Callers who need a particular slice are better
    /// served extracting it themselves than paying for allocations they may
    /// not want on a polling loop.
    /// </para>
    /// <para>
    /// Passing a <see cref="TransactionId"/> that already carries a
    /// <c>ChildNonce</c> or <c>Scheduled</c> flag narrows the response
    /// to that specific record — this method turns those flags into the
    /// <c>nonce=</c> and <c>scheduled=</c> query parameters on the
    /// request. In that case the "group"
    /// is usually a single record, and the method's plural return
    /// shape (<see cref="TransactionDetailData"/><c>[]</c>) degrades
    /// gracefully to length 1.
    /// </para>
    /// </remarks>
    public static async Task<TransactionDetailData[]> GetTransactionGroupAsync(this MirrorRestClient client, TransactionId transactionId, CancellationToken cancellationToken = default)
    {
        var (txId, txFilters) = MirrorFormat(transactionId);
        var path = GenerateInitialPath($"transactions/{txId}", txFilters);
        var list = await client.GetSingleItemAsync(path, MirrorJsonContext.Default.TransactionDetailByIdResponse, cancellationToken).ConfigureAwait(false);
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
    /// <param name="cancellationToken">
    /// Optional token that cancels the request.
    /// </param>
    /// <returns>
    /// The transaction details, or null if not found.
    /// </returns>
    public static async Task<TransactionDetailData?> GetTransactionAsync(this MirrorRestClient client, ConsensusTimeStamp consensus, CancellationToken cancellationToken = default)
    {
        var path = GenerateInitialPath("transactions", new PageLimit(100), TimestampFilter.Is(consensus));
        var list = await client.GetSingleItemAsync(path, MirrorJsonContext.Default.TransactionDetailByIdResponse, cancellationToken).ConfigureAwait(false);
        var transactions = list?.Transactions;
        return transactions is { Length: > 0 } ? transactions[0] : null;
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
    /// <param name="cancellationToken">
    /// Token that cancels the enumeration, including a page request in
    /// flight. <c>WithCancellation</c> on the result works as well.
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
    public static IAsyncEnumerable<TransactionDetailData> GetTransactionsAsync(this MirrorRestClient client, CancellationToken cancellationToken, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath("transactions", new PageLimit(100), filters);
        return client.GetPagedItemsAsync<TransactionDetailDataPage, TransactionDetailData>(path, MirrorJsonContext.Default.TransactionDetailDataPage, cancellationToken);
    }
    /// <inheritdoc cref="GetTransactionsAsync(MirrorRestClient, CancellationToken, IMirrorQueryParameter[])"/>
    public static IAsyncEnumerable<TransactionDetailData> GetTransactionsAsync(this MirrorRestClient client, params IMirrorQueryParameter[] filters)
    {
        return client.GetTransactionsAsync(CancellationToken.None, filters);
    }
}
