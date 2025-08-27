using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
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
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TransactionDetailDataExtensions
{
    /// <summary>
    /// Retrieves the entire list of parent and child transactions
    /// having the givin root transaction ID.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="transactionId">
    /// The transaction ID to search by.
    /// </param>
    /// <returns>
    /// A list of transactions (including child transactions with nonces)
    /// matching the given transaciton ID, or an empty list if none are found.
    /// </returns>
    public static async Task<TransactionDetailData[]> GetTransactionGroupAsync(this MirrorRestClient client, TransactionId transactionId)
    {
        var list = await client.GetSingleItemAsync<TransactionDetailByIdResponse>($"transactions/{MirrorFormat(transactionId)}").ConfigureAwait(false);
        if (list?.Transactions?.Length > 0)
        {
            return list.Transactions;
        }
        return [];
    }
    /// <summary>
    /// Retrieves the details of an individual transaction (root or child)
    /// with the given consensus timestamp.
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
        var path = GenerateInitialPath($"transactions", [new LimitFilter(100), new TimestampEqualsFilter(consensus)]);
        var list = await client.GetSingleItemAsync<TransactionDetailByIdResponse>(path).ConfigureAwait(false);
        return list?.Transactions?.FirstOrDefault();
    }
    /// <summary>
    /// Retrieves a list of transactions associated with this contract
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="account">
    /// Payer of the contract to search for.
    /// </param>
    /// <param name="filters">
    /// Additional query filters if desired.
    /// </param>
    /// <returns>
    /// A list of transactions (which may be child transactions) that
    /// involve the specified contract (regardless of payer status).
    /// </returns>
    public static IAsyncEnumerable<TransactionDetailData> GetTransactionsForAccountAsync(this MirrorRestClient client, EntityId account, params IMirrorQueryFilter[] filters)
    {
        var path = GenerateInitialPath($"transactions", [new LimitFilter(100), new AccountIsFilter(account), .. filters]);
        return client.GetPagedItemsAsync<TransactionDetailDataPage, TransactionDetailData>(path);
    }
}