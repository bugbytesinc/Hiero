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
/// Nft (NFT) information retrieved from a mirror node.
/// </summary>
public class NftData
{
    /// <summary>
    /// The Current Holder of the Nft (NFT)
    /// </summary>
    [JsonPropertyName("account_id")]
    public EntityId Owner { get; set; } = default!;
    /// <summary>
    /// The consensus timestamp of the asset's creation.
    /// </summary>
    [JsonPropertyName("created_timestamp")]
    public ConsensusTimeStamp Created { get; set; }
    /// <summary>
    /// An account that is permitted to create allowances for this
    /// Nft on the owner's behalf.
    /// </summary>
    [JsonPropertyName("delegating_spender")]
    public EntityId DelegatingSpender { get; set; } = default!;
    /// <summary>
    /// Flag indicating the asset has been deleted.
    /// </summary>
    [JsonPropertyName("deleted")]
    [JsonConverter(typeof(BooleanMirrorConverter))]
    public bool Deleted { get; set; }
    /// <summary>
    /// Arbitrary binary metadata attached to this individual NFT
    /// serial. The wire format is base64 per OpenAPI
    /// (<c>format: byte</c>); decoded to raw bytes here.
    /// </summary>
    [JsonPropertyName("metadata")]
    [JsonConverter(typeof(Base64StringToBytesConverter))]
    public ReadOnlyMemory<byte> Metadata { get; set; }
    /// <summary>
    /// The last time this asset was modified.
    /// </summary>
    [JsonPropertyName("modified_timestamp")]
    public ConsensusTimeStamp Modified { get; set; }
    /// <summary>
    /// The serial number of the asset.
    /// </summary>
    [JsonPropertyName("serial_number")]
    [JsonConverter(typeof(LongMirrorConverter))]
    public long SerialNumber { get; set; }
    /// <summary>
    /// An account that is permitted to transfer this asset
    /// Nft on the owner's behalf.
    /// </summary>
    [JsonPropertyName("spender")]
    public EntityId Spender { get; set; } = default!;
    /// <summary>
    /// The Hedera token address of this asset class.
    /// </summary>
    [JsonPropertyName("token_id")]
    public EntityId Token { get; set; } = default!;
}
/// <summary>
/// Extension methods for querying NFT data from the mirror node.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class NftDataExtensions
{
    /// <summary>
    /// Retrieves the record for a specific NFT serial from
    /// <c>/api/v1/tokens/{tokenId}/nfts/{serialNumber}</c>.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="nft">
    /// The identifier of the NFT to retrieve, which includes the token address
    /// and the serial number of the NFT.
    /// </param>
    /// <param name="filters">
    /// Reserved for forward compatibility. The endpoint currently
    /// accepts no query parameters per the OpenAPI spec; any filters
    /// supplied here are included in the URL but ignored by the server.
    /// </param>
    /// <returns>
    /// The NFT record, or null if the token/serial pair is unknown to
    /// the mirror node.
    /// </returns>
    public static Task<NftData?> GetNftAsync(this MirrorRestClient client, Nft nft, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"tokens/{nft.Token}/nfts/{nft.SerialNumber}", filters);
        return client.GetSingleItemAsync(path, MirrorJsonContext.Default.NftData);
    }
    /// <summary>
    /// Enumerates the NFTs held by the given account. The records
    /// are returned newest-first by default (governed by
    /// <c>token.id</c> then <c>serialnumber</c>); pass
    /// <see cref="OrderBy.Ascending"/> to reverse.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="account">
    /// The account whose NFT holdings are requested.
    /// </param>
    /// <param name="filters">
    /// Additional query filters. The endpoint supports
    /// <see cref="TokenFilter"/>, <see cref="SpenderFilter"/>, and
    /// <see cref="SerialNumberFilter"/>, along with the usual
    /// <see cref="PageLimit"/> and <see cref="OrderBy"/>. The
    /// mirror node requires a <see cref="TokenFilter"/> to be
    /// present whenever <see cref="SerialNumberFilter"/> is used.
    /// </param>
    /// <returns>
    /// An async enumerable of NFT records.
    /// </returns>
    public static IAsyncEnumerable<NftData> GetAccountNftsAsync(this MirrorRestClient client, EntityId account, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"accounts/{MirrorFormat(account)}/nfts", [new PageLimit(100), .. filters]);
        return client.GetPagedItemsAsync<NftDataPage, NftData>(path, MirrorJsonContext.Default.NftDataPage);
    }
    /// <summary>
    /// Enumerates the individual NFT serials that have been minted
    /// under the given token. The records are returned
    /// newest-serial-first by default; pass
    /// <see cref="OrderBy.Ascending"/> to reverse.
    /// </summary>
    /// <param name="client">
    /// Mirror Rest Client to use for the request.
    /// </param>
    /// <param name="token">
    /// The NFT class whose individual serials are requested.
    /// </param>
    /// <param name="filters">
    /// Additional query filters. The endpoint supports
    /// <see cref="AccountFilter"/> (current holder),
    /// <see cref="SerialNumberFilter"/>, <see cref="PageLimit"/>,
    /// and <see cref="OrderBy"/>.
    /// </param>
    /// <returns>
    /// An async enumerable of NFT records.
    /// </returns>
    public static IAsyncEnumerable<NftData> GetTokenNftsAsync(this MirrorRestClient client, EntityId token, params IMirrorQueryParameter[] filters)
    {
        var path = GenerateInitialPath($"tokens/{MirrorFormat(token)}/nfts", [new PageLimit(100), .. filters]);
        return client.GetPagedItemsAsync<NftDataPage, NftData>(path, MirrorJsonContext.Default.NftDataPage);
    }
}