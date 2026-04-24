// SPDX-License-Identifier: Apache-2.0
using Hiero.Converters;
using Hiero.Mirror.Filters;
using Hiero.Mirror.Implementation;
using Hiero.Mirror.Paging;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static Hiero.Mirror.Implementation.MirrorRestClientUtils;

namespace Hiero.Mirror;
/// <summary>
/// A single record in an NFT serial's transaction history, as
/// reported by the
/// <c>/api/v1/tokens/{tokenId}/nfts/{serialNumber}/transactions</c>
/// mirror-node endpoint.
/// </summary>
/// <remarks>
/// Covers any transaction that affected this particular serial —
/// mint, burn, wipe, transfer, allowance grant, etc. For mint
/// records <see cref="Sender"/> is absent; for burn/wipe records
/// <see cref="Receiver"/> is absent.
/// </remarks>
public class NftTransactionData
{
    /// <summary>
    /// The consensus timestamp at which the transaction reached
    /// consensus.
    /// </summary>
    [JsonPropertyName("consensus_timestamp")]
    public ConsensusTimeStamp Consensus { get; set; }
    /// <summary>
    /// True when this transaction was executed under an
    /// allowance granted by the current owner rather than by the
    /// owner directly.
    /// </summary>
    [JsonPropertyName("is_approval")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool IsApproval { get; set; }
    /// <summary>
    /// The child-nonce of the transaction — zero for top-level
    /// user transactions, non-zero for child transactions spawned
    /// within a contract call or atomic batch.
    /// </summary>
    [JsonPropertyName("nonce")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long Nonce { get; set; }
    /// <summary>
    /// The account receiving the NFT in this transaction. Absent
    /// for burn/wipe transactions.
    /// </summary>
    [JsonPropertyName("receiver_account_id")]
    public EntityId? Receiver { get; set; }
    /// <summary>
    /// The account sending the NFT in this transaction. Absent
    /// for mint transactions.
    /// </summary>
    [JsonPropertyName("sender_account_id")]
    public EntityId? Sender { get; set; }
    /// <summary>
    /// The full Hedera transaction identifier (payer plus
    /// valid-start timestamp).
    /// </summary>
    [JsonPropertyName("transaction_id")]
    [JsonConverter(typeof(TransactionIdMirrorConverter))]
    public TransactionId TransactionId { get; set; } = default!;
    /// <summary>
    /// The HAPI transaction type as reported by the mirror node,
    /// e.g., <c>CRYPTOTRANSFER</c>, <c>TOKENMINT</c>,
    /// <c>TOKENBURN</c>, <c>TOKENWIPE</c>.
    /// </summary>
    [JsonPropertyName("type")]
    public string? TransactionType { get; set; }
}
/// <summary>
/// Extension methods for querying an NFT serial's transaction
/// history from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class NftTransactionDataExtensions
{
    /// <summary>
    /// Enumerates the transaction history for a single NFT
    /// serial — every mint, transfer, allowance grant, and
    /// burn/wipe that touched it. Newest-first by default.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="nft">
    /// The NFT (token + serial) whose history is requested.
    /// </param>
    /// <param name="filters">
    /// Additional query filters. The endpoint supports
    /// <see cref="TimestampFilter"/>, <see cref="PageLimit"/>,
    /// and <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of the NFT's transaction records.
    /// </returns>
    public static IAsyncEnumerable<NftTransactionData> GetNftTransactionHistoryAsync(this MirrorRestClient client, Nft nft, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"tokens/{nft.Token}/nfts/{nft.SerialNumber}/transactions", [new PageLimit(100), .. filters]);
        return client.GetPagedItemsAsync<NftTransactionDataPage, NftTransactionData>(path, MirrorJsonContext.Default.NftTransactionDataPage);
    }
}
